#nullable enable

using Larnix.Socket.Security.Encryption;

namespace Larnix.Socket.Security.Encryption;

internal class EmptyKey : IFullEncryptor
{
    private EmptyKey() { }

    public static EmptyKey Instance { get; } = new EmptyKey();

    public byte[] Encrypt(byte[] plaintext)
    {
        return plaintext[..];
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        return ciphertext[..];
    }
}
