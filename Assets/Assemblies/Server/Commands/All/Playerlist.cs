#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Server.Entities;
using Larnix.Socket.Backend;
using Larnix.Core;
using Larnix.Model.Interfaces;

namespace Larnix.Server.Commands.All;

internal class Playerlist : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
    public override string Pattern => $"{Name}";
    public override string ShortDescription => "Displays a list of all connected players.";

    private QuickServer QuickServer => GlobRef.Get<QuickServer>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();

    public override void Inject(string command)
    {
        if (!TrySplit(command, 1, out _))
        {
            throw FormatException(InvalidCmdFormat);
        }
    }

    public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
    {
        IPEndPoint? EndPointOf(string nick)
        {
            if (QuickServer.TryGetClientEndPoint(nick, out var endPoint))
                return endPoint;

            return null;
        }

        string StateOf(string nick)
        {
            return ConnectedPlayers.StateOf(nick)
                .ToString()
                .ToUpperInvariant();
        }

        IEnumerable<string> lines = ConnectedPlayers.AllPlayers
            .Select(nick => $"{nick} from {EndPointOf(nick)} is {StateOf(nick)}.")
            .OrderBy(line => line);

        return (CmdResult.Raw,
            MakeRobustList("PLAYER LIST", lines));
    }
}
