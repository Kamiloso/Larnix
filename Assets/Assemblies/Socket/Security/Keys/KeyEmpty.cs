using System.Collections;
using System.Collections.Generic;

namespace Larnix.Socket.Security.Keys
{
    internal class KeyEmpty : IEncryptionKey
    {
        private static KeyEmpty _instance;

        private KeyEmpty() { }

        public static KeyEmpty GetInstance()
        {
            if (_instance == null)
                _instance = new KeyEmpty();

            return _instance;
        }

        public byte[] Encrypt(byte[] plaintext) => plaintext;
        public byte[] Decrypt(byte[] ciphertext) => ciphertext;
    }
}
