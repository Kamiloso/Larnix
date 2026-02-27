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
        // --- Sync Methods ---
        bool IsOnline(string nickname);
        bool IsAutoManagedUser(string username);
        IEnumerable<string> AllUsernames();
        bool TryRenameUser(string oldUsername, string newUsername);
        bool TryDeleteUserLink(string username);
        void ResetLimits();
        bool UserExists(string username);
        long GetUID(string username);

        // --- CPU Heavy Operations ---
        bool TryAddUserSync(string username, string password);
        bool TryChangePasswordSync(string username, string newHash);
        bool TryChangePasswordOrAddUserSync(string username, string password);
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
            return _server.ConnDict.IsOnline(nickname);
        }

        public bool IsAutoManagedUser(string username)
        {
            string hostUser = _server.Config.HostUser;

            return username == Common.ReservedNickname ||
                   username == hostUser;
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
            // WARNING: Fallback to default
            return TryGetUser(username, out DbUser userData) ?
                userData.UID : 0; // 0 --> no user
        }

        public string GetPasswordHash(string username)
        {
            // WARNING: Fallback to default
            return TryGetUser(username, out DbUser userData) ?
                userData.PasswordHash : null; // null --> no user
        }

        public long GetChallengeID(string username)
        {
            // WARNING: Fallback to default
            return TryGetUser(username, out DbUser userData) ?
                userData.ChallengeID : 0; // 0 --> no user
        }

        public bool MatchesOldHash(string username, string oldHash)
        {
            return TryGetUser(username, out DbUser userData) &&
                userData.PasswordHash == oldHash;
        }

        public bool MatchesChallengeID(string username, long challengeID)
        {
            return TryGetUser(username, out DbUser userData) &&
                userData.ChallengeID == challengeID;
        }

        // ======== User Management ========

        public bool TryIncrementLogin(string username)
        {
            if (TryGetUser(username, out DbUser userData))
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
                DbUser updatedUser = DbUser.CreateNew(username, passwordHash);
                _userAccess.SaveUserData(updatedUser);
                return true;
            }
            return false;
        }

        public bool TryRenameUser(string oldUsername, string newUsername)
        {
            if (TryGetUser(oldUsername, out DbUser userData) &&
                !IsOnline(oldUsername) && !UserExists(newUsername))
            {
                DbUser updatedUser = userData.AfterUsernameChange(newUsername);
                _userAccess.SaveUserData(updatedUser);
                return true;
            }
            return false;
        }

        public bool TryDeleteUserLink(string username)
        {
            if (TryGetUser(username, out DbUser userData) &&
                !IsOnline(username))
            {
                DbUser updatedUser = userData.WithoutUsername();
                _userAccess.SaveUserData(updatedUser);
                return true;
            }
            return false;
        }

        public bool TryChangePasswordHash(string username, string newHash)
        {
            if (TryGetUser(username, out DbUser userData))
            {
                DbUser updatedUser = userData.AfterPasswordChange(newHash);
                _userAccess.SaveUserData(updatedUser);
                return true;
            }
            return false;
        }

        // ======== Heavy Sync Operations ========

        public bool TryAddUserSync(string username, string password)
        {
            string hash = Hasher.HashPassword(password);
            return TryAddUser(username, hash);
        }

        public bool TryChangePasswordSync(string username, string newPassword)
        {
            string hash = Hasher.HashPassword(newPassword);
            return TryChangePasswordHash(username, hash);
        }

        public bool TryChangePasswordOrAddUserSync(string username, string password)
        {
            if (UserExists(username))
            {
                return TryChangePasswordSync(username, password);
            }
            else
            {
                return TryAddUserSync(username, password);
            }
        }

#endregion
#region Coroutines

        public enum LoginMode
        {
            Discovery, // stateless login or register, no active session
            Establishment, // trying to establish game connection
            PasswordChange // login + password change
        }

        public void StartLogin(IPEndPoint target, P_LoginTry logtry, LoginMode mode,
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
                                // hash will be unique per hashing with 16 byte random salt,
                                // so it can be used to identify passchange session
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

            if (
                // Basic checks
                !(serverSecret == _server.ServerSecret) ||
                !(runID == _server.RunID) ||

                // Complex checks
                !Timestamp.InTimestamp(timestamp) || // login message is outdated
                !(isLoopback || nickname != Common.ReservedNickname) || // loopback-only nickname
                !(isLoopback || password != Common.ReservedPassword) || // loopback-only password
                !(challengeID == GetChallengeID(nickname))) // wrong challengeID
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
                        string oldHash = GetPasswordHash(nickname);

                        if (Hasher.InCache(password, oldHash, out bool matches))
                        {
                            _hashLimiter.Remove(internetID); // there won't be hashing, remove ID
                            holder.Dispose(); // dispose before end of using
                        }
                        else
                        {
                            Task<bool> verifying = Hasher.VerifyPasswordAsync(password, oldHash);
                            while (!verifying.IsCompleted)
                            {
                                yield return null;
                            }

                            matches = verifying.Result;
                        }
                        
                        if (matches)
                        {
                            bool success = MatchesOldHash(nickname, oldHash) &&
                                           MatchesChallengeID(nickname, challengeID) &&
                                           TryIncrementLogin(nickname);

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
                    
                    bool success = MatchesOldHash(username, oldHash) &&
                                   TryChangePasswordHash(username, hash);

                    yield return new Box<bool>(success);
                }
                
                yield return new Box<bool>(false);
            }
        }

#endregion

    }
}
