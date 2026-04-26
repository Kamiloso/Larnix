#nullable enable

namespace Larnix.Socket.Backend.Utility;

public interface ISecretRepository
{
    void StoreSecret(string key, string secret);
    string? ReadSecret(string key);
}
