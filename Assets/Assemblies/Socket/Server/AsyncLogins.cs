#nullable enable
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Security;
using Larnix.Socket.Server.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Larnix.Socket.Server;

internal interface IAsyncLogins
{
    long GetChallengeId(string nickname); // info for clients, 0 = no account
    IEnumerable<bool?> Login(long uid, Credentials credentials);
    IEnumerable<bool?> SetPassword(long uid, string nickname, string newPassword);
}

internal class AsyncLogins : IAsyncLogins
{
    private readonly IInfoProvider _infoProvider;
    private readonly QuickConfig _settings;

    private readonly IBanProvider _bans;
    private readonly IUserRepository _users;

    public AsyncLogins(IInfoProvider infoProvider, QuickConfig settings)
    {
        _infoProvider = infoProvider;
        _settings = settings;

        _bans = _settings.Interfaces.BanProvider;
        _users = _settings.Interfaces.UserRepository;
    }

    public long GetChallengeId(string nickname)
    {
        long? uid = _settings.Interfaces.UserRepository
            .FindByNickname(nickname)?.Uid;

        long? challengeId = uid.HasValue
             ? _settings.Interfaces.UserRepository.FindByUid(uid.Value)?.ChallengeId
             : null;

        return challengeId ?? 0;
    }

    public IEnumerable<bool?> Login(long uid, Credentials credentials)
    {
        bool isLogin = credentials.IsLogin();
        bool isRegister = credentials.IsRegister();

        string nickname = credentials.Nickname;
        string password = credentials.Password;

        if (!_infoProvider.CheckGlobalCredentials(credentials) ||
            _bans.IsBannedNickname(nickname))
        {
            yield return false;
        }

        User? user = _users.FindByUid(uid);

        if (isLogin)
        {
            if (user == null ||
                user.Nickname != nickname ||
                credentials.ChallengeId != user.ChallengeId)
            {
                yield return false;
            }

            Task<bool> hashing = Task.Run(() => Hasher.VerifyPassword(password, user!.PasswordHash));
            while (!hashing.IsCompleted)
            {
                yield return null;
            }

            _users.SaveUser(user!.AfterLogin());

            yield return hashing.Result;
        }

        if (isRegister)
        {
            if (!_settings.EnableRegister ||
                user != null)
            {
                yield return false;
            }

            IEnumerator<bool?> passchange = SetPassword(uid, nickname, password).GetEnumerator();
            while (passchange.MoveNext())
            {
                yield return passchange.Current;
            }
        }
    }

    public IEnumerable<bool?> SetPassword(long uid, string nickname, string newPassword)
    {
        User? user1 = _users.FindByUid(uid); // before hashing
        if (user1 != null && user1.Nickname != nickname)
        {
            yield return false; // nickname does not match
        }

        Task<string> hashing = Task.Run(() => Hasher.HashPassword(newPassword));
        while (!hashing.IsCompleted)
        {
            yield return null;
        }

        User? user2 = _users.FindByUid(uid); // after hashing
        if (user1 != user2)
        {
            yield return false; // user was modified during hashing
        }

        string newPasswordHash = hashing.Result;

        User user = user1?.AfterPasswordChange(newPasswordHash) ??
            User.CreateAccount(uid, nickname, newPasswordHash);

        _users.SaveUser(user);

        yield return true;
    }
}
