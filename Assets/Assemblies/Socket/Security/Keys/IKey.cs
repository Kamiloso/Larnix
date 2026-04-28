#nullable enable
using System;

namespace Larnix.Socket.Security.Keys;

internal interface IKey : IDisposable
{
    byte[] Encrypt(byte[] plaintext);
    byte[] Decrypt(byte[] ciphertext);
    T CloneKey<T>() where T : IKey;
    IKey CloneKey() => CloneKey<IKey>();
}
