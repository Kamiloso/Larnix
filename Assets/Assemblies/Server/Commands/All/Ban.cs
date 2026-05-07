#nullable enable
using System;
using System.Collections.Generic;
using Larnix.Core;
using Larnix.Server.Entities;
using Larnix.Model;
using Larnix.Server.Network;
using Larnix.Model.Configs;

namespace Larnix.Server.Commands.All;

internal class Ban : BaseCmd
{
    public override PrivilegeLevel PrivilegeLevel => PrivilegeLevel.Console;
    public override string Pattern => $"{Name} <params...>";
    public override string Hint => $"Type 'help {Name}' for more information.";
    public override string ShortDescription => "Manages bans.";

    public override string LongDescription => ShortDescription + " Usage:\n" +
        "ban add <nickname/IP/CIDR> - Bans a nickname, an IP address or a network.\n" +
        "ban remove <entry> - Removes a ban entry.\n" +
        "ban list - Lists all ban entries.\n" +
        "ban clear - Clears the ban list.";

    private Config Config => GlobRef.Get<Config>();
    private IServer Server => GlobRef.Get<IServer>();
    private IServerInfo ServerInfo => GlobRef.Get<IServerInfo>();
    private IConnectedPlayers ConnectedPlayers => GlobRef.Get<IConnectedPlayers>();

    private string _subname = "";
    private string _target = "";

    public override void Inject(string command)
    {
        if (!TrySplitMin(command, 2, out string[] parts, out int length))
            throw FormatException(InvalidCmdFormat);

        string subname = parts[1];
        string subcommand = string.Join(' ', parts[1..]);
        _subname = subname;

        _target = "";

        var commands = new Dictionary<string, Action<string[]>>
        {
            ["add"] = args =>
            {
                _target = args[1];
            },
            ["remove"] = args =>
            {
                _target = args[1];
            },
            ["list"] = args => { },
            ["clear"] = args => { },
        };

        if (!commands.TryGetValue(subname, out var action))
            throw FormatException(InvalidCmdFormat);

        int expectedArgs = subname switch
        {
            "add" or "remove" => 2,
            "list" or "clear" => 1,
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
            ["add"] = ExecuteAdd,
            ["remove"] = ExecuteRemove,
            ["list"] = ExecuteList,
            ["clear"] = ExecuteClear,
        };

        if (!executes.TryGetValue(_subname, out var execute))
        {
            return (CmdResult.Error, "Invalid subcommand.");
        }

        return execute();
    }

    private (CmdResult, string) ExecuteAdd()
    {
        if (!Config.Administration_Banned.Contains(_target))
        {
            Config.Administration_Banned.Add(_target);
            BaseConfig.ToFile(ServerInfo.WorldPath, Common.ConfigFile, Config);

            if (ConnectedPlayers.IsConnected(_target))
            {
                // banning a player -> kick them immediately
                Server.KickRequest(_target);
            }

            return (CmdResult.Success,
                $"Successfully added '{_target}' to the ban list.");
        }

        return (CmdResult.Error,
            $"'{_target}' is already in the ban list.");
    }

    private (CmdResult, string) ExecuteRemove()
    {
        if (Config.Administration_Banned.Contains(_target))
        {
            Config.Administration_Banned.Remove(_target);
            BaseConfig.ToFile(ServerInfo.WorldPath, Common.ConfigFile, Config);

            return (CmdResult.Success,
                $"Successfully removed '{_target}' from the ban list.");
        }

        return (CmdResult.Error,
            $"'{_target}' is not in the ban list.");
    }

    private (CmdResult, string) ExecuteList()
    {
        return (CmdResult.Raw,
            MakeRobustList("BAN ENTRIES", "'", Config.Administration_Banned, "'"));
    }

    private (CmdResult, string) ExecuteClear()
    {
        Config.Administration_Banned.Clear();
        BaseConfig.ToFile(ServerInfo.WorldPath, Common.ConfigFile, Config);

        return (CmdResult.Success,
            "Successfully cleared the ban list.");
    }
}
