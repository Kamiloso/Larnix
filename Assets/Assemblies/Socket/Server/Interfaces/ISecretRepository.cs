#nullable enable

namespace Larnix.Socket.Server.Interfaces;

public interface ISecretRepository
{
    void StoreSecret(string key, string secret);
    string? ReadSecret(string key);
}
