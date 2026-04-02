#nullable enable
using Larnix.Model.Database.Connection;
using System;

namespace Larnix.Model.Database;

public interface IValueAccess
{
    void Put(string key, long value);
    long? Get(string key);

    long GetOrPut(string key, Func<long> valueFactory)
    {
        long? value = Get(key);
        if (value is null)
        {
            value = valueFactory();
            Put(key, value.Value);
        }
        return value.Value;
    }
}

internal class ValueAccess : IValueAccess
{
    private readonly IDbHandle _db;
    public ValueAccess(IDbHandle db) => _db = db;

    public void Put(string key, long value)
    {
        string cmd = @"
            INSERT OR REPLACE INTO key_values
                (key, value)
                VALUES ($p1, $p2);
        ";

        _db.Execute(cmd, key, value);
    }

    public long? Get(string key)
    {
        string cmd = @"
            SELECT value
                FROM key_values
                WHERE key = $p1;
        ";

        DbRecord? record = _db.QuerySingle(cmd, key);

        if (record is not null)
        {
            return record.Get<long>("value");
        }

        return null;
    }
}