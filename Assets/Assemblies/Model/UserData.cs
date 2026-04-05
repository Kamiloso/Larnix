#nullable enable
using System;
using System.Collections.Generic;

namespace Larnix.Model;

public class UserData
{
    public long Uid { get; }
    public string Username { get; }
    public string PasswordHash { get; }
    public long ChallengeID { get; }

    public bool HasUID => Uid != 0;

    private UserData(long uid, string username, string passwordHash, long challengeID)
    {
        Uid = uid;
        Username = username;
        PasswordHash = passwordHash;
        ChallengeID = challengeID;
    }

    public static UserData CreateNew(string username, string passwordHash)
    {
        const long INIT_CHALLENGE_ID = 1000; // Arbitrary non-zero starting value
        return new UserData(
            0, username, passwordHash, INIT_CHALLENGE_ID);
    }

    public static UserData FromRecord(long uid, string username, string passwordHash, long challengeID)
    {
        if (uid == 0)
            throw new ArgumentException("UID cannot be zero for an existing user.", nameof(uid));

        return new UserData(uid, username, passwordHash, challengeID);
    }

    public UserData AfterLogin()
    {
        return new UserData(
            Uid, Username, PasswordHash, ChallengeID + 1);
    }

    public UserData AfterPasswordChange(string newPasswordHash)
    {
        return new UserData(
            Uid, Username, newPasswordHash, ChallengeID);
    }

    public UserData AfterUsernameChange(string newUsername)
    {
        return new UserData(
            Uid, newUsername, PasswordHash, ChallengeID);
    }

    public UserData WithoutUsername()
    {
        return new UserData(
            Uid, string.Empty, string.Empty, ChallengeID);
    }
}
