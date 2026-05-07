#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Core;
using Larnix.Model.Utils;
using Larnix.Server.Entities;
using Larnix.Model;
using Larnix.Server.Repositories;
using Larnix.Server.Network;
using Larnix.Socket;

namespace Larnix.Server.Commands.All;

internal class User : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Host;
    public override string Pattern => $"{Name} <params...>";
    public override string Hint => $"Type 'help {Name}' for more information.";
    public override string ShortDescription => "Manages user accounts.";

    public override string LongDescription => ShortDescription + " Usage:\n" +
        $"user set <username> <password> - Sets user's credentials.\n" +
        $"user rename <oldusername> <newusername> - Renames a user.\n" +
        $"user delete <username> - Deletes a user.\n" +
        $"user resetlimits - Resets all limits in a socket.\n" + // TODO: Move this to a separate command
        $"user list - Lists all registered users.\n" +
        $"user deleteall - Deletes all users except host and '{SocketInfo.ReservedNickname}'.";

    private IServer Server => GlobRef.Get<IServer>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();
    private IUserRepository UserRepository => GlobRef.Get<IUserRepository>();

    private string _subname = "";
    private string _username = "";
    private string _password = "";
    private string _oldusername = "";
    private string _newusername = "";

    public override void Inject(string command)
    {
        if (!TrySplitMin(command, 2, out string[] parts, out int length))
            throw FormatException(InvalidCmdFormat);

        string subname = parts[1];
        string subcommand = string.Join(' ', parts[1..]);
        _subname = subname;

        _username = "";
        _password = "";
        _oldusername = "";
        _newusername = "";

        var commands = new Dictionary<string, Action<string[]>>
        {
            ["set"] = args =>
            {
                if (!Parsing.TryParseNickname(args[1], out string username)) throw FormatException(Validation.WrongNicknameInfo);
                if (!Parsing.TryParsePassword(args[2], out string password)) throw FormatException(Validation.WrongPasswordInfo);
                _username = username;
                _password = password;
            },
            ["rename"] = args =>
            {
                if (!Parsing.TryParseNickname(args[1], out string oldusername)) throw FormatException(Validation.WrongNicknameInfo);
                if (!Parsing.TryParseNickname(args[2], out string newusername)) throw FormatException(Validation.WrongNicknameInfo);
                _oldusername = oldusername;
                _newusername = newusername;
            },
            ["delete"] = args =>
            {
                if (!Parsing.TryParseNickname(args[1], out string username)) throw FormatException(Validation.WrongNicknameInfo);
                _username = username;
            },
            ["resetlimits"] = args => { },
            ["list"] = args => { },
            ["deleteall"] = args => { }
        };

        if (!commands.TryGetValue(subname, out var action))
            throw FormatException(InvalidCmdFormat);

        int expectedArgs = subname switch
        {
            "set" or "rename" => 3,
            "delete" => 2,
            "resetlimits" or "list" or "deleteall" => 1,
            _ => throw FormatException(InvalidCmdFormat)
        };

        if (!TrySplit(subcommand, expectedArgs, out string[] cmdParts))
            throw FormatException(InvalidCmdFormat);

        action(cmdParts);
    }

    public override (CmdResult, string) Execute(string sender, PrivilegeLevel privilege)
    {
        var executes = new Dictionary<string, Func<(CmdResult, string)>>
        {
            ["set"] = ExecuteSet,
            ["rename"] = ExecuteRename,
            ["delete"] = ExecuteDelete,
            ["resetlimits"] = ExecuteResetLimits,
            ["list"] = ExecuteList,
            ["deleteall"] = ExecuteDeleteAll
        };

        if (!executes.TryGetValue(_subname, out var execute))
        {
            return (CmdResult.Error, "Invalid subcommand.");
        }

        foreach (string nick in new[] { _username, _oldusername, _newusername })
        {
            if (nick != null && !UserRepository.Managable(nick))
                return (CmdResult.Error, $"Nickname '{nick}' is reserved by server.");
        }

        return execute();
    }

    private (CmdResult, string) ExecuteSet()
    {
        if (UserRepository.Managable(_username))
        {
            UserRepository.SetUserSync(_username, _password);

            return (CmdResult.Success,
                $"User '{_username}' set successfully.");
        }

        return (CmdResult.Error,
            $"Failed to set user '{_username}'.");
    }

    private (CmdResult, string) ExecuteRename()
    {
        if (UserRepository.Managable(_oldusername) && UserRepository.Managable(_newusername) && 
            UserRepository.Exists(_oldusername) && !UserRepository.Exists(_newusername))
        {
            UserRepository.RenameUser(_oldusername, _newusername);

            return (CmdResult.Success,
                $"User '{_oldusername}' renamed to '{_newusername}' successfully.");
        }

        return (CmdResult.Error,
            $"Failed to rename user '{_oldusername}' to '{_newusername}'.");
    }

    private (CmdResult, string) ExecuteDelete()
    {
        if (UserRepository.Managable(_username) &&
            UserRepository.Exists(_username))
        {
            UserRepository.DeleteUser(_username);

            return (CmdResult.Success,
                $"User '{_username}' deleted successfully.");
        }

        return (CmdResult.Error,
            $"Failed to delete user '{_username}'.");
    }

    private (CmdResult, string) ExecuteResetLimits()
    {
        Server.ResetLimiters();

        return (CmdResult.Success,
            "All socket limiters have been reset.");
    }

    private (CmdResult, string) ExecuteList()
    {
        IPEndPoint? EndpointOf(string nick)
        {
            JoinedPlayer? player = ConnectedPlayers.GetPlayer(nick);
            return player?.EndPoint;
        }

        string StateOf(string nick)
        {
            return ConnectedPlayers
                .StateOf(nick)
                .ToString()
                .ToUpperInvariant();
        }

        string NONE = PlayerState.None.ToString().ToUpperInvariant();

        IEnumerable<string> lines = UserRepository.AllUsernames()
            .OrderBy(nick => StateOf(nick) == NONE ? 1 : 0)
            .ThenBy(nick => nick)
            .Select(nick =>
            {
                IPEndPoint? endpoint = EndpointOf(nick);
                string state = StateOf(nick);

                return state != NONE ?
                    $"{nick} from {endpoint} is {state}." :
                    $"{nick} is OFFLINE.";
            });

        return (CmdResult.Raw,
            MakeRobustList("USER LIST", lines));
    }

    private (CmdResult, string) ExecuteDeleteAll()
    {
        List<string> allUsers = UserRepository.AllUsernames()
            .Where(nick => UserRepository.Managable(nick))
            .ToList();

        if (allUsers.Any(nick => ConnectedPlayers.IsConnected(nick)))
        {
            return (CmdResult.Error,
                "Cannot delete all users while some are still online.");
        }

        List<string> failed = new();
        foreach (string nick in allUsers)
        {
            if (UserRepository.Managable(nick))
            {
                UserRepository.DeleteUser(nick);
            }
            else
            {
                failed.Add(nick);
            }
        }

        if (!failed.Any())
        {
            return (CmdResult.Success,
                "All users deleted successfully.");
        }
        else
        {
            string listedFailed = string.Join(", ", failed.Select(nick => $"'{nick}'"));
            return (CmdResult.Error,
                $"Failed to delete the following users: {listedFailed}.");
        }
    }
}
