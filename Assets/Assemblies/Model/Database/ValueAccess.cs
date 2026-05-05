#nullable enable
using Larnix.Model.Database.Connection;
using System;

namespace Larnix.Model.Database;

public interface IValueAccess
{
    void Put(string key, long value);
    long? Get(string key);
    long GetOrInsert(string key, Func<long> fallback);

    void PutString(string key, string value);
    string? GetString(string key);
    string GetStringOrInsert(string key, Func<string> fallback);
}

internal class ValueAccess : IValueAccess
{
    private readonly IDbHandle _db;
    public ValueAccess(IDbHandle db) => _db = db;

    public void Put(string key, long value)
    {
        PutString(key, value.ToString());
    }

    public long? Get(string key)
    {
        string? strValue = GetString(key);
        if (strValue != null && long.TryParse(strValue, out long value))
        {
            return value;
        }
        return null;
    }

    public long GetOrInsert(string key, Func<long> fallback)
    {
        long? value = Get(key);
        if (value is null)
        {
            Put(key, fallback());
            return Get(key)!.Value;
        }
        return value.Value;
    }

    public void PutString(string key, string value)
    {
        string cmd = @"
            INSERT OR REPLACE INTO key_values
                (key, value)
                VALUES ($p1, $p2);
        ";

        _db.Execute(cmd, key, value);
    }

    public string? GetString(string key)
    {
        string cmd = @"
            SELECT value
                FROM key_values
                WHERE key = $p1;
        ";

        DbRecord? record = _db.QuerySingle(cmd, key);

        if (record is not null)
        {
            return record.Get<string>("value");
        }

        return null;
    }

    public string GetStringOrInsert(string key, Func<string> fallback)
    {
        string? value = GetString(key);
        if (value is null)
        {
            PutString(key, fallback());
            return GetString(key)!;
        }
        return value;
    }
}
