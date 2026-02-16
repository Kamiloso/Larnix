using System.Collections;
using System.Collections.Generic;

namespace Larnix.Socket
{
    public interface IEncryptionKey
    {
        byte[] Encrypt(byte[] plaintext);
        byte[] Decrypt(byte[] ciphertext);
    }
}
