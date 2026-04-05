#nullable enable
using System;
using Larnix.Core;
using Larnix.Model;
using Larnix.Model.Database;

namespace Larnix.Server.Data;

internal interface IUserRepository
{
    ulong GetUserUid(string nickname);
}

internal class UserRepository : IUserRepository
{
    private IDbControl Db => GlobRef.Get<IDbControl>();

    public ulong GetUserUid(string nickname)
    {
        Db.Users.TryGetUserData(nickname, out UserData? user);
        return (ulong)(user?.Uid ?? throw new InvalidOperationException(
            $"User '{nickname}' does not exist in the database."
            ));
    }
}
