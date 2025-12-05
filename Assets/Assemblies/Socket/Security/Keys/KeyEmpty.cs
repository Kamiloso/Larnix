using System.Collections;
using System.Collections.Generic;

namespace Larnix.Socket.Security.Keys
{
    internal class KeyEmpty : IEncryptionKey
    {
        public KeyEmpty() { }

        public byte[] Encrypt(byte[] plaintext) => plaintext;
        public byte[] Decrypt(byte[] ciphertext) => ciphertext;
    }
}
