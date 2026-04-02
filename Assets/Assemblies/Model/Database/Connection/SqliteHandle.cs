#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Larnix.Model.Database.Connection;

public class SqliteHandle : IDbHandle
{
    private SqliteConnection Connection { get; init; }
    private SqliteTransaction? Transaction { get; set; }

    private int _transactionDepth = 0;

    private bool _disposed = false;

    private static bool _initialized = false;
    private static readonly object _initLock = new();

    public SqliteHandle(string path, string filename)
    {
        lock (_initLock)
        {
            if (!_initialized)
            {
                SQLitePCL.Batteries_V2.Init();
                _initialized = true;
            }
        }

        string normalizedPath = Path.GetFullPath(path);
        string fullDatabasePath = Path.Combine(normalizedPath, filename);

        if (!Directory.Exists(normalizedPath))
        {
            Directory.CreateDirectory(normalizedPath);
        }

        SqliteConnectionStringBuilder csb = new()
        {
            DataSource = fullDatabasePath,
            Pooling = false,
        };

        Connection = new SqliteConnection(csb.ConnectionString);
        Connection.Open();

        Execute(@"
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=NORMAL;
            PRAGMA temp_store=MEMORY;
            ");
    }

    public void Execute(string query, params object[] parameters)
    {
        using SqliteCommand cmd = MakeCommand(query, parameters);

        cmd.ExecuteNonQuery();
    }

    public DbRecord? QuerySingle(string query, params object[] parameters)
    {
        using SqliteCommand cmd = MakeCommand(query, parameters);
        using SqliteDataReader reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            Dictionary<string, object?> data = new();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                data[reader.GetName(i)] = reader.GetValue(i);
            }

            return new DbRecord(data);
        }

        return null;
    }

    public IList<DbRecord> QueryList(string query, params object[] parameters)
    {
        using SqliteCommand cmd = MakeCommand(query, parameters);
        using SqliteDataReader reader = cmd.ExecuteReader();

        List<DbRecord> records = new();

        while (reader.Read())
        {
            Dictionary<string, object?> data = new();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                data[reader.GetName(i)] = reader.GetValue(i);
            }

            records.Add(new DbRecord(data));
        }

        return records;
    }

    private SqliteCommand MakeCommand(string query, params object[] parameters)
    {
        SqliteCommand cmd = Connection.CreateCommand();

        cmd.CommandText = query;
        cmd.Transaction = Transaction;

        for (int i = 1; i <= parameters.Length; i++)
        {
            cmd.Parameters.AddWithValue($"$p{i}", parameters[i - 1]);
        }

        return cmd;
    }

    public void BeginTransaction()
    {
        if (_transactionDepth == 0)
        {
            Transaction = Connection.BeginTransaction();
        }
        else
        {
            Execute($"SAVEPOINT Larnix_SP_{_transactionDepth}");
        }

        _transactionDepth++;
    }

    public void CommitTransaction()
    {
        if (_transactionDepth == 0)
            throw new InvalidOperationException("No active transaction to commit!");

        _transactionDepth--;

        if (_transactionDepth == 0)
        {
            Transaction!.Commit();
            Transaction.Dispose();
            Transaction = null;
        }
        else
        {
            Execute($"RELEASE SAVEPOINT Larnix_SP_{_transactionDepth}");
        }
    }

    public void RollbackTransaction()
    {
        if (_transactionDepth == 0)
            throw new InvalidOperationException("No active transaction to rollback!");

        _transactionDepth--;

        if (_transactionDepth == 0)
        {
            Transaction!.Rollback();
            Transaction.Dispose();
            Transaction = null;
        }
        else
        {
            Execute($"ROLLBACK TO SAVEPOINT Larnix_SP_{_transactionDepth}");
            Execute($"RELEASE SAVEPOINT Larnix_SP_{_transactionDepth}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Transaction != null)
        {
            try { Transaction.Rollback(); } catch { }
            Transaction.Dispose();
            Transaction = null;
        }

        Connection?.Dispose();
    }
}