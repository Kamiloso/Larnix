using System.Collections;
using System.Collections.Generic;

namespace Larnix.Core
{
    public interface IEncryptionKey
    {
        public abstract byte[] Encrypt(byte[] plaintext);
        public abstract byte[] Decrypt(byte[] ciphertext);
    }
}
