#nullable enable
using Larnix.Core;
using Larnix.Model.Database;
using Larnix.Socket.Server.Interfaces;

namespace Larnix.Server.Repositories;

internal interface IValueRepository : ISecretRepository
{
    void StoreValue(string key, long value);
    long? ReadValue(string key);

    void StoreString(string key, string value);
    string? ReadString(string key);
}

internal class ValueRepository : IValueRepository
{
    // WARNING: No "secret" features are included here. It's just a prefix / namespace convention.
    private const string NPF = "$"; // normal prefix
    private const string SPF = "#"; // secret prefix (not actually secret)

    private IDbControl Db => GlobRef.Get<IDbControl>();

    // Long values
    public void StoreValue(string key, long value) => Db.Values.Put($"{NPF}{key}", value);
    public long? ReadValue(string key) => Db.Values.Get($"{NPF}{key}");

    // String values
    public void StoreString(string key, string value) => Db.Values.PutString($"{NPF}{key}", value);
    public string? ReadString(string key) => Db.Values.GetString($"{NPF}{key}");

    // String values (secret mode)
    public void StoreSecret(string key, string value) => Db.Values.PutString($"{SPF}{key}", value);
    public string? ReadSecret(string key) => Db.Values.GetString($"{SPF}{key}");
}
