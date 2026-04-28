#nullable enable

namespace Larnix.Socket.Security.Keys;

internal class KeyEmpty : IKey
{
    private KeyEmpty() { }

    public static KeyEmpty Instance { get; } = new KeyEmpty(); // stateless = thread-safe

    public byte[] Encrypt(byte[] plaintext) => plaintext[..];
    public byte[] Decrypt(byte[] ciphertext) => ciphertext[..];

    public T CloneKey<T>() where T : IKey => (T)(IKey)this;
    public void Dispose() { }
}
