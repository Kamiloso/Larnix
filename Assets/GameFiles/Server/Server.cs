using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Larnix.Socket;
using UnityEditor;
using Larnix.Socket.Commands;
using Larnix.Server.Data;
using System.Security.Cryptography;
using System.Linq;
using Larnix.Files;
using System.Net;
using System.Threading.Tasks;

namespace Larnix.Server
{
    public class Server : MonoBehaviour
    {
        private Locker locker = null;
        private RSA CompleteRSA = null;
        private Socket.Server LarnixServer = null;

        public string WorldDir { get; private set; } = "";
        public bool IsLocal { get; private set; } = false;
        public Config ServerConfig { get; private set; } = null;
        public Database Database { get; private set; } = null;

        private float saveCycleTimer = 0f;
        private float broadcastCycleTimer = 0f;

        private readonly Dictionary<InternetID, uint> loginAmount = new();
        private const uint MAX_HASHING_AMOUNT = 4; // max hashing amount per minute per client

        // Server initialization
        private void Awake()
        {
            if(WorldLoad.LoadType == WorldLoad.LoadTypes.None)
                WorldLoad.StartServer();

            WorldDir = WorldLoad.WorldDirectory;
            IsLocal = WorldLoad.LoadType == WorldLoad.LoadTypes.Local;

            EarlyUpdateInjector.InjectEarlyUpdate(this.EarlyUpdate);
            References.Server = this;

            // LOCKER --> 1
            locker = Locker.TryLock(WorldDir, "world_locker.lock");
            if (locker == null)
                throw new Exception("Trying to access world that is already open.");

            // RSA --> 2
            CompleteRSA = KeyObtainer.ObtainKeyRSA(WorldDir, false);

            // CONFIG --> 3
            ServerConfig = Config.Obtain(WorldDir, IsLocal);
            InternetID.MaskIPv4 = ServerConfig.ClientIdentityPrefixSizeIPv4;
            InternetID.MaskIPv6 = ServerConfig.ClientIdentityPrefixSizeIPv6;

            // DATABASE --> 4
            Database = new Database(WorldDir, "database.sqlite");

            // SERVER --> 5
            LarnixServer = new Socket.Server(
                ServerConfig.Port,
                ServerConfig.MaxPlayers,
                ServerConfig.AllowRemoteClients,
                CompleteRSA,
                TryLogin,
                AnswerToNcnPacket
            );

            WorldLoad.ServerAddress = "localhost:" + LarnixServer.Port;
            
            if (IsLocal)
                WorldLoad.RsaPublicKey = KeyToPublicBytes(CompleteRSA);
        }

        private void Start()
        {
            Console.SetTitle("Larnix Server " + Common.GAME_VERSION);

            Console.LogRaw(new string('-', 60) + "\n");

            UnityEngine.Debug.Log(" Socket created on port " + LarnixServer.Port);
            if (CompleteRSA != null) UnityEngine.Debug.Log(" Authcode (copy to connect): " + KeyObtainer.ProduceAuthCodeRSA(KeyToPublicBytes(CompleteRSA)));
            else UnityEngine.Debug.LogWarning(" Every connection will be unencrypted! Couldn't find or generate RSA keys!");

            Console.LogRaw(new string('-', 60) + "\n");

            Console.LogSuccess("Server is ready!");

            if (!IsLocal)
                Console.StartInputThread();

            StartCoroutine(RunEveryMinute());
        }

        private IEnumerator RunEveryMinute()
        {
            while (true)
            {
                // clean login limits
                loginAmount.Clear();

                yield return new WaitForSeconds(60f);
            }
        }

        private void EarlyUpdate() // Executes BEFORE default Update() time
        {
            Queue<PacketAndOwner> messages = LarnixServer.ServerTickAndReceive(Time.deltaTime);
            foreach (PacketAndOwner message in messages)
            {
                string owner = message.Nickname;
                Packet packet = message.Packet;
                
                if((Name)packet.ID == Name.AllowConnection)
                {
                    AllowConnection msg = new AllowConnection(packet);
                    if (msg.HasProblems) continue;

                    // Create player connection
                    References.PlayerManager.JoinPlayer(owner);

                    // Info to console
                    Console.Log(owner + " joined the game.");
                }

                if((Name)packet.ID == Name.Stop)
                {
                    Stop msg = new Stop(packet);
                    if(msg.HasProblems) continue;
                    
                    // Remove player connection
                    References.PlayerManager.DisconnectPlayer(owner);

                    // Info to console
                    Console.Log(owner + " disconnected.");
                }

                if ((Name)packet.ID == Name.PlayerUpdate)
                {
                    PlayerUpdate msg = new PlayerUpdate(packet);
                    if (msg.HasProblems) continue;

                    // check if most recent data (fast mode receiving - over raw udp)
                    Dictionary<string, PlayerUpdate> RecentPlayerUpdates = References.PlayerManager.RecentPlayerUpdates;
                    if(!RecentPlayerUpdates.ContainsKey(owner) || RecentPlayerUpdates[owner].FixedFrame < msg.FixedFrame)
                    {
                        // Update player data
                        References.PlayerManager.UpdatePlayerDataIfHasController(owner, msg);
                    }
                }

                if ((Name)packet.ID == Name.CodeInfo)
                {
                    CodeInfo msg = new CodeInfo(packet);
                    if (msg.HasProblems) continue;

                    CodeInfo.Info code = (CodeInfo.Info)msg.Code;

                    if(code == CodeInfo.Info.RespawnMe)
                    {
                        if(References.PlayerManager.GetPlayerState(owner) == PlayerManager.PlayerState.Dead)
                            References.PlayerManager.CreatePlayerInstance(owner);
                    }
                }
            }

            References.ChunkLoading.FromEarlyUpdate(); // 1
            References.EntityManager.FromEarlyUpdate(); // 2

            InterpretConsoleInput(); // n
        }

        private void LateUpdate()
        {
            broadcastCycleTimer += Time.deltaTime;
            if(ServerConfig.EntityBroadcastPeriod > 0f && broadcastCycleTimer > ServerConfig.EntityBroadcastPeriod)
            {
                broadcastCycleTimer %= ServerConfig.EntityBroadcastPeriod;
                References.EntityManager.SendEntityBroadcast(); // must be in LateUpdate()
            }

            saveCycleTimer += Time.deltaTime;
            if (ServerConfig.DataSavingPeriod > 0f && saveCycleTimer > ServerConfig.DataSavingPeriod)
            {
                saveCycleTimer %= ServerConfig.DataSavingPeriod;
                SaveAllNow();
            }
        }

        public void Send(string nickname, Packet packet, bool safemode = true)
        {
            LarnixServer.Send(nickname, packet, safemode);
        }
        public void Broadcast(Packet packet, bool safemode = true)
        {
            LarnixServer.Broadcast(packet, safemode);
        }
        public void Kick(string nickname)
        {
            LarnixServer.FinishConnection(nickname);
        }

        private Packet AnswerToNcnPacket(Packet packet)
        {
            if (packet == null)
                return null;

            if((Name)packet.ID == Name.P_ServerInfo)
            {
                P_ServerInfo prompt = new P_ServerInfo(packet);
                if (prompt.HasProblems)
                    return null;

                string checkNickname = prompt.Nickname;
                byte[] publicKey = KeyToPublicBytes(CompleteRSA);

                // Key is required for server info prompts, because remote clients can't send unencrypted SYNs
                if (publicKey == null)
                    return null;

                A_ServerInfo answer = new A_ServerInfo(
                    publicKey[0..256],
                    publicKey[256..264],
                    Common.IsGoodMessage(ServerConfig.Motd) ? ServerConfig.Motd : "Invalid motd format :(",
                    LarnixServer.CountPlayers(),
                    LarnixServer.MaxClients,
                    Common.GAME_VERSION_UINT,
                    Database.GetPasswordIndex(checkNickname)
                    );
                if (answer.HasProblems)
                    throw new Exception("Error making server info answer.");

                return answer.GetPacket();
            }

            return null;
        }

        private void TryLogin(IPEndPoint remoteEP, string username, string password)
        {
            StartCoroutine(LoginCoroutine(remoteEP, username, password));
        }

        private IEnumerator LoginCoroutine(IPEndPoint remoteEP, string username, string password)
        {
            if (!IsLocal && username == "Player")
            {
                LarnixServer.LoginDeny(remoteEP);
                yield break; // "Player" nickname is reserved for singleplayer
            }

            InternetID internetID = new InternetID(remoteEP.Address);

            if (!loginAmount.ContainsKey(internetID))
                loginAmount[internetID] = 0;

            if (loginAmount[internetID] >= MAX_HASHING_AMOUNT)
            {
                LarnixServer.LoginDeny(remoteEP);
                yield break; // too many hashing tries in this minute
            }

            if (Database.UserExists(username))
            {
                string password_hash = Database.GetPasswordHash(username);
                Task<bool> verifyTask = Hasher.VerifyPasswordAsync(password, password_hash);

                if (!Hasher.InCache(password, password_hash))
                    loginAmount[internetID]++; // hash will be calculated

                yield return new WaitUntil(() => verifyTask.IsCompleted);

                if (verifyTask.Result)
                {
                    LarnixServer.LoginAccept(remoteEP);
                    yield break; // good password
                }
                else
                {
                    LarnixServer.LoginDeny(remoteEP);
                    yield break; // wrong password
                }
            }
            else
            {
                Task<string> hashTask = Hasher.HashPasswordAsync(password);

                loginAmount[internetID]++; // hash will be calculated

                yield return new WaitUntil(() => hashTask.IsCompleted);

                string hashed_password = hashTask.Result;
                Database.AddUser(username, hashed_password);
                LarnixServer.LoginAccept(remoteEP);
                yield break; // created new account
            }
        }

        public void InterpretConsoleInput() // n
        {
            while (true)
            {
                string cmd = Console.GetCommand();
                if (cmd == null) break;
                string[] arg = cmd.Split(' ');
                int len = arg.Length;

                if (arg.Length == 1 && arg[0] == "help")
                {
                    Console.LogRaw("\n");
                    Console.LogRaw(" | ------ COMMAND LIST ------\n");
                    Console.LogRaw(" |\n");
                    Console.LogRaw(" | help - Displays this documentation.\n");
                    Console.LogRaw(" | stop - Turns off the server.\n");
                    Console.LogRaw(" | playerlist - Shows all players on the server.\n");
                    Console.LogRaw(" | kick [nickname] - Kicks the player if online.\n");
                    Console.LogRaw(" | kill [nickname] - Kills the player if alive.\n");
                    Console.LogRaw(" | spawn [entity] [x] [y] - Spawns entity at the given position.\n");
                    Console.LogRaw("\n");
                }

                else if (len == 1 && arg[0] == "stop")
                {
                    if (!IsLocal) Application.Quit();
                    else Kick("Player"); // when the main player is kicked, he will return to menu and the local server will close
                }

                else if (len == 1 && arg[0] == "playerlist")
                {
                    Console.LogRaw("\n");
                    Console.LogRaw($" | ------ PLAYER LIST [ {LarnixServer.CountPlayers()} / {LarnixServer.MaxClients} ] ------\n");
                    Console.LogRaw(" |\n");

                    foreach (string nickname in References.PlayerManager.PlayerUID.Keys)
                    {
                        Console.LogRaw($" | {nickname} from {LarnixServer.GetClientEndPoint(nickname)}" +
                            $" is {References.PlayerManager.GetPlayerState(nickname).ToString().ToUpper()} \n");
                    }

                    Console.LogRaw("\n");
                }

                else if (len == 2 && arg[0] == "kick")
                {
                    string nickname = arg[1];

                    if (References.PlayerManager.GetPlayerState(nickname) != PlayerManager.PlayerState.None)
                    {
                        Kick(nickname);
                        Console.LogSuccess("Player " + nickname + " has been kicked.");
                    }
                    else
                    {
                        Console.LogError("Player " + nickname + " is not online!");
                    }
                }

                else if (len == 2 && arg[0] == "kill")
                {
                    string nickname = arg[1];

                    if (References.PlayerManager.GetPlayerState(nickname) == PlayerManager.PlayerState.Alive)
                    {
                        ulong uid = References.PlayerManager.PlayerUID[nickname];
                        References.EntityManager.KillEntity(uid);
                        Console.LogSuccess("Player " + nickname + " has been killed.");
                    }
                    else
                    {
                        Console.LogError("Player " + nickname + " is not alive!");
                    }
                }

                else if (len == 4 && arg[0] == "spawn")
                {
                    string entityname = arg[1];

                    if(Enum.TryParse(entityname, ignoreCase: true, out Entities.EntityID entityID) &&
                        Enum.IsDefined(typeof(Entities.EntityID), entityID) &&
                        entityID != Entities.EntityID.Player)
                    {
                        if (float.TryParse(arg[2], out float x) && float.TryParse(arg[3], out float y))
                        {
                            References.EntityManager.SummonEntity(new Entities.EntityData
                            {
                                ID = entityID,
                                Position = new Vector2(x, y)
                            });
                            Console.LogSuccess($"Spawned {entityname} at position ({x}, {y}).");
                        }
                        else Console.LogError($"Cannot parse coordinates!");
                    }
                    else Console.LogError($"Cannot spawn entity named \"{entityname}\"!");
                }

                else Console.LogError("Unknown command! Type 'help' for documentation.");
            }
        }

        private static byte[] KeyToPublicBytes(RSA rsa)
        {
            if (rsa == null)
                return null;

            RSAParameters publicKey = rsa.ExportParameters(false);
            byte[] modulus = publicKey.Modulus;
            byte[] exponent = publicKey.Exponent;

            if (modulus.Length > 256)
                modulus = modulus[0..256];

            while (exponent.Length < 8)
                exponent = (new byte[1]).Concat(exponent).ToArray();

            if (exponent.Length > 8)
                exponent = exponent[0..8];

            return modulus.Concat(exponent).ToArray();
        }

        public void SaveAllNow()
        {
            Database.BeginTransaction();
            try
            {
                References.EntityDataManager.FlushIntoDatabase();
                Database.CommitTransaction();
            }
            catch
            {
                Database.RollbackTransaction();
                throw;
            }

            Config.Save(WorldDir, ServerConfig);
        }

        private void OnDestroy()
        {
            SaveAllNow();

            // 5 --> SERVER
            LarnixServer?.Dispose();

            // 4 ---> DATABASE
            Database?.Dispose();

            // 3 ---> CONFIG
            // (non-disposable)

            // 2 --> RSA
            CompleteRSA?.Dispose();

            // 1 --> LOCKER
            locker?.Dispose();

            EarlyUpdateInjector.ClearEarlyUpdate();

            UnityEngine.Debug.Log("Server close");
        }
    }
}
