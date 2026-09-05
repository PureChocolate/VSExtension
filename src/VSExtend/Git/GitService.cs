using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VSExtend.Git
{
	public sealed class GitService
	{
		private sealed class Entry
		{
			public GitInfo Info;
			public DateTimeOffset Fetched;
		}

		private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(1);
		private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Entry> _cache =
			new System.Collections.Concurrent.ConcurrentDictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

		public GitInfo Cached(string solutionDir)
		{
			if (string.IsNullOrEmpty(solutionDir)) return GitInfo.Empty;
			if (_cache.TryGetValue(solutionDir, out var entry) && DateTimeOffset.UtcNow - entry.Fetched < MaxAge)
				return entry.Info;
			return GitInfo.Empty;
		}

		public Task<GitInfo> RefreshAsync(string solutionDir)
		{
			return Task.Run(() => RefreshCore(solutionDir));
		}

		public void Invalidate(string solutionDir)
		{
			if (!string.IsNullOrEmpty(solutionDir)) _cache.TryRemove(solutionDir, out _);
		}

		private GitInfo RefreshCore(string solutionDir)
		{
			var result = GitInfo.Empty;
			if (string.IsNullOrEmpty(solutionDir)) return result;

			var root = FindRepoRoot(solutionDir);
			if (root == null) return result;

			var branch = RunGit(root, "branch --show-current").Trim();
			var porcelain = RunGit(root, "status --porcelain");
			var dirty = porcelain == null ? -1 : porcelain.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

			result = new GitInfo
			{
				Branch = string.IsNullOrEmpty(branch) ? null : branch,
				DirtyCount = dirty < 0 ? (int?)null : dirty
			};

			_cache[solutionDir] = new Entry { Info = result, Fetched = DateTimeOffset.UtcNow };
			return result;
		}

		private static string FindRepoRoot(string startDir)
		{
			var dir = new DirectoryInfo(startDir);
			for (int i = 0; i < 10 && dir != null; i++)
			{
				if (Directory.Exists(Path.Combine(dir.FullName, ".git")) || File.Exists(Path.Combine(dir.FullName, ".git")))
					return dir.FullName;
				dir = dir.Parent;
			}
			return null;
		}

		private static string RunGit(string workDir, string arguments)
		{
			try
			{
				var psi = new System.Diagnostics.ProcessStartInfo
				{
					FileName = "git",
					Arguments = "-C \"" + workDir + "\" " + arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				};
				using (var process = System.Diagnostics.Process.Start(psi))
				{
					if (process == null) return "";
					var output = process.StandardOutput.ReadToEnd();
					process.WaitForExit(10000);
					return output ?? "";
				}
			}
			catch
			{
				return "";
			}
		}
	}
}
