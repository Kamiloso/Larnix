using Larnix.Core;
using Larnix.Model.Utils;
using Larnix.Server.Entities;
using Larnix.Model;

namespace Larnix.Server.Commands.All;

internal class Kill : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Admin;
    public override string Pattern => $"{Name} <nickname>";
    public override string ShortDescription => "Kills a player.";

    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();

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
        if (ConnectedPlayers.IsAlive(_nickname))
        {
            ulong uid = ConnectedPlayers.UidByNickname(_nickname);
            EntityControllers.KillController(uid);

            return (CmdResult.Success,
                $"Player {_nickname} has been killed.");
        }
        else
        {
            return (CmdResult.Error,
                $"Player {_nickname} is not alive.");
        }
    }
}
