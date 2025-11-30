using System;
using System.Collections.Generic;

namespace Larnix.Socket.Backend
{
    public class UserManager
    {
        private IUserAPI UserAPI;

        public UserManager(IUserAPI userAPI)
        {
            UserAPI = userAPI;
        }

        public bool UserExists(string username)
        {
            return GetChallengeID(username) != 0;
        }

        public void AddUser(string username, string passwordHash)
        {
            UserData user = new UserData(username, passwordHash);
            UserAPI.SaveUserData(user, true);
        }

        public long GetUserID(string username)
        {
            UserData user = UserAPI.ReadUserData(username) ??
                throw new KeyNotFoundException($"Username {username} not found!");
            return user.UserID.Value;
        }

        public void ChangePassword(string username, string hashedPassword)
        {
            UserData? n_user = UserAPI.ReadUserData(username);
            if (n_user != null)
            {
                UserData user = n_user.Value;

                user.PasswordHash = hashedPassword;
                UserAPI.SaveUserData(user, false);
            }
            else
            {
                AddUser(username, hashedPassword);
            }
        }

        public string GetPasswordHash(string username)
        {
            UserData user = UserAPI.ReadUserData(username) ??
                throw new KeyNotFoundException($"Username {username} not found!");
            return user.PasswordHash;
        }

        internal long GetChallengeID(string username)
        {
            UserData? user = UserAPI.ReadUserData(username);
            return user?.ChallengeID ?? 0; // 0 --> no user
        }

        internal void IncrementChallengeID(string username)
        {
            UserData user = UserAPI.ReadUserData(username) ??
                throw new KeyNotFoundException($"Username {username} not found!");

            user.ChallengeID++;
            UserAPI.SaveUserData(user, false);
        }
    }
}
