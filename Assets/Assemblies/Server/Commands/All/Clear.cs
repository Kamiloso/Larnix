using System;
using System.Collections.Generic;
using System.Linq;
using Larnix.Socket.Backend;
using Larnix.Worldgen;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Clear : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.User;
        public override string Pattern => $"{Name}";
        public override string ShortDescription => "Clears the chat / console.";

        public override void Inject(string command)
        {
            if (!TrySplit(command, 1, out _))
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            return (CmdResult.Clear, string.Empty);
        }
    }
}
