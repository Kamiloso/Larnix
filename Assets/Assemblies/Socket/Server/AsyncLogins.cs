#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Security;
using Larnix.Socket.Server.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Larnix.Socket.Server;

internal interface IAsyncLogins
{
    long GetChallengeId(in FixedString32 nickname); // 0 = no user
    IEnumerable<bool?> Login(Credentials credentials);
    IEnumerable<bool?> Register(Credentials credentials);
    IEnumerable<bool?> SetPassword(FixedString32 nickname, FixedString64 newPassword);
}

internal class AsyncLogins : IAsyncLogins
{
    private readonly IInfoProvider _infoProvider;
    private readonly QuickConfig _settings;

    private readonly IBanProvider _bans;
    private readonly IQuickUserRepository _users;

    public AsyncLogins(IInfoProvider infoProvider, QuickConfig settings)
    {
        _infoProvider = infoProvider;
        _settings = settings;

        _bans = _settings.Interfaces.BanProvider;
        _users = _settings.Interfaces.UserRepository;
    }

    public long GetChallengeId(in FixedString32 nickname) // 0 = no user
    {
        QuickUser? user = _settings.Interfaces.UserRepository
            .FindByNickname(nickname);

        long? challengeId = (user?.Uid).HasValue
             ? user?.ChallengeId
             : null;

        return challengeId ?? 0;
    }

    public IEnumerable<bool?> Login(Credentials credentials)
    {
        var (nickname, password, challengeId) = credentials.Extract();

        if (!credentials.IsLogin())
            yield return false;

        if (!_infoProvider.CheckGlobalCredentials(credentials))
            yield return false;

        if (_bans.IsBannedNickname(nickname))
            yield return false;


        QuickUser? user1 = _users.FindByNickname(nickname);
        if (user1 == null)
        {
            yield return false; // user not found
        }

        if (user1 == null ||
            nickname != user1!.Nickname ||
            challengeId != user1.ChallengeId)
        {
            yield return false; // basic credentials mismatch
        }

        Task<bool> hashing = Task.Run(() => Hasher.VerifyPassword(password, user1!.PasswordHash));
        while (!hashing.IsCompleted)
        {
            yield return null;
        }

        QuickUser? user2 = _users.FindByNickname(nickname);
        if (user1 != user2)
        {
            yield return false; // user was modified during hashing
        }

        bool success = hashing.Result;
        if (success)
        {
            _users.SaveUser(user1!.AfterLogin());
            yield return true;
        }
        else
        {
            yield return false;
        }
    }

    public IEnumerable<bool?> Register(Credentials credentials)
    {
        var (nickname, password, challengeId) = credentials.Extract();

        if (!_settings.EnableRegister)
            yield return false;

        if (!credentials.IsRegister())
            yield return false;

        if (!_infoProvider.CheckGlobalCredentials(credentials))
            yield return false;

        if (_bans.IsBannedNickname(nickname))
            yield return false;

        QuickUser? user1 = _users.FindByNickname(nickname);
        if (user1 != null)
        {
            yield return false; // nickname already exists
        }

        Task<string> hashing = Task.Run(() => Hasher.HashPassword(password));
        while (!hashing.IsCompleted)
        {
            yield return null;
        }

        QuickUser? user2 = _users.FindByNickname(nickname);
        if (user2 != null)
        {
            yield return false; // nickname was taken during hashing
        }

        long nextUid = _users.NextFreeUid();
        string passwordHash = hashing.Result;

        _users.SaveUser(
            QuickUser.CreateAccount(nextUid, nickname, passwordHash)
            );

        yield return true;
    }

    public IEnumerable<bool?> SetPassword(FixedString32 nickname, FixedString64 newPassword)
    {
        QuickUser? user1 = _users.FindByNickname(nickname);
        if (user1 == null)
        {
            IEnumerator<bool?> registration = Register(
                _infoProvider.CreateCredentials(nickname, newPassword, 0)
                ).GetEnumerator();

            while (registration.MoveNext())
            {
                yield return registration.Current;
            }
        }

        Task<string> hashing = Task.Run(() => Hasher.HashPassword(newPassword));
        while (!hashing.IsCompleted)
        {
            yield return null;
        }

        QuickUser? user2 = _users.FindByNickname(nickname);
        if (user1 != user2)
        {
            yield return false; // user was modified during hashing
        }

        string newPasswordHash = hashing.Result;

        _users.SaveUser(
            user1!.AfterPasswordChange(newPasswordHash)
            );

        yield return true;
    }
}
