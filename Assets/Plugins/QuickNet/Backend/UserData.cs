using System;
using System.Collections;
using System.Collections.Generic;

namespace QuickNet
{
    internal class UserData
    {
        internal const int SIZE = 8 + 32 + 256 + 8;

        internal long UserID;
        internal String32 Username; // 16 chars
        internal String256 PasswordHash; // 128 chars
        internal long ChallengeID;

        internal UserData(string username, string passwordHash)
        {
            Username = username;
            PasswordHash = passwordHash;
            ChallengeID = 1;
        }

        internal UserData(byte[] bytes, int offset = 0)
        {
            if (bytes == null || bytes.Length - offset < SIZE)
                throw new ArgumentException("Cannot deserialize UserData! Too small array.");

            UserID = EndianUnsafe.FromBytes<long>(bytes, 0 + offset);
            Username = EndianUnsafe.FromBytes<String32>(bytes, 8 + offset);
            PasswordHash = EndianUnsafe.FromBytes<String256>(bytes, 40 + offset);
            ChallengeID = EndianUnsafe.FromBytes<long>(bytes, 296 + offset);
        }

        internal byte[] GetBytes()
        {
            return ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(UserID),
                EndianUnsafe.GetBytes(Username),
                EndianUnsafe.GetBytes(PasswordHash),
                EndianUnsafe.GetBytes(ChallengeID)
                );
        }
    }
}
