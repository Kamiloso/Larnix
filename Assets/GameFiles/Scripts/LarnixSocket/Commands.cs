using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larnix.Socket
{
    public static class Commands
    {
        public enum Name : byte
        {
            None,
            AllowConnection,
            Stop,
        }

        public class AllowConnection
        {
            // === [ 64 bytes ] ===
            // | nickname        | 32 bytes | string
            // | verification ID | 16 bytes | binary
            // | AES key         | 16 bytes | binary

            const int COMMAND_SIZE = 64;

            public string nickname { get; private set; }
            public byte[] verification { get; private set; }
            public byte[] AES { get; private set; }

            public AllowConnection(string _nickname, byte[] _verification, byte[] _AES)
            {
                nickname = _nickname;
                verification = _verification;
                AES = _AES;

                CheckContents();
            }

            public AllowConnection(byte[] bytes)
            {
                if (bytes == null || bytes.Length != COMMAND_SIZE)
                    throw new Exception("Byte length must be " + COMMAND_SIZE + ".");

                byte[] bytes_nickname = bytes[0..32];
                byte[] bytes_verification = bytes[32..48];
                byte[] bytes_AES = bytes[48..64];

                nickname = System.Text.Encoding.Unicode.GetString(bytes_nickname).TrimEnd('\0'); ;
                verification = bytes_verification;
                AES = bytes_AES;

                CheckContents();
            }

            public Packet GetPacket()
            {
                byte[] bytes = new byte[64];
                byte[] nameBytes = System.Text.Encoding.Unicode.GetBytes(nickname);

                Array.Copy(nameBytes, 0, bytes, 0, nameBytes.Length);
                Array.Copy(verification, 0, bytes, 32, 16);
                Array.Copy(AES, 0, bytes, 48, 16);

                return new Packet((byte)Name.AllowConnection, 0, bytes);
            }

            private void CheckContents()
            {
                if (nickname.Length < 3)
                    throw new Exception("Min nickname length is 3.");

                if (nickname.Length > 16)
                    throw new Exception("Max nickname length is 16.");

                if (!nickname.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                    throw new Exception("Nickname contains unwanted characters.");

                if (verification.Length != 16)
                    throw new Exception("Verification token length must be 16.");

                if (AES.Length != 16)
                    throw new Exception("AES key length must be 16.");
            }
        }
    }
}
