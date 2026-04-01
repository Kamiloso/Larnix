#nullable enable
using System;
using System.Collections.Generic;

namespace Larnix.GameCore.DbStructs
{
    public interface IDbUserAccess
    {
        void SaveUserData(UserData userData);
        bool TryGetUserData(string username, out UserData? userData);
        List<string> AllUsernames();
    }

    public class UserData
    {
        public long UID { get; }
        public string Username { get; }
        public string PasswordHash { get; }
        public long ChallengeID { get; }

        public bool HasUID => UID != 0;

        private UserData(long uid, string username, string passwordHash, long challengeID)
        {
            UID = uid;
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
                UID, Username, PasswordHash, ChallengeID + 1);
        }

        public UserData AfterPasswordChange(string newPasswordHash)
        {
            return new UserData(
                UID, Username, newPasswordHash, ChallengeID);
        }

        public UserData AfterUsernameChange(string newUsername)
        {
            return new UserData(
                UID, newUsername, PasswordHash, ChallengeID);
        }

        public UserData WithoutUsername()
        {
            return new UserData(
                UID, string.Empty, string.Empty, ChallengeID);
        }
    }
}
