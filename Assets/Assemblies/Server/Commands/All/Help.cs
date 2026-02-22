using System;
using System.Collections.Generic;
using System.Text;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Help : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.User;
        public override string Pattern => $"{Name} [command]";
        public override string ShortDescription => "Displays help information about commands.";

        private bool _hasArgument;
        private string _argument;

        public override void Inject(string command)
        {
            if (TrySplit(command, 2, out string[] parts) ||
                TrySplit(command, 1, out parts))
            {
                _hasArgument = parts.Length == 2;
                _argument = _hasArgument ?
                    parts[1].ToLowerInvariant() : null;
            }
            else
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            if (_hasArgument) // command
            {
                if (TryGetDocumentation(_argument, privilege, out var docs))
                {
                    return (CmdResult.Raw,
                        MakeRobustList("COMMAND INFO", docs.Split('\n')));
                }

                return (CmdResult.Error,
                    $"No command named '{_argument}' was found.");
            }
            else // documentation
            {
                IEnumerable<string> lines = AllCommandDocs(privilege);

                return (CmdResult.Raw,
                    MakeRobustList("COMMAND LIST", lines));
            }
        }
    }
}
