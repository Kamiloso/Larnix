using Larnix.Model.Utils;
using Larnix.Server.Entities;
using Larnix.Socket.Backend;
using Larnix.Core;
using Larnix.Model;

namespace Larnix.Server.Commands.All;

internal class Kick : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
    public override string Pattern => $"{Name} <nickname>";
    public override string ShortDescription => "Kicks a player.";

    private QuickServer QuickServer => GlobRef.Get<QuickServer>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();

    private string _nickname;

    public override void Inject(string command)
    {
        if (TrySplit(command, 2, out string[] parts))
        {
            if (!Parsing.TryParseNickname(parts[1], out var nickname))
            {
                throw FormatException(Validation.WrongNicknameInfo);
            }

            _nickname = nickname;
        }
        else
        {
            throw FormatException(InvalidCmdFormat);
        }
    }

    public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
    {
        if (ConnectedPlayers.IsConnected(_nickname))
        {
            QuickServer.KickRequest(_nickname);

            return (CmdResult.Info,
                $"Player {_nickname} is being kicked...");
        }
        else
        {
            return (CmdResult.Error,
                $"Player {_nickname} is not connected.");
        }
    }
}
