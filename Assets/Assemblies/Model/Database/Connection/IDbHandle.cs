#nullable enable
using System;
using System.Collections.Generic;

namespace Larnix.Model.Database.Connection;

public interface IDbHandle : IDisposable
{
    void Execute(string query, params object[] parameters);
    DbRecord? QuerySingle(string query, params object[] parameters);
    IList<DbRecord> QueryList(string query, params object[] parameters);

    void BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();

    public void AsTransaction(Action action)
    {
        BeginTransaction();
        try
        {
            action();
            CommitTransaction();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
    }
}
