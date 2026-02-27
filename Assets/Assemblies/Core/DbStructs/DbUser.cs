using System;
using System.Collections.Generic;

namespace Larnix.Core.DbStructs
{
    public interface IDbUserAccess
    {
        void SaveUserData(DbUser userData);
        bool TryGetUserData(string username, out DbUser userData);
        IEnumerable<string> AllUsernames();
    }

    public class DbUser
    {
        public long UID { get; }
        public string Username { get; }
        public string PasswordHash { get; }
        public long ChallengeID { get; }

        public bool HasUID => UID != 0;

        private DbUser(long uid, string username, string passwordHash, long challengeID)
        {
            UID = uid;
            Username = username;
            PasswordHash = passwordHash;
            ChallengeID = challengeID;
        }

        public static DbUser CreateNew(string username, string passwordHash)
        {
            const long INIT_CHALLENGE_ID = 1000; // Arbitrary non-zero starting value
            return new DbUser(
                0, username, passwordHash, INIT_CHALLENGE_ID);
        }

        public static DbUser FromRecord(long uid, string username, string passwordHash, long challengeID)
        {
            if (uid == 0)
                throw new ArgumentException("UID cannot be zero for an existing user.", nameof(uid));
            
            return new DbUser(uid, username, passwordHash, challengeID);
        }

        public DbUser AfterLogin()
        {
            return new DbUser(
                UID, Username, PasswordHash, ChallengeID + 1);
        }

        public DbUser AfterPasswordChange(string newPasswordHash)
        {
            return new DbUser(
                UID, Username, newPasswordHash, ChallengeID);
        }

        public DbUser AfterUsernameChange(string newUsername)
        {
            return new DbUser(
                UID, newUsername, PasswordHash, ChallengeID);
        }

        public DbUser WithoutUsername()
        {
            return new DbUser(
                UID, string.Empty, string.Empty, ChallengeID);
        }
    }
}
