using System.Windows;
using VSExtend.Options;

namespace VSExtend.Settings
{
	public partial class SettingsWindow : Window
	{
		private readonly GeneralOptionsPage _page;

		public SettingsWindow(GeneralOptionsPage page)
		{
			InitializeComponent();
			_page = page;
			chkEnabled.IsChecked = page.Enabled;
			txtAppId.Text = page.ApplicationId ?? "";
			chkFileIcons.IsChecked = page.ShowFileTypeIcons;
			chkSolution.IsChecked = page.ShowSolutionName;
			chkProject.IsChecked = page.ShowProjectName;
			chkBranch.IsChecked = page.ShowGitBranch;
			chkDirty.IsChecked = page.ShowDirtyCount;
			chkDebug.IsChecked = page.ShowDebugMode;
			chkElapsed.IsChecked = page.ShowElapsedTime;
			chkCursor.IsChecked = page.ShowCursorPosition;
		}

		private void OnOk(object sender, RoutedEventArgs e)
		{
			_page.Enabled = chkEnabled.IsChecked == true;
			_page.ApplicationId = (txtAppId.Text ?? "").Trim();
			_page.ShowFileTypeIcons = chkFileIcons.IsChecked == true;
			_page.ShowSolutionName = chkSolution.IsChecked == true;
			_page.ShowProjectName = chkProject.IsChecked == true;
			_page.ShowGitBranch = chkBranch.IsChecked == true;
			_page.ShowDirtyCount = chkDirty.IsChecked == true;
			_page.ShowDebugMode = chkDebug.IsChecked == true;
			_page.ShowElapsedTime = chkElapsed.IsChecked == true;
			_page.ShowCursorPosition = chkCursor.IsChecked == true;
			_page.SaveAndNotify();
			DialogResult = true;
			Close();
		}
	}
}
