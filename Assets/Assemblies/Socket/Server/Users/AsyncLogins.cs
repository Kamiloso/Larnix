#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Server.Configuration;
using Larnix.Socket.Server.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Larnix.Socket.Server.Users;

internal class AsyncLogins
{
    private readonly QuickSettings _settings;
    private readonly SecretProvider _secrets;
    private readonly Limiters _limiters;

    private readonly IBanProvider _bans;
    private readonly IQuickUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public AsyncLogins(MainServices services)
    {
        _settings = services.Settings;
        _secrets = services.Secrets;
        _limiters = services.Limiters;

        _bans = services.Interfaces.BanProvider;
        _users = services.Interfaces.UserRepository;
        _hasher = services.Interfaces.PasswordHasher;
    }

    public long GetChallengeId(in FixedString32 nickname) // 0 = no user
    {
        QuickUser? user = _users.FindByNickname(nickname);

        long? challengeId = (user?.Uid).HasValue
             ? user?.ChallengeId
             : null;

        return challengeId ?? 0;
    }

    public IEnumerable<bool?> LoginOrRegister(Credentials credentials, bool isLoopback)
    {
        IEnumerator<bool?> process =
            credentials.IsLogin()
                ? Login(credentials, isLoopback).GetEnumerator()
                : Register(credentials, isLoopback).GetEnumerator();

        while (process.MoveNext())
        {
            yield return process.Current;
        }
    }

    public IEnumerable<bool?> Login(Credentials credentials, bool isLoopback)
    {
        var (nickname, password, challengeId) = credentials.ExtractLoginData();

        if (!credentials.IsLogin())
            yield return false;

        if (!isLoopback && credentials.IsLoopbackOnly())
            yield return false;

        if (!_secrets.CheckGlobalCredentials(credentials))
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

        Task<bool> hashing = Task.Run(() => _hasher.VerifyPassword(password, user1!.PasswordHash));
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

    public IEnumerable<bool?> Register(Credentials credentials, bool isLoopback)
    {
        var (nickname, password, challengeId) = credentials.ExtractLoginData();

        if (!_settings.EnableRegister)
            yield return false;

        if (!credentials.IsRegister())
            yield return false;

        if (!isLoopback && credentials.IsLoopbackOnly())
            yield return false;

        if (!_secrets.CheckGlobalCredentials(credentials))
            yield return false;

        if (_bans.IsBannedNickname(nickname))
            yield return false;

        QuickUser? user1 = _users.FindByNickname(nickname);
        if (user1 != null)
        {
            yield return false; // nickname already exists
        }

        Task<string> hashing = Task.Run(() => _hasher.HashPassword(password));
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
                credentials: _secrets.CreateCredentials(nickname, newPassword, 0),
                isLoopback: true // inner registration -> assume it's loopback
                ).GetEnumerator();

            while (registration.MoveNext())
            {
                yield return registration.Current;
            }
        }

        Task<string> hashing = Task.Run(() => _hasher.HashPassword(newPassword));
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
