using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Socket.Security.Keys
{
    internal interface IEncryptionKey
    {
        public abstract byte[] Encrypt(byte[] plaintext);
        public abstract byte[] Decrypt(byte[] ciphertext);
    }
}
