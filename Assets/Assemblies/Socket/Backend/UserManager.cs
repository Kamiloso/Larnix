using Larnix.Core;
using Larnix.Core.Coroutines;
using Larnix.Core.DbStructs;
using Larnix.Core.Utils;
using Larnix.Socket.Helpers;
using Larnix.Socket.Helpers.Limiters;
using Larnix.Socket.Packets.Control;
using Larnix.Socket.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Larnix.Socket.Backend
{
    public interface IUserManager
    {
        // --- Async Methods ---
        void RegisterUserAsync(string username, string password, Action<bool> callback);
        void ChangePasswordAsync(string username, string newPassword, Action<bool> callback);

        // --- Sync Methods ---
        bool IsOnline(string nickname);
        IEnumerable<string> AllUsernames();
        bool TryDeleteUserLink(string username);
        void ResetLimits();
        bool UserExists(string username);
        long GetUID(string username);
        void ChangePasswordOrAddUserSync(string username, string password);
    }
    
    internal class UserManager : IUserManager, ITickable, IDisposable
    {
        private readonly QuickServer _server;
        private readonly IDbUserAccess _userAccess;
        private readonly CoroutineRunner _coroutines;

        private readonly Limiter<InternetID> _hashLimiter;
        private readonly Limiter<InternetID> _registerLimiter;
        private readonly Limiter _hashingLimiter;

        private readonly CycleTimer[] _cycleTimers;
        private readonly Limiter<InternetID>[] _localLimiters;

        private bool _disposed = false;

        public UserManager(QuickServer server)
        {
            _server = server;
            _userAccess = server.Config.UserAccess;
            _coroutines = new CoroutineRunner();

            InternetID classE = InternetID.ClassE();

            _hashLimiter = new(6, ignoreKey: classE); // per minute
            _registerLimiter = new(6, ignoreKey: classE); // per 3 hours
            _hashingLimiter = new(6); // global concurrent hashing

            _cycleTimers = new[]
            {
                new CycleTimer(60f, () => _hashLimiter.Reset()), // 1 minute
                new CycleTimer(60f * 60f * 3f, () => _registerLimiter.Reset()), // 3 hours
            };

            _localLimiters = new[]
            {
                _hashLimiter,
                _registerLimiter,
            };
        }

        public void Tick(float deltaTime)
        {
            // Tick cycle timers
            foreach (var timer in _cycleTimers)
            {
                timer.Tick(deltaTime);
            }

            // Tick coroutines
            _coroutines.Tick(deltaTime);
        }

        public void ResetLimits()
        {
            foreach (var limiter in _localLimiters)
            {
                limiter.Reset();
            }
        }

        public bool IsOnline(string nickname)
        {
            return _server.ConnDict.Nicknames.Contains(nickname);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _coroutines?.Dispose();
            }
        }

#region Sync Methods

        // ======== User Data Access ========

        public bool TryGetUser(string username, out DbUser userData)
        {
            return _userAccess.TryGetUserData(username, out userData);
        }

        public DbUser GetUser(string username)
        {
            if (TryGetUser(username, out DbUser userData))
                return userData;
            
            return null;
        }

        public bool UserExists(string username)
        {
            return TryGetUser(username, out _);
        }

        public IEnumerable<string> AllUsernames()
        {
            return _userAccess.AllUsernames();
        }

        // ======== User Properties ========

        public long GetUID(string username)
        {
            return GetUser(username)?.UID ??
                default; // 0 --> no user
        }

        public string GetPasswordHash(string username)
        {
            return GetUser(username)?.PasswordHash ??
                default; // null --> no user
        }

        public long GetChallengeID(string username)
        {
            return GetUser(username)?.ChallengeID ??
                default; // 0 --> no user
        }

        // ======== Login & Register ========

        public bool TryIncrementLogin(string username, long currentChallengeID)
        {
            if (TryGetUser(username, out DbUser userData) &&
                userData.ChallengeID == currentChallengeID)
            {
                DbUser updatedUser = userData.AfterLogin();
                _userAccess.SaveUserData(updatedUser);
                return true;
            }
            return false;
        }

        public bool TryAddUser(string username, string passwordHash)
        {
            if (!UserExists(username))
            {
                DbUser user = DbUser.CreateNew(username, passwordHash);
                _userAccess.SaveUserData(user);
                return true;
            }
            return false;
        }

        public bool TryDeleteUserLink(string username)
        {
            if (TryGetUser(username, out DbUser userData))
            {
                DbUser user = userData.WithoutUsername();
                _userAccess.SaveUserData(user);
                return true;
            }
            return false;
        }

        // ======== Password Change ========

        public bool TryChangePasswordHash(string username, string oldHash, string newHash)
        {
            if (TryGetUser(username, out DbUser userData) &&
                (oldHash is null || userData.PasswordHash == oldHash))
            {
                DbUser updatedUser = userData.AfterPasswordChange(newHash);
                _userAccess.SaveUserData(updatedUser);
                return true;
            }
            return false;
        }

        public void ChangePasswordOrAddUserSync(string username, string password)
        {
            string hash = Hasher.HashPassword(password);
            if (!TryChangePasswordHash(username, null, hash))
            {
                TryAddUser(username, hash);
            }
        }

#endregion
#region Async Methods

        private P_LoginTry NewLoginTry(string nickname, string password,
            bool isRegister = false, string newPassword = null)
        {
            return new P_LoginTry(
                nickname: nickname,
                password: password,
                newPassword: newPassword,
                serverSecret: _server.ServerSecret,
                challengeID: isRegister ? 0 : GetChallengeID(nickname),
                timestamp: Timestamp.GetTimestamp(),
                runID: _server.RunID
            );
        }

        public void RegisterUserAsync(string username, string password, Action<bool> callback)
        {
            IPEndPoint target = Common.RandomClassE();
            P_LoginTry logtry = NewLoginTry(username, password, isRegister: true);

            _coroutines.Start(
                LoginCoroutine(target, logtry),
                (success) =>
                {
                    callback?.Invoke(success);
                }
            );
        }

        public void ChangePasswordAsync(string username, string newPassword, Action<bool> callback)
        {
            IPEndPoint target = Common.RandomClassE();
            string oldHash = GetPasswordHash(username);
            
            _coroutines.Start(
                ChangePasswordCoroutine(target, username, oldHash, newPassword),
                (success) =>
                {
                    callback?.Invoke(success);
                }
            );
        }

#endregion
#region Coroutines

        internal enum LoginMode { Discovery, Establishment, PasswordChange }
        internal void StartLogin(IPEndPoint target, P_LoginTry logtry, LoginMode mode,
            Action<bool> Finalize = null)
        {
            string nickname = logtry.Nickname;
            string newPassword = logtry.NewPassword; // optional

            switch (mode)
            {
                case LoginMode.Establishment:

                    _coroutines.Start(
                        LoginCoroutine(target, logtry),
                        (success) =>
                        {
                            if (!success || !_server.ConnDict.TryPromoteConnection(target))
                            {
                                _server.ConnDict.DiscardIncoming(target);
                            }
                        });
                    
                    break;

                case LoginMode.Discovery:

                    _coroutines.Start(
                        LoginCoroutine(target, logtry),
                        (success) =>
                        {
                            Finalize?.Invoke(success);
                        });

                    break;

                case LoginMode.PasswordChange:

                    _coroutines.Start(
                        LoginCoroutine(target, logtry),
                        (success) =>
                        {
                            if (success)
                            {
                                string oldHash = GetPasswordHash(nickname);
                                _coroutines.Start(
                                    ChangePasswordCoroutine(target, nickname, oldHash, newPassword),
                                    (success) =>
                                    {
                                        Finalize?.Invoke(success);
                                    });
                            }
                            else
                            {
                                Finalize?.Invoke(false);
                            }
                        });

                    break;
            }
        }

        private IEnumerator<Box<bool>> LoginCoroutine(IPEndPoint target, P_LoginTry logtry)
        {
            InternetID internetID = _server.MakeInternetID(target);
            bool isLoopback = IPAddress.IsLoopback(target.Address);

            string nickname = logtry.Nickname;
            string password = logtry.Password;
            long serverSecret = logtry.ServerSecret;
            long challengeID = logtry.ChallengeID;
            long timestamp = logtry.Timestamp;
            long runID = logtry.RunID;

            bool IsOkTimestamp(long timestamp) => Timestamp.InTimestamp(timestamp);
            bool IsOkNickname(string nickname) => isLoopback || nickname != Common.ReservedNickname;
            bool IsOkPassword(string password) => isLoopback || password != Common.ReservedPassword;
            bool IsOkChallengeID(long challengeID) => challengeID == GetChallengeID(nickname);

            if (
                // Basic checks
                serverSecret != _server.ServerSecret ||
                runID != _server.RunID ||

                // Complex checks
                !IsOkTimestamp(timestamp) || // login message is outdated
                !IsOkNickname(nickname) || // loopback-only nickname
                !IsOkPassword(password) || // loopback-only password
                !IsOkChallengeID(challengeID)) // wrong challengeID
            {
                yield return new Box<bool>(false);
            }

            bool isRegister = challengeID == 0;
            bool isLogin = !isRegister;
            bool userExists = UserExists(nickname);
            bool mayRegister = _server.Config.AllowRegistration;
            bool isInternal = internetID.IsClassE;

            if ((isRegister && userExists) || (isLogin && !userExists)) // non-matching state
            {
                yield return new Box<bool>(false);
            }

            if (!isInternal && isRegister && !mayRegister) // network registration not allowed
            {
                yield return new Box<bool>(false);
            }
            
            if (isLogin) // LOGIN
            {
                MoreLimiters limiters = new(
                    (() => _hashLimiter.TryAdd(internetID), () => _hashLimiter.Remove(internetID)), // 0
                    (() => _hashingLimiter.TryAdd(), () => _hashingLimiter.Remove()) // 1
                );

                using (var holder = new LimitHolder(
                    () => limiters.TryAdd(),
                    () => limiters.RemoveOnly(1), out bool acquired
                    ))
                {
                    if (acquired)
                    {
                        string hash = GetPasswordHash(nickname);

                        if (Hasher.InCache(password, hash, out bool matches))
                        {
                            _hashLimiter.Remove(internetID); // there won't be hashing, remove ID
                            holder.Dispose(); // dispose before end of using
                        }
                        else
                        {
                            Task<bool> verifying = Hasher.VerifyPasswordAsync(password, hash);
                            while (!verifying.IsCompleted)
                            {
                                yield return null;
                            }

                            matches = verifying.Result;
                        }
                        
                        if (matches)
                        {
                            bool success = TryIncrementLogin(nickname, challengeID);
                            yield return new Box<bool>(success);
                        }
                    }

                    yield return new Box<bool>(false);
                }
            }
            else // REGISTER
            {
                MoreLimiters limiters = new(
                    (() => _registerLimiter.TryAdd(internetID), () => _registerLimiter.Remove(internetID)), // 0
                    (() => _hashLimiter.TryAdd(internetID), () => _hashLimiter.Remove(internetID)), // 1
                    (() => _hashingLimiter.TryAdd(), () => _hashingLimiter.Remove()) // 2
                );

                using (var holder = new LimitHolder(
                    () => limiters.TryAdd(),
                    () => limiters.RemoveOnly(2), out bool acquired
                    ))
                {
                    if (acquired)
                    {
                        Task<string> hashing = Hasher.HashPasswordAsync(password);
                        while (!hashing.IsCompleted)
                        {
                            yield return null;
                        }

                        string hash = hashing.Result;
                        if (!TryAddUser(nickname, hash))
                        {
                            yield return new Box<bool>(false);
                        }

                        if (!isInternal)
                        {
                            ulong cur = _registerLimiter.Current(internetID);
                            ulong max = _registerLimiter.Max;
                            Core.Debug.Log($"{nickname} registered from network {internetID} | Reg: {cur}/{max}");
                        }

                        yield return new Box<bool>(true);
                    }

                    if (!isInternal)
                    {
                        ulong cur = _registerLimiter.Current(internetID);
                        ulong max = _registerLimiter.Max;
                        if (cur >= max)
                        {
                            Core.Debug.LogWarning($"Network {internetID} has reached the limit of {max} registrations.\n" +
                                $"Please wait a few hours or restart the server to reset the limit.");
                        }
                    }
                    
                    yield return new Box<bool>(false);
                }
            }
        }

        private IEnumerator<Box<bool>> ChangePasswordCoroutine(IPEndPoint target, string username,
            string oldHash, string newPassword)
        {
            InternetID internetID = _server.MakeInternetID(target);

            MoreLimiters limiters = new(
                (() => _hashLimiter.TryAdd(internetID), () => _hashLimiter.Remove(internetID)), // 0
                (() => _hashingLimiter.TryAdd(), () => _hashingLimiter.Remove()) // 1
            );

            using (var holder = new LimitHolder(
                () => limiters.TryAdd(),
                () => limiters.RemoveOnly(1), out bool acquired
                ))
            {
                if (acquired)
                {
                    Task<string> hashing = Hasher.HashPasswordAsync(newPassword);
                    while (!hashing.IsCompleted)
                    {
                        yield return null;
                    }
                    
                    string hash = hashing.Result;

                    bool success = TryChangePasswordHash(username, oldHash, hash);
                    yield return new Box<bool>(success);
                }
                
                yield return new Box<bool>(false);
            }
        }

#endregion

    }
}
