#nullable enable
using System;
using System.Collections.Generic;

namespace Larnix.Server.Data.Database;

internal class DbRecord
{
    private readonly Dictionary<string, object?> _data = new();

    public DbRecord(Dictionary<string, object?> data)
    {
        _data = data;
    }

    public T? Get<T>(string column, T? defaultValue = default)
    {
        if (!_data.TryGetValue(column, out object? value))
            throw new KeyNotFoundException($"Column \"{column}\" not found in record!");

        if (value is null || value == DBNull.Value)
            return defaultValue;

        Type targetType = typeof(T);
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return underlyingType.IsEnum
            ? (T)Enum.ToObject(underlyingType, value)
            : (T)Convert.ChangeType(value, underlyingType);
    }
}
