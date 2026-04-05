#nullable enable
using Larnix.Core;
using Larnix.Server.Data;
using Larnix.Server.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Server.Entities;

internal interface IConnectedPlayers
{
    public IEnumerable<string> AllPlayers { get; }

    void JoinPlayer(string nickname);
    void UpdatePlayer(string nickname, PlayerUpdate msg);
    void RespawnPlayer(string nickname);
    void DisconnectPlayer(string nickname);

    JoinedPlayer? GetPlayer(string nickname);
    JoinedPlayer this[string nickname] => GetPlayer(nickname) ?? throw new KeyNotFoundException(
        $"Player with nickname {nickname} is not connected.");

    string NicknameByUid(ulong uid);
    ulong UidByNickname(string nickname);

    PlayerState StateOf(string nickname) => GetPlayer(nickname)?.State ?? PlayerState.None;
    bool IsConnected(string nickname) => StateOf(nickname) != PlayerState.None;
    bool IsAlive(string nickname) => StateOf(nickname) == PlayerState.Alive;
}

internal class ConnectedPlayers : IConnectedPlayers
{
    private readonly Dictionary<string, JoinedPlayer> _players = new();
    private readonly Dictionary<ulong, string> _uidToNickname = new();

    public IEnumerable<string> AllPlayers => _players.Keys.ToList();

    private IUserRepository UserRepository => GlobRef.Get<IUserRepository>();
    private IEntityControllers EntityControllers => GlobRef.Get<IEntityControllers>();

    public void JoinPlayer(string nickname)
    {
        if (_players.ContainsKey(nickname))
            throw new InvalidOperationException($"Player with nickname {nickname} is already connected.");

        ulong uid = UserRepository.GetUserUid(nickname);
        var player = new JoinedPlayer(uid, nickname);

        _players[nickname] = player;
        _uidToNickname[uid] = nickname;

        EntityControllers.LoadPlayerController(uid, nickname);
    }

    public void UpdatePlayer(string nickname, PlayerUpdate msg)
    {
        if (!_players.TryGetValue(nickname, out var player))
            throw new InvalidOperationException($"Player with nickname {nickname} is not connected.");

        player.Update(msg);
    }

    public void RespawnPlayer(string nickname)
    {
        if (!_players.TryGetValue(nickname, out var player))
            throw new InvalidOperationException($"Player with nickname {nickname} is not connected.");

        if (player.State != PlayerState.Dead)
            throw new InvalidOperationException($"Player with nickname {nickname} is not dead and cannot be respawned.");

        EntityControllers.LoadPlayerController(player.Uid, nickname);
    }

    public void DisconnectPlayer(string nickname)
    {
        if (!_players.TryGetValue(nickname, out var player))
            throw new InvalidOperationException($"Player with nickname {nickname} is not connected.");

        EntityControllers.UnloadController(player.Uid);

        _players.Remove(nickname);
        _uidToNickname.Remove(player.Uid);
    }

    public JoinedPlayer? GetPlayer(string nickname)
    {
        return _players.TryGetValue(nickname, out var player) ? player : null;
    }

    public string NicknameByUid(ulong uid)
    {
        if (!_uidToNickname.TryGetValue(uid, out var nickname))
            throw new KeyNotFoundException($"Player with UID {uid} is not connected.");

        return nickname;
    }

    public ulong UidByNickname(string nickname)
    {
        if (!_players.TryGetValue(nickname, out var player))
            throw new KeyNotFoundException($"Player with nickname {nickname} is not connected.");

        return player.Uid;
    }
}
