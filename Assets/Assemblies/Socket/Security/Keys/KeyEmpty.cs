#nullable enable

using System.Threading;

namespace Larnix.Socket.Security.Keys;

internal class KeyEmpty : IEncryptionKey
{
    private static readonly ThreadLocal<KeyEmpty> _keyProvider = new(() => new());

    private KeyEmpty() { }

    public static KeyEmpty Instance => _keyProvider.Value;

    public byte[] Encrypt(byte[] plaintext) => plaintext[..];
    public byte[] Decrypt(byte[] ciphertext) => ciphertext[..];
}
