#nullable enable
using Larnix.Core;
using Larnix.Model;
using Larnix.Model.Database;
using Larnix.Server.Data;
using Larnix.Socket.Server.Interfaces;
using Larnix.Socket.Server.Users;
using System.Collections.Generic;

namespace Larnix.Server.Repositories;

internal interface IUserRepository : IQuickUserRepository
{
    public List<string> AllUsernames();
    void SetUserSync(string nickname, string password);
    bool Managable(string nickname);
    bool Exists(string nickname);
    void DeleteUser(string nickname);
    void RenameUser(string oldNickname, string newNickname);
}

internal class UserRepository : IUserRepository
{
    private IWorldMetaManager WorldMetaManager => GlobRef.Get<IWorldMetaManager>();
    private IPasswordHasher PasswordHasher => GlobRef.Get<IPasswordHasher>();
    private IDbControl Db => GlobRef.Get<IDbControl>();

    public List<string> AllUsernames()
    {
        return Db.Users.AllUsernames();
    }

    public long NextFreeUid()
    {
        return (long)Db.Users.NextUserUid();
    }

    public void SaveUser(QuickUser user)
    {
        Db.Users.SaveUserData(user);
    }

    public QuickUser? FindByNickname(string nickname)
    {
        return Db.Users.TryGetUserData(nickname, out QuickUser? user) ? user : null;
    }

    public void SetUserSync(string nickname, string password)
    {
        QuickUser? user = FindByNickname(nickname);
        string passHash = PasswordHasher.HashPassword(password);

        SaveUser(user != null
            ? user.AfterPasswordChange(passHash)
            : QuickUser.CreateAccount(
                uid: NextFreeUid(),
                nickname: nickname,
                passwordHash: passHash
                ));
    }

    public bool Managable(string nickname)
    {
        return nickname != WorldMetaManager.HostNickname &&
               nickname != GameInfo.ReservedNickname;
    }

    public bool Exists(string nickname)
    {
        return FindByNickname(nickname) != null;
    }

    public void DeleteUser(string nickname)
    {
        Db.Users.DeleteUser(nickname);
    }

    public void RenameUser(string oldNickname, string newNickname)
    {
        QuickUser user = FindByNickname(oldNickname)
            ?? throw new KeyNotFoundException($"User with nickname '{oldNickname}' not found.");

        SaveUser(
            user.AfterNicknameChange(newNickname)
            );
    }
}
