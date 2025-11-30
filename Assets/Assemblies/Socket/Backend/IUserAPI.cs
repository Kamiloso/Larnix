using Larnix.Core.Utils;
using Larnix.Socket.Backend;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Socket
{
    public interface IUserAPI
    {
        public void SaveUserData(UserData userData, bool create);
        public UserData? ReadUserData(string nickname);
    }

    public struct UserData
    {
        public long? UserID;
        public string Username;
        public string PasswordHash;
        public long ChallengeID;

        public UserData(string username, string passwordHash)
        {
            UserID = null;
            Username = username;
            PasswordHash = passwordHash;
            ChallengeID = 1;
        }
    }
}
