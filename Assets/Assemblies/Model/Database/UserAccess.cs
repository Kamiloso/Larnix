#nullable enable
using Larnix.Model.Database.Connection;
using Larnix.Socket.Server.Users;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Model.Database;

public interface IUserAccess
{
    ulong NextUserUid();
    List<string> AllUsernames();
    void SaveUserData(QuickUser user);
    bool TryGetUserData(string username, out QuickUser? user);
    void DeleteUser(string username);
}

internal class UserAccess : IUserAccess
{
    private readonly IDbHandle _db;
    public UserAccess(IDbHandle db) => _db = db;

    private ulong _nextUid = 0;
    public ulong NextUserUid()
    {
        string cmd = @"
            SELECT max(uid) AS scalar FROM players;
        ";

        if (_nextUid == 0)
        {
            DbRecord record = _db.QuerySingle(cmd)!;

            long maxUid = record.Get<long>("scalar", 0);
            unchecked { _nextUid = (ulong)Math.Max(maxUid, 0) + 1; }
        }

        return ++_nextUid;
    }

    public List<string> AllUsernames()
    {
        string cmd = @"
            SELECT nickname FROM players
                WHERE nickname IS NOT NULL and nickname <> '';
        ";

        return _db.QueryList(cmd)
            .Select(record => record.Get<string>("nickname")!)
            .Where(name => !string.IsNullOrEmpty(name)) // ignore removed
            .ToList();
    }

    public void SaveUserData(QuickUser user)
    {
        if (string.IsNullOrEmpty(user.Nickname))
            throw new ArgumentException("Username cannot be empty", nameof(user));

        string cmd_0 = @"
            SELECT count(nickname) AS amount
                FROM players
                WHERE uid <> $p1 and nickname = $p2;
        ";

        if (_db.QuerySingle(cmd_0, user.Nickname, user.Uid)!.Get<long>("amount") > 0)
            throw new InvalidOperationException("Username collision detected");

        string cmd = @"
            INSERT INTO players (uid, nickname, password_hash, challenge_id)
            VALUES ($p1, $p2, $p3, $p4)
            ON CONFLICT(uid) DO UPDATE SET
                nickname = excluded.nickname,
                password_hash = excluded.password_hash,
                challenge_id = excluded.challenge_id;
        ";

        _db.Execute(cmd,
            user.Uid,
            user.Nickname,
            user.PasswordHash,
            user.ChallengeId
        );
    }

    public bool TryGetUserData(string username, out QuickUser? user)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        string cmd = @"
            SELECT * FROM players
                WHERE nickname = $p1;
        ";

        DbRecord? record = _db.QuerySingle(cmd, username);

        if (record is not null)
        {
            user = new QuickUser(
                Uid: record.Get<long>("uid"),
                Nickname: record.Get<string>("nickname") ?? "",
                PasswordHash: record.Get<string>("password_hash") ?? "",
                ChallengeId: record.Get<long>("challenge_id")
                );

            return true;
        }

        user = default;
        return false;
    }

    public void DeleteUser(string username)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        _db.AsTransaction(() =>
        {
            string cmd = @"
                UPDATE players
                    SET nickname = '', password_hash = '', challenge_id = 0
                    WHERE nickname = $p1;

                DELETE FROM players
                    WHERE uid <> (
                        SELECT max(uid) FROM players
                    );
            ";

            _db.Execute(cmd, username);
        });
    }
}