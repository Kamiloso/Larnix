using System.Collections;
using System.Collections.Generic;

namespace Larnix.Core
{
    public interface IEncryptionKey
    {
        byte[] Encrypt(byte[] plaintext);
        byte[] Decrypt(byte[] ciphertext);
    }
}
