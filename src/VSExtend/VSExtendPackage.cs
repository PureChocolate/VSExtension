using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using VSExtend.Commands;
using VSExtend.Git;
using VSExtend.Options;
using VSExtend.Presence;

namespace VSExtend
{
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("VSExtend (Discord Rich Presence)", "Shows your Visual Studio coding activity on Discord.", "1.0")]
	[Guid("3dcf0749-569f-4ba0-9c44-730c6e8498d0")]
	[ProvideMenuResource("VSExtend.ctmenu", 1)]
	[ProvideOptionPage(typeof(GeneralOptionsPage), "VSExtend", "General", 0, 0, false, "")]
	[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class VSExtendPackage : AsyncPackage
	{
		public const string OutputPaneGuidString = "{f3244ab6-4dd9-4bc2-a7b6-d31a8fbac96f}";

		private DTE2 _dte;
		private DiscordService _discord;
		private GitService _git;
		private System.Threading.Timer _gitTimer;
		private System.Threading.Timer _caretTimer;
		private IVsOutputWindowPane _outputPane;
		private bool _shuttingDown;

		private DTEEvents _dteEvents;
		private SolutionEvents _solutionEvents;
		private WindowEvents _windowEvents;
		private DocumentEvents _documentEvents;
		private DebuggerEvents _debuggerEvents;
		private SelectionEvents _selectionEvents;

		private PresenceContext _context = PresenceContext.Empty;

		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			await base.InitializeAsync(cancellationToken, progress);
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			await EnsureOutputPaneAsync();

			_dte = (DTE2)await GetServiceAsync(typeof(SDTE));
			var commands = (IMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService));
			if (commands != null)
			{
				var menu = new MenuCommand(OnToggleExecuted, new CommandID(CommandIds.CommandSet, CommandIds.CmdidToggleRichPresence));
				commands.AddCommand(menu);
				var settingsMenu = new MenuCommand(OnOpenSettingsExecuted, new CommandID(CommandIds.CommandSet, CommandIds.CmdidOpenSettings));
				commands.AddCommand(settingsMenu);
			}

			_git = new GitService();
			_discord = new DiscordService(Log);

			var events = _dte.Events;
			_dteEvents = events.DTEEvents;
			_dteEvents.OnBeginShutdown += OnBeginShutdown;
			_solutionEvents = events.SolutionEvents;
			_solutionEvents.Opened += OnSolutionOpened;
			_solutionEvents.AfterClosing += OnSolutionClosing;
			_solutionEvents.ProjectAdded += OnProjectChanged;
			_solutionEvents.ProjectRemoved += OnProjectChanged;
			_windowEvents = events.WindowEvents;
			_windowEvents.WindowActivated += OnWindowActivated;
			_documentEvents = events.DocumentEvents;
			_documentEvents.DocumentOpened += OnDocumentChanged;
			_documentEvents.DocumentSaved += OnDocumentChanged;
			_documentEvents.DocumentClosing += OnDocumentChanged;
			_debuggerEvents = events.DebuggerEvents;
			_debuggerEvents.OnEnterRunMode += OnDebugModeChanged;
			_debuggerEvents.OnEnterBreakMode += OnDebugBreakMode;
			_debuggerEvents.OnEnterDesignMode += OnDebugModeChanged;
			_selectionEvents = events.SelectionEvents;
			_selectionEvents.OnChange += OnSelectionChanged;

			GetOptionsPage().Changed += OnOptionsChanged;
			ApplyOptions();

			_gitTimer = new System.Threading.Timer(OnGitTimer, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(60));
			_caretTimer = new System.Threading.Timer(OnCaretTimer, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
		}

		private GeneralOptionsPage GetOptionsPage()
		{
			return (GeneralOptionsPage)GetDialogPage(typeof(GeneralOptionsPage));
		}

		private OptionsSnapshot ReadOptions()
		{
			return OptionsSnapshot.From(GetOptionsPage());
		}

		private void ApplyOptions()
		{
			var options = ReadOptions();
			_discord.Configure(options.Enabled, options.ApplicationId);
			UpdatePresence();
		}

		private void OnOptionsChanged(object sender, EventArgs e)
		{
			ApplyOptions();
		}

		private void OnToggleExecuted(object sender, EventArgs e)
		{
			var page = GetOptionsPage();
			page.Enabled = !page.Enabled;
			page.SaveSettingsToStorage();
			Log("VSExtend: rich presence " + (page.Enabled ? "enabled" : "disabled") + ".");
			ApplyOptions();
		}

		private void OnOpenSettingsExecuted(object sender, EventArgs e)
		{
			var window = new Settings.SettingsWindow(GetOptionsPage());
			window.ShowDialog();
		}

		private void UpdatePresence()
		{
			if (_shuttingDown || _dte == null) return;
			PresenceContext ctx;
			try
			{
				ctx = ReadContext();
			}
			catch (Exception ex)
			{
				Log("VSExtend: failed to read editor state: " + ex.Message);
				return;
			}
			_context = ctx;
			_discord.Push(_context, _git.Cached(ctx.SolutionDir), ReadOptions());
		}

		private PresenceContext ReadContext()
		{
			var ctx = new PresenceContext();
			try
			{
				var solution = _dte.Solution;
				var full = solution?.FullName;
				if (!string.IsNullOrEmpty(full))
				{
					ctx.SolutionName = Path.GetFileNameWithoutExtension(full);
					ctx.SolutionDir = Path.GetDirectoryName(full);
				}
			}
			catch { }

			try
			{
				var doc = _dte.ActiveDocument;
				if (doc != null && !string.IsNullOrEmpty(doc.FullName))
				{
					ctx.FileName = doc.Name;
					var project = doc.ProjectItem?.ContainingProject;
					if (project != null && !string.IsNullOrEmpty(project.Name)) ctx.ProjectName = project.Name;
				}
			}
			catch { }

			try
			{
				var selection = _dte.ActiveDocument?.Selection as EnvDTE.TextSelection;
				if (selection != null)
				{
					var point = selection.ActivePoint;
					ctx.Line = point.Line;
					ctx.Column = point.LineCharOffset;
				}
			}
			catch { }

			try
			{
				var window = _dte.ActiveWindow;
				if (string.IsNullOrEmpty(ctx.ProjectName) && window?.Project != null && !string.IsNullOrEmpty(window.Project.Name))
					ctx.ProjectName = window.Project.Name;
			}
			catch { }

			try
			{
				var mode = _dte.Debugger.CurrentMode;
				if (mode == dbgDebugMode.dbgRunMode) ctx.DebugMode = "Debugging";
				else if (mode == dbgDebugMode.dbgBreakMode) ctx.DebugMode = "Paused";
			}
			catch { }

			return ctx;
		}

		private void OnSolutionOpened()
		{
			UpdatePresence();
			KickGitRefresh();
		}

		private void OnSolutionClosing()
		{
			_git.Invalidate(_context.SolutionDir);
			UpdatePresence();
		}

		private void OnProjectChanged(Project project)
		{
			UpdatePresence();
			KickGitRefresh();
		}

		private void OnWindowActivated(Window gotFocus, Window lostFocus)
		{
			UpdatePresence();
		}

		private void OnDocumentChanged(Document document)
		{
			UpdatePresence();
		}

		private void OnDebugModeChanged(dbgEventReason reason)
		{
			UpdatePresence();
		}

		private void OnDebugBreakMode(dbgEventReason reason, ref dbgExecutionAction executionAction)
		{
			UpdatePresence();
		}

		private void OnSelectionChanged()
		{
			UpdatePresence();
		}

		private void KickGitRefresh()
		{
			var dir = _context.SolutionDir;
			if (string.IsNullOrEmpty(dir)) return;
			_git.RefreshAsync(dir).ContinueWith(t =>
			{
				if (t.IsFaulted || t.IsCanceled) return;
				_discord.Push(_context, t.Result, ReadOptions());
			}, TaskScheduler.Default);
		}

		private void OnGitTimer(object state)
		{
			KickGitRefresh();
		}

		private void OnCaretTimer(object state)
		{
			if (_shuttingDown || _dte == null) return;
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				if (_shuttingDown || _dte == null) return;
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				PresenceContext ctx;
				try
				{
					ctx = ReadContext();
				}
				catch
				{
					return;
				}
				if (ctx.Line != _context.Line || ctx.Column != _context.Column)
					UpdatePresence();
			}).Task.Forget();
		}

		private void OnBeginShutdown()
		{
			_shuttingDown = true;
			try { _gitTimer?.Dispose(); } catch { }
			try { _caretTimer?.Dispose(); } catch { }
			try { _dteEvents.OnBeginShutdown -= OnBeginShutdown; } catch { }
			try { _solutionEvents.Opened -= OnSolutionOpened; } catch { }
			try { _solutionEvents.AfterClosing -= OnSolutionClosing; } catch { }
			try { _solutionEvents.ProjectAdded -= OnProjectChanged; } catch { }
			try { _solutionEvents.ProjectRemoved -= OnProjectChanged; } catch { }
			try { _windowEvents.WindowActivated -= OnWindowActivated; } catch { }
			try { _documentEvents.DocumentOpened -= OnDocumentChanged; } catch { }
			try { _documentEvents.DocumentSaved -= OnDocumentChanged; } catch { }
			try { _documentEvents.DocumentClosing -= OnDocumentChanged; } catch { }
			try { _debuggerEvents.OnEnterRunMode -= OnDebugModeChanged; } catch { }
			try { _debuggerEvents.OnEnterBreakMode -= OnDebugBreakMode; } catch { }
			try { _debuggerEvents.OnEnterDesignMode -= OnDebugModeChanged; } catch { }
			try { _selectionEvents.OnChange -= OnSelectionChanged; } catch { }
			_discord?.Dispose();
		}

		private async Task EnsureOutputPaneAsync()
		{
			var outputWindow = (IVsOutputWindow)await GetServiceAsync(typeof(SVsOutputWindow));
			var paneGuid = new Guid(OutputPaneGuidString);
			if (ErrorHandler.Failed(outputWindow.GetPane(ref paneGuid, out _outputPane)))
			{
				outputWindow.CreatePane(ref paneGuid, "VSExtend", 1, 1);
				outputWindow.GetPane(ref paneGuid, out _outputPane);
			}
		}

		private void Log(string message)
		{
			try
			{
				_outputPane?.OutputString(message + Environment.NewLine);
			}
			catch { }
		}
	}
}
