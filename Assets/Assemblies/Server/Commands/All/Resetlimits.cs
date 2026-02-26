using System;
using System.Collections.Generic;
using Larnix.Core;
using Larnix.Socket.Backend;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Resetlimits : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
        public override string Pattern => $"{Name}";
        public override string ShortDescription => "Resets all registration and hashing limits.";

        private IUserManager UserManager => GlobRef.Get<IUserManager>();

        public override void Inject(string command)
        {
            if (!TrySplit(command, 1, out _))
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            UserManager.ResetLimits();

            return (CmdResult.Success,
                "Reset all registration and hashing limits.");
        }
    }
}
