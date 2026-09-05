namespace VSExtend.Git
{
	public sealed class GitInfo
	{
		public static readonly GitInfo Empty = new GitInfo();

		public string Branch { get; set; }
		public int? DirtyCount { get; set; }
	}
}
