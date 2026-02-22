using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Server.Entities;
using Larnix.Socket.Backend;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Playerlist : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
        public override string Pattern => $"{Name}";
        public override string ShortDescription => "Displays a list of all connected players.";

        private QuickServer QuickServer => Ref.QuickServer;
        private PlayerManager PlayerManager => Ref.PlayerManager;

        public override void Inject(string command)
        {
            if (!TrySplit(command, 1, out _))
            {
                throw FormatException(InvalidCmdFormat);
            }
        }

        public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
        {
            IPEndPoint EndPointOf(string nick)
            {
                if (QuickServer.TryGetClientEndPoint(nick, out var endPoint))
                    return endPoint;
                
                return null;
            }

            string StateOf(string nick)
            {
                return PlayerManager
                    .StateOf(nick)
                    .ToString()
                    .ToUpperInvariant();
            }

            IEnumerable<string> lines = PlayerManager.AllPlayers()
                .Select(nick => $"{nick} from {EndPointOf(nick)} is {StateOf(nick)}.")
                .OrderBy(line => line);

            return (CmdResult.Raw,
                MakeRobustList("PLAYER LIST", lines));
        }
    }
}
