using System;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Threading;
using System.Threading.Tasks;

namespace VSExtend.Commands
{
	internal static class CommandIds
	{
		public const string CommandSetGuidString = "{b3fc5b2e-357f-40f2-b04e-d243f50433a5}";
		public static readonly Guid CommandSet = new Guid(CommandSetGuidString);
		public const int CmdidToggleRichPresence = 0x0100;
		public const int CmdidOpenSettings = 0x0101;
	}
}
