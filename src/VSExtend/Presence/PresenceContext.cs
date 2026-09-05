namespace VSExtend.Presence
{
	public sealed class PresenceContext
	{
		public static readonly PresenceContext Empty = new PresenceContext();

		public string FileName { get; set; }
		public string ProjectName { get; set; }
		public string SolutionName { get; set; }
		public string SolutionDir { get; set; }
		public string DebugMode { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
	}
}
