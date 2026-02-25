using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using Larnix.Socket.Packets;
using System.Threading.Tasks;
using System;
using Larnix.Core.Utils;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Channel;
using Larnix.Socket.Security;
using Larnix.Socket.Packets.Control;
using Larnix.Socket.Channel.Networking;
using Larnix.Socket.Helpers;
using Larnix.Socket.Helpers.Limiters;
using Larnix.Core.Coroutines;
using Larnix.Core;

namespace Larnix.Socket.Backend
{
    public class QuickServer : IDisposable, ITickable
    {
        // --- Server properties ---
        public string Authcode { get; init; }
        internal long Secret { get; init; }
        internal long RunID { get; init; }
        public ushort PlayerCount => (ushort)_connDict.Count;

        // --- Public classes ---
        public QuickConfig Config { get; init; }
        public UserManager UserManager { get; init; }

        // --- Private classes ---
        private readonly KeyRSA _keyRSA; // 1
        private readonly TripleSocket _udpSocket; // 2
        private readonly ConnDict _connDict; // 3
        private readonly CoroutineRunner _coroutines; // 4
        private readonly CycleTimer[] _cycleTimers;

        // --- Limiters ---
        private readonly TrafficLimiter<InternetID> _heavyPacketLimiter = new(5, 50); // per second
        private readonly Limiter<InternetID> _hashLimiter = new(6); // per minute
        private readonly Limiter<InternetID> _registerLimiter = new(6); // per 3 hours
        private readonly Limiter _hashingLimiter = new(6); // global concurrent hashings

        // --- Other ---
        private readonly Dictionary<CmdID, Action<HeaderSpan, string>> Subscriptions = new();
        private bool _disposed;

        public QuickServer(QuickConfig serverConfig)
        {
            // Managed classes
            _keyRSA = new KeyRSA(serverConfig.DataPath, "private_key.pem"); // 1
            _udpSocket = new TripleSocket(serverConfig.Port, serverConfig.IsLoopback); // 2
            _connDict = new ConnDict(_udpSocket, this, serverConfig.MaxClients); // 3
            _coroutines = new CoroutineRunner(); // 4

            // Classes
            UserManager = new UserManager(serverConfig.UserAPI);
            Config = serverConfig.WithPort(_udpSocket.Port);

            // Constant properties
            Secret = Security.Authcode.ObtainSecret(serverConfig.DataPath, "server_secret.txt");
            Authcode = Security.Authcode.ProduceAuthCodeRSA(_keyRSA.ExportPublicKey(), Secret);
            RunID = Common.GetSecureLong();

            // Cyclic objects
            _cycleTimers = new[]
            {
                new CycleTimer(1f, () => _heavyPacketLimiter.Reset()),
                new CycleTimer(60f, () => _hashLimiter.Reset()),
                new CycleTimer(60f * 60f * 3f, () => _registerLimiter.Reset()),
                new CycleTimer(5f, () => _udpSocket.KeepAlive())
            };
        }

        public async Task<string> EstablishRelayAsync(string relayAddress)
        {
            ushort? relayPort = await _udpSocket.StartRelayAsync(relayAddress);
                
            if (relayPort != null)
            {
                string address = Common.FormatUdpAddress(relayAddress, relayPort.Value);
                Core.Debug.LogSuccess("Connected to relay!");
                Core.Debug.Log("Address: " + address);
                return address;
            }
            else
            {
                Core.Debug.LogWarning("Cannot connect to relay!");
                return null;
            }
        }

        public void Tick(float deltaTime)
        {
            // AckTimers tick
            foreach (var timer in _cycleTimers)
            {
                timer.Tick(deltaTime);
            }

            // Interpret bytes from socket
            while (_udpSocket.TryReceive(out var item))
            {
                IPEndPoint remoteEP = item.target;
                byte[] bytes = item.data;

                InterpretBytes(remoteEP, bytes);
            }

            // Tick & Receive from connections
            Queue<PacketPair> packets = _connDict.TickAndReceive(deltaTime);
            while (packets.Count > 0)
            {
                var element = packets.Dequeue();
                var packet = element.Packet;
                string owner = element.Owner;

                if (Subscriptions.TryGetValue(packet.ID, out var Execute))
                {
                    Execute(packet, owner);
                }
            }

            // Tick coroutines
            _coroutines.Tick(deltaTime);
        }

        private void InterpretBytes(IPEndPoint target, byte[] bytes)
        {
            if (PayloadBox.TryDeserializeHeader(bytes, out var header))
            {
                InternetID internetID = MakeInternetID(target);

                // Heavy packet limiter
                if (header.HasFlag(PacketFlag.SYN) ||
                    header.HasFlag(PacketFlag.RSA) ||
                    header.HasFlag(PacketFlag.NCN))
                {
                    if (!_heavyPacketLimiter.TryAdd(internetID))
                        return; // drop heavy packet
                }

                // Decrypt RSA
                if (header.HasFlag(PacketFlag.RSA))
                {
                    if (!PayloadBox.TryDeserialize(bytes, _keyRSA, out var decrypted))
                        return; // drop invalid RSA packet
                    
                    decrypted.UnsetFlag(PacketFlag.RSA);
                    bytes = decrypted.Serialize(KeyEmpty.GetInstance());
                }

                if (header.HasFlag(PacketFlag.NCN))
                {
                    // Non-connection packet - NCN
                    if (PayloadBox.TryDeserialize(bytes, KeyEmpty.GetInstance(), out var box))
                        ProcessNCN(target, box.SeqNum, new HeaderSpan(box.Bytes));
                }
                else
                {
                    if (header.HasFlag(PacketFlag.SYN))
                    {
                        // Start new connection
                        if (PayloadBox.TryDeserialize(bytes, KeyEmpty.GetInstance(), out var synBox) &&
                            Payload.TryConstructPayload<AllowConnection>(synBox.Bytes, out var allowcon) &&
                            _connDict.TryAddPreLogin(target, synBox))
                        {
                            P_LoginTry logtry = allowcon.ToLoginTry();
                            StartLogin(target, logtry, LoginMode.Establishment);
                        }
                    }
                    else
                    {
                        // Established connection packet
                        _connDict.EnqueueReceivedPacket(target, bytes);
                    }
                }
            }
        }

        private void ProcessNCN(IPEndPoint target, int ncnID, HeaderSpan headerSpan)
        {
            void SendNCN(Payload packet)
            {
                PayloadBox safeAnswer = new PayloadBox(
                    seqNum: ncnID,
                    ackNum: 0,
                    flags: (byte)PacketFlag.NCN,
                    payload: packet
                    );

                // answer always as plaintext
                byte[] payload = safeAnswer.Serialize(KeyEmpty.GetInstance());
                _udpSocket.Send(target, payload);
            };

            if (Payload.TryConstructPayload<P_ServerInfo>(headerSpan, out var infoask))
            {
                string nickname = infoask.Nickname;
                
                A_ServerInfo srvInfo = new A_ServerInfo(
                    publicKey: _keyRSA.ExportPublicKey(),
                    currentPlayers: PlayerCount,
                    maxPlayers: Config.MaxClients,
                    gameVersion: Core.Version.Current,
                    challengeID: UserManager.GetChallengeID(nickname),
                    timestamp: Timestamp.GetTimestamp(),
                    runID: RunID,
                    motd: Config.Motd,
                    hostUser: Config.HostUser,
                    mayRegister: Config.AllowRegistration
                    );

                SendNCN(srvInfo);
            }

            else if (Payload.TryConstructPayload<P_LoginTry>(headerSpan, out var logtry))
            {
                string nickname = logtry.Nickname;

                var loginMode = logtry.IsPasswordChange() ?
                    LoginMode.PasswordChange : LoginMode.Discovery;
                
                StartLogin(target, logtry, loginMode, success =>
                {
                    SendNCN(new A_LoginTry(success));
                });
            }
        }

        /// <summary>
        /// AllowConnection packet always starts the connection.
        /// Stop packet always ends the connection.
        /// Stop packet can only appear once in a returned packet queue.
        /// </summary>
        public void Subscribe<T>(Action<T, string> InterpretPacket) where T : Payload, new()
        {
            CmdID ID = Payload.CmdID<T>();
            Subscriptions[ID] = (HeaderSpan headerSpan, string owner) =>
            {
                if (Payload.TryConstructPayload<T>(headerSpan, out var message))
                {
                    InterpretPacket(message, owner);
                }
            };
        }

        public bool IsActiveConnection(string nickname)
        {
            return TryGetClientEndPoint(nickname, out _);
        }

        public bool TryGetClientEndPoint(string nickname, out IPEndPoint endPoint)
        {
            IPEndPoint endPoint1 = _connDict.EndPointOf(nickname);
            if (endPoint1 != null)
            {
                endPoint = endPoint1;
                return true;
            }
            endPoint = null;
            return false;
        }

        public void Send(string nickname, Payload packet, bool safemode = true)
        {
            if (TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
            {
                _connDict.SendTo(endPoint, packet, safemode);
            }
        }

        public void Broadcast(Payload packet, bool safemode = true)
        {
            _connDict.SendToAll(packet, safemode);
        }

        public float GetPing(string nickname)
        {
            if (TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
            {
                Connection conn = _connDict.GetConnectionObject(endPoint);
                return conn.AvgRTT;
            }
            return 0f;
        }

        public void FinishConnectionRequest(string nickname)
        {
            if (TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
            {
                _connDict.KickRequest(endPoint);
            }
        }

        private enum LoginMode { Discovery, Establishment, PasswordChange }
        private void StartLogin(IPEndPoint target, P_LoginTry logtry, LoginMode mode,
            Action<bool> SendAnswer = null)
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
                            if (success && _connDict.TryPromoteConnection(target))
                            {
                                UserManager.IncrementChallengeID(nickname);
                            }
                            else
                            {
                                _connDict.DiscardIncoming(target);
                            }
                        });
                    
                    break;

                case LoginMode.Discovery:

                    _coroutines.Start(
                        LoginCoroutine(target, logtry),
                        (success) =>
                        {
                            if (success)
                            {
                                UserManager.IncrementChallengeID(nickname);
                            }
                            SendAnswer?.Invoke(success);
                        });

                    break;

                case LoginMode.PasswordChange:

                    _coroutines.Start(
                        LoginCoroutine(target, logtry),
                        (success) =>
                        {
                            if (success)
                            {
                                UserManager.IncrementChallengeID(nickname);

                                _coroutines.Start(
                                    ChangePasswordCoroutine(target, nickname, newPassword),
                                    (success) =>
                                    {
                                        SendAnswer?.Invoke(success);
                                    });
                            }
                            else
                            {
                                SendAnswer?.Invoke(false);
                            }
                        });

                    break;
            }
        }

        private IEnumerator<Box<bool>> LoginCoroutine(IPEndPoint target, P_LoginTry logtry)
        {
            InternetID internetID = MakeInternetID(target);
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
            bool IsOkChallengeID(long challengeID) => challengeID == UserManager.GetChallengeID(nickname);

            if (
                // Basic checks
                serverSecret != Secret ||
                runID != RunID ||

                // Complex checks
                !IsOkTimestamp(timestamp) || // login message is outdated
                !IsOkNickname(nickname) || // loopback-only nickname
                !IsOkPassword(password) || // loopback-only password
                !IsOkChallengeID(challengeID)) // wrong challengeID
            {
                yield return new Box<bool>(false);
            }
            
            if (UserManager.UserExists(nickname)) // LOGIN
            {
                string hashedPassword = UserManager.GetPasswordHash(nickname);

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
                        if (Hasher.InCache(password, hashedPassword, out bool matches))
                        {
                            _hashLimiter.Remove(internetID); // there won't be hashing, remove ID
                            holder.Dispose(); // dispose before end of using
                        }
                        else
                        {
                            Task<bool> verifying = Hasher.VerifyPasswordAsync(password, hashedPassword);
                            while (!verifying.IsCompleted)
                            {
                                yield return null;
                            }

                            matches = verifying.Result;
                        }
                        
                        yield return new Box<bool>(matches);
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
                        UserManager.AddUser(nickname, hash);

                        ulong cur = _registerLimiter.Current(internetID);
                        ulong max = _registerLimiter.Max;
                        Core.Debug.Log($"{nickname} registered from network {internetID} | Reg: {cur}/{max}");

                        yield return new Box<bool>(true);
                    }

                    using (var holder2 = new LimitHolder(
                        () => _registerLimiter.TryAdd(internetID),
                        () => _registerLimiter.Remove(internetID), out bool acquired2
                        ))
                    {
                        if (!acquired2)
                        {
                            ulong max = _registerLimiter.Max;
                            Core.Debug.LogWarning($"Network {internetID} has reached the limit of {max} registrations.\n" +
                                $"Please wait a few hours or restart the server to reset the limit.");
                        }
                    }
                    
                    yield return new Box<bool>(false);
                }
            }
        }

        private IEnumerator<Box<bool>> ChangePasswordCoroutine(IPEndPoint target, string username, string newPassword)
        {
            InternetID internetID = MakeInternetID(target);

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
                    UserManager.SetPasswordHash(username, hash);

                    yield return new Box<bool>(true);
                }
                
                yield return new Box<bool>(false);
            }
        }

        internal InternetID MakeInternetID(IPEndPoint endPoint)
        {
            return new InternetID(
                endPoint.Address,
                endPoint.AddressFamily == AddressFamily.InterNetwork ?
                    Config.MaskIPv4 : Config.MaskIPv6
                );
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _coroutines?.Dispose(); // 4
                _connDict?.Dispose(); // 3
                _udpSocket?.Dispose(); // 2
                _keyRSA?.Dispose(); // 1
            }
        }
    }
}
