using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Server.Entities;
using Larnix.Socket.Backend;
using Larnix.Core;
using CmdResult = Larnix.Core.ICmdExecutor.CmdResult;

namespace Larnix.Server.Commands.All
{
    internal class Userlist : BaseCmd
    {
        public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
        public override string Pattern => $"{Name}";
        public override string ShortDescription => "Displays a list of all registered users.";

        private QuickServer QuickServer => GlobRef.Get<QuickServer>();
        private PlayerActions PlayerActions => GlobRef.Get<PlayerActions>();
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
            IPEndPoint EndPointOf(string nick)
            {
                if (QuickServer.TryGetClientEndPoint(nick, out var endPoint))
                    return endPoint;
                
                return null;
            }

            string StateOf(string nick)
            {
                return PlayerActions
                    .StateOf(nick)
                    .ToString()
                    .ToUpperInvariant();
            }

            string NONE = PlayerActions.PlayerState.None
                .ToString().ToUpperInvariant();

            IEnumerable<string> lines = UserManager.AllUsernames()
                .OrderBy(nick => StateOf(nick) == NONE ? 1 : 0)
                .ThenBy(nick => nick)
                .Select(nick =>
                {
                    IPEndPoint endPoint = EndPointOf(nick);
                    string state = StateOf(nick);

                    return state != NONE ?
                        $"{nick} from {endPoint} is {state}." :
                        $"{nick} is OFFLINE.";
                });

            return (CmdResult.Raw,
                MakeRobustList("USER LIST", lines));
        }
    }
}
