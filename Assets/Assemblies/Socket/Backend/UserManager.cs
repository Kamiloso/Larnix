using System.Collections;
using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;

namespace Socket.Backend
{
    public class UserManager
    {
        private readonly QuickServer Server;
        private readonly string DataPath;
        private readonly string AccountsPath;
        private readonly long Pepper;

        internal UserManager(QuickServer server, string dataPath)
        {
            Server = server;
            DataPath = dataPath;
            AccountsPath = Path.Combine(DataPath, "Users");

            byte[] pepperBytes = FileManager.ReadBinary(DataPath, "pepper.bin");
            if (pepperBytes?.Length == sizeof(long)) // read pepper
            {
                Pepper = EndianUnsafe.FromBytes<long>(pepperBytes);
            }
            else // generate pepper
            {
                Pepper = Processing.KeyObtainer.GetSecureLong();
                pepperBytes = EndianUnsafe.GetBytes(Pepper);
                FileManager.WriteBinary(DataPath, "pepper.bin", pepperBytes);
            }
        }

        // ===== ACCOUNT MANAGEMENT =====

        private void SaveUserData(UserData userData)
        {
            string username = userData.Username;

            ushort hashID = ContinerID(username);
            string filename = ContainerName(username, hashID);
            byte[] bytes = FileManager.ReadBinary(AccountsPath, filename) ?? new byte[0];

            int size = UserData.SIZE;
            
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                List<long> reservedIDs = new(bytes.Length / size);
                bool inserted = false;

                for (int i = 0; i + size <= bytes.Length; i += size)
                {
                    UserData element = new UserData(bytes, i);
                    if (element.Username != username)
                    {
                        writer.Write(element.GetBytes());
                    }
                    else
                    {
                        userData.UserID = element.UserID;
                        writer.Write(userData.GetBytes());
                        inserted = true;
                    }

                    if (!inserted)
                    {
                        reservedIDs.Add(element.UserID);
                    }
                }

                if (!inserted)
                {
                    do
                    {
                        userData.UserID = (Processing.KeyObtainer.GetSecureLong() & 0x7F_FF_FF_FF_FF_FF_00_00) | (ushort)hashID;
                    }
                    while (reservedIDs.Contains(userData.UserID) || userData.UserID == 0);

                    writer.Write(userData.GetBytes());
                    inserted = true;
                }

                bytes = ms.ToArray();
            }

            FileManager.WriteBinary(AccountsPath, filename, bytes);
        }

        private UserData ReadUserData(string username)
        {
            string filename = ContainerName(username);
            byte[] bytes = FileManager.ReadBinary(AccountsPath, filename);

            if (bytes != null)
            {
                int size = UserData.SIZE;
                for (int i = 0; i + size <= bytes.Length; i += size)
                {
                    UserData userData = new UserData(bytes, i);
                    if (userData.Username == username)
                        return userData;
                }
            }

            return null;
        }

        private string ContainerName(string username, ushort? hashID = null)
        {
            return "users_" + (hashID == null ? ContinerID(username) : hashID) + ".bin";
        }

        private ushort ContinerID(string username)
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes<String32>(username),
                EndianUnsafe.GetBytes(Pepper)
                );

            using SHA256 sha256 = SHA256.Create();
            byte[] result = sha256.ComputeHash(bytes);

            return EndianUnsafe.FromBytes<ushort>(result);
        }

        // ===== INTERNAL / PUBLIC API =====

        public bool UserExists(string username)
        {
            return GetChallengeID(username) != 0;
        }

        public void AddUser(string username, string hashedPassword)
        {
            UserData user = new UserData(username, hashedPassword);
            SaveUserData(user);
        }

        public long GetUserID(string username)
        {
            UserData user = ReadUserData(username) ?? throw new KeyNotFoundException($"Username {username} not found!");
            return user.UserID;
        }

        public void ChangePassword(string username, string hashedPassword)
        {
            UserData user = ReadUserData(username);
            if (user != null)
            {
                user.PasswordHash = hashedPassword;
                SaveUserData(user);
            }
            else
            {
                AddUser(username, hashedPassword);
            }
        }

        public string GetPasswordHash(string username)
        {
            UserData user = ReadUserData(username) ?? throw new KeyNotFoundException($"Username {username} not found!");
            return user.PasswordHash;
        }

        internal long GetChallengeID(string username)
        {
            UserData user = ReadUserData(username);
            return user?.ChallengeID ?? 0; // 0 --> no user
        }

        internal void IncrementChallengeID(string username)
        {
            UserData user = ReadUserData(username) ?? throw new KeyNotFoundException($"Username {username} not found!");
            user.ChallengeID++;
            SaveUserData(user);
        }
    }
}
