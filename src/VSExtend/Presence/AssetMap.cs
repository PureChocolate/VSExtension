using System;
using System.Collections.Generic;

namespace VSExtend.Presence
{
	public static class AssetMap
	{
		private static readonly Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ ".c", "c" }, { ".h", "c" },
			{ ".cpp", "cpp" }, { ".cc", "cpp" }, { ".cxx", "cpp" }, { ".hpp", "cpp" },
			{ ".cs", "csharp" }, { ".csx", "csharp" }, { ".cshtml", "csharp" }, { ".razor", "csharp" },
			{ ".vb", "csharp" },
			{ ".js", "javascript" }, { ".mjs", "javascript" }, { ".cjs", "javascript" },
			{ ".ts", "typescript" }, { ".tsx", "typescript" }, { ".d.ts", "typescript" },
			{ ".jsx", "react" },
			{ ".py", "python" }, { ".pyw", "python" }, { ".ipynb", "python" },
			{ ".java", "java" },
			{ ".kt", "kotlin" }, { ".kts", "kotlin" },
			{ ".swift", "swift" },
			{ ".go", "go" },
			{ ".rs", "rust" },
			{ ".rb", "ruby" },
			{ ".php", "php" },
			{ ".lua", "lua" },
			{ ".scala", "scala" },
			{ ".clj", "java" }, { ".cljs", "java" },
			{ ".dart", "dart" },
			{ ".ex", "elixir" }, { ".exs", "elixir" },
			{ ".erl", "erlang" }, { ".hrl", "erlang" },
			{ ".hs", "haskell" },
			{ ".m", "matlab" }, { ".mat", "matlab" },
			{ ".r", "r" },
			{ ".jl", "julia" },
			{ ".html", "html5" }, { ".htm", "html5" }, { ".xhtml", "html5" },
			{ ".css", "css3" }, { ".scss", "sass" }, { ".less", "sass" },
			{ ".json", "json" }, { ".jsonc", "json" },
			{ ".xml", "html5" }, { ".xaml", "html5" }, { ".config", "html5" },
			{ ".md", "markdown" }, { ".markdown", "markdown" }, { ".mdx", "markdown" },
			{ ".tex", "latex" }, { ".bib", "latex" },
			{ ".yaml", "yaml" }, { ".yml", "yaml" },
			{ ".toml", "toml" }, { ".ini", "toml" }, { ".cfg", "toml" },
			{ ".sql", "sqlite" },
			{ ".ps1", "powershell" }, { ".psm1", "powershell" },
			{ ".sh", "bash" }, { ".bash", "bash" }, { ".zsh", "bash" },
			{ ".bat", "powershell" }, { ".cmd", "powershell" },
			{ ".dockerfile", "docker" }, { ".dockerignore", "docker" },
			{ ".sln", "visualstudio" }, { ".csproj", "visualstudio" }, { ".vbproj", "visualstudio" },
			{ ".vue", "vuejs" },
			{ ".graphql", "graphql" }, { ".gql", "graphql" },
			{ ".tf", "terraform" }, { ".tfvars", "terraform" },
			{ ".zig", "zig" },
			{ ".nim", "csharp" }
		};

		public static string IconFor(string fileName, bool useFileTypeIcons)
		{
			if (useFileTypeIcons && !string.IsNullOrEmpty(fileName))
			{
				var ext = System.IO.Path.GetExtension(fileName);
				if (!string.IsNullOrEmpty(ext) && Map.TryGetValue(ext, out var key)) return key;
			}
			return "visualstudio";
		}
	}
}
