using System;
using System.Collections.Generic;
using System.Linq;
using Larnix.Socket.Backend;
using Larnix.Worldgen;
using Larnix.Core;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Info : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name}";
        public override string ShortDescription => "Displays the server information.";

        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private Server Server => GlobRef.Get<Server>();
        private Generator Generator => GlobRef.Get<Generator>();

        public override void Inject(string command)
        {
            if (!TrySplit(command, 1, out _))
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            IEnumerable<string> lines = new[] {
                $"Version: {Core.Version.Current}",
                $"Players: {QuickServer.PlayerCount} / {QuickServer.PlayerLimit}",
                $"Port: {Server.Port}",
                $"Authcode: {QuickServer.Authcode}",
                $"Seed: {Generator.Seed}",
            };

            return (CmdResult.Raw,
                MakeRobustList("SERVER INFO", lines));
        }
    }
}
