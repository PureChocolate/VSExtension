using System;
using System.Collections.Generic;
using DiscordRPC;
using VSExtend.Git;
using VSExtend.Options;

namespace VSExtend.Presence
{
	public static class PresenceBuilder
	{
		public static RichPresence Build(PresenceContext ctx, GitInfo git, OptionsSnapshot options, DateTimeOffset sessionStart)
		{
			var details = string.IsNullOrEmpty(ctx.FileName) ? "Visual Studio" : "Editing " + ctx.FileName;

			var parts = new List<string>();
			if (options.ShowDebugMode && !string.IsNullOrEmpty(ctx.DebugMode)) parts.Add(ctx.DebugMode);
			if (options.ShowSolutionName && !string.IsNullOrEmpty(ctx.SolutionName)) parts.Add(ctx.SolutionName);
			if (options.ShowProjectName && !string.IsNullOrEmpty(ctx.ProjectName)
				&& !string.Equals(ctx.ProjectName, ctx.SolutionName, StringComparison.OrdinalIgnoreCase))
				parts.Add(ctx.ProjectName);
			if (options.ShowGitBranch && !string.IsNullOrEmpty(git.Branch)) parts.Add(git.Branch);
			if (options.ShowDirtyCount && git.DirtyCount.HasValue && git.DirtyCount.Value > 0)
				parts.Add(git.DirtyCount.Value == 1 ? "1 modified" : git.DirtyCount.Value + " modified");
			if (options.ShowCursorPosition && ctx.Line > 0)
				parts.Add("Ln " + ctx.Line + ", Col " + ctx.Column);

			var state = parts.Count == 0 ? null : string.Join(" \u00B7 ", parts);

			var assets = new Assets
			{
				LargeImageKey = AssetMap.IconFor(ctx.FileName, options.ShowFileTypeIcons),
				LargeImageText = ctx.ProjectName ?? ctx.SolutionName ?? "Visual Studio"
			};

			if (options.ShowDebugMode && ctx.DebugMode != null)
			{
				assets.SmallImageKey = "debugging";
				assets.SmallImageText = ctx.DebugMode;
			}

			var presence = new RichPresence
			{
				Details = Truncate(details, 128),
				State = Truncate(state, 128),
				Assets = assets
			};

			if (options.ShowElapsedTime)
				presence.Timestamps = new Timestamps { StartUnixMilliseconds = (ulong)sessionStart.ToUnixTimeMilliseconds() };

			return presence;
		}

		private static string Truncate(string s, int max)
		{
			if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
			return s.Substring(0, max - 1) + "\u2026";
		}
	}
}
