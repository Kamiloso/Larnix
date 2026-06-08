#nullable enable

namespace Larnix.Socket.Security.Encryption;

internal interface IFullEncryptor : IEncryptor, IDecryptor { }
internal interface IEncryptor { byte[] Encrypt(byte[] plaintext); }
internal interface IDecryptor { byte[] Decrypt(byte[] ciphertext); }
