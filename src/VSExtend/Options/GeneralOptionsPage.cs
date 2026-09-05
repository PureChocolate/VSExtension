using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace VSExtend.Options
{
	public class GeneralOptionsPage : DialogPage
	{
		public event EventHandler Changed;

		[DisplayName("Enabled"), Description("Enable or disable Discord Rich Presence while Visual Studio is running.")]
		public bool Enabled { get; set; } = true;

		[DisplayName("Application ID"), Description("Discord application ID from discord.com/developers/applications (under My Applications, the numeric ID).")]
		public string ApplicationId { get; set; } = "";

		[DisplayName("Show git branch"), Description("Show the current git branch in the presence state.")]
		public bool ShowGitBranch { get; set; } = true;

		[DisplayName("Show modified file count"), Description("Show how many files are modified in the working tree.")]
		public bool ShowDirtyCount { get; set; } = true;

		[DisplayName("Show debug state"), Description("Show Debugging/Paused with an icon while a debug session is active.")]
		public bool ShowDebugMode { get; set; } = true;

		[DisplayName("Show elapsed time"), Description("Show how long the session is active (since Visual Studio enabled presence).")]
		public bool ShowElapsedTime { get; set; } = true;

		[DisplayName("Show project name"), Description("Show the current project name in the presence state.")]
		public bool ShowProjectName { get; set; } = true;

		[DisplayName("Show solution name"), Description("Show the current solution name in the presence state.")]
		public bool ShowSolutionName { get; set; } = true;

		[DisplayName("Use file type icons"), Description("Use uploaded per-language icons for the large image (falls back to the Visual Studio icon).")]
		public bool ShowFileTypeIcons { get; set; } = true;

		[DisplayName("Show cursor position"), Description("Show the current line and column of the caret.")]
		public bool ShowCursorPosition { get; set; } = true;

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void SaveAndNotify()
		{
			SaveSettingsToStorage();
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
