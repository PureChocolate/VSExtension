namespace VSExtend.Options
{
	public sealed class OptionsSnapshot
	{
		public bool Enabled { get; set; } = true;
		public string ApplicationId { get; set; } = "";
		public bool ShowGitBranch { get; set; } = true;
		public bool ShowDirtyCount { get; set; } = true;
		public bool ShowDebugMode { get; set; } = true;
		public bool ShowElapsedTime { get; set; } = true;
		public bool ShowProjectName { get; set; } = true;
		public bool ShowSolutionName { get; set; } = true;
		public bool ShowFileTypeIcons { get; set; } = true;
		public bool ShowCursorPosition { get; set; } = true;

		public static OptionsSnapshot From(GeneralOptionsPage page)
		{
			return new OptionsSnapshot
			{
				Enabled = page.Enabled,
				ApplicationId = page.ApplicationId ?? "",
				ShowGitBranch = page.ShowGitBranch,
				ShowDirtyCount = page.ShowDirtyCount,
				ShowDebugMode = page.ShowDebugMode,
				ShowElapsedTime = page.ShowElapsedTime,
				ShowProjectName = page.ShowProjectName,
				ShowSolutionName = page.ShowSolutionName,
				ShowFileTypeIcons = page.ShowFileTypeIcons,
				ShowCursorPosition = page.ShowCursorPosition
			};
		}
	}
}
