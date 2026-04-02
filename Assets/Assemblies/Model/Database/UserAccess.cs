#nullable enable
using Larnix.Model.Database.Connection;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Model.Database;

public interface IUserAccess
{
    List<string> AllUsernames();
    void SaveUserData(UserData userData);
    bool TryGetUserData(string username, out UserData? userData);
}

internal class UserAccess : IUserAccess
{
    private readonly IDbHandle _db;
    public UserAccess(IDbHandle db) => _db = db;

    public List<string> AllUsernames()
    {
        string cmd = @"
            SELECT nickname FROM players
                WHERE nickname IS NOT NULL and nickname <> '';
        ";

        return _db.QueryList(cmd)
            .Select(record => record.Get<string>("nickname")!)
            .ToList();
    }

    public void SaveUserData(UserData userData)
    {
        if (userData.HasUID)
        {
            string cmd = @"
                UPDATE players
                SET nickname = $p2,
                    password_hash = $p3,
                    challenge_id = $p4
                WHERE uid = $p1;
            ";

            _db.Execute(cmd,
                userData.UID,
                userData.Username,
                userData.PasswordHash,
                userData.ChallengeID
            );
        }
        else
        {
            string cmd = @"
                INSERT INTO players (nickname, password_hash, challenge_id)
                    VALUES ($p1, $p2, $p3);
            ";

            _db.Execute(cmd,
                userData.Username,
                userData.PasswordHash,
                userData.ChallengeID
            );
        }
    }

    public bool TryGetUserData(string username, out UserData? userData)
    {
        string cmd = @"
            SELECT * FROM players
                WHERE nickname = $p1;
        ";

        DbRecord? record = _db.QuerySingle(cmd, username);

        if (record is not null)
        {
            userData = UserData.FromRecord(
                uid: record.Get<long>("uid"),
                username: record.Get<string>("nickname") ?? "",
                passwordHash: record.Get<string>("password_hash") ?? "",
                challengeID: record.Get<long>("challenge_id")
            );
            return true;
        }

        userData = default;
        return false;
    }
}