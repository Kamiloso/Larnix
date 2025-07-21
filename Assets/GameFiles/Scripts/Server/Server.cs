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
        private bool updateStartDone = false;
        private float broadcastCycleTimer = 0f;

        private readonly Dictionary<InternetID, uint> loginAmount = new();
        private const uint MAX_HASHING_AMOUNT = 3; // logins per minute

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
            if(!updateStartDone)
            {
                UnityEngine.Debug.Log("Done! Server started on port " + LarnixServer.Port);
                if (!IsLocal && CompleteRSA != null)
                    UnityEngine.Debug.Log("AuthCodeRSA (copy to connect): " + KeyObtainer.ProduceAuthCodeRSA(KeyToPublicBytes(CompleteRSA)));
                updateStartDone = true;
            }

            References.ChunkLoading.FromEarlyUpdate(); // 1
            References.EntityManager.FromEarlyUpdate(); // 2

            Queue<PacketAndOwner> messages = LarnixServer.ServerTickAndReceive(Time.deltaTime);
            foreach (PacketAndOwner message in messages)
            {
                string owner = message.Nickname;
                Packet packet = message.Packet;
                
                if((Name)packet.ID == Name.AllowConnection)
                {
                    AllowConnection msg = new AllowConnection(packet);
                    if (msg.HasProblems) continue;

                    // Initialize player controller
                    References.EntityManager.CreatePlayerController(owner);

                    // Construct and send answer
                    EntityController playerController = References.EntityManager.GetPlayerController(owner);
                    PlayerInitialize answer = new PlayerInitialize(
                        playerController.EntityData.Position,
                        playerController.uID
                    );
                    if(!answer.HasProblems)
                    {
                        Send(owner, answer.GetPacket());
                    }

                    // Info to console
                    UnityEngine.Debug.Log("Player [" + owner + "] joined.");

                    References.EntityManager.SummonEntity(new Entities.EntityData
                    {
                        ID = Entities.EntityID.Wildpig,
                        Position = playerController.EntityData.Position,
                        Rotation = 0f,
                        NBT = "{}"
                    });
                }

                if((Name)packet.ID == Name.Stop)
                {
                    Stop msg = new Stop(packet);
                    if(msg.HasProblems) continue;

                    // Remove player controller
                    if (References.EntityManager.GetPlayerController(owner) != null)
                        References.EntityManager.UnloadPlayerController(owner);

                    // Info to console
                    UnityEngine.Debug.Log("Player [" + owner + "] disconnected.");
                }

                /*if((Name)packet.ID == Name.DebugMessage)
                {
                    DebugMessage msg = new DebugMessage(packet);
                    if (msg.HasProblems) continue;

                    // Temporary wildpig spawn
                    EntityController playerController = References.EntityManager.GetPlayerController(owner);
                    References.EntityManager.SummonEntity(new Entities.EntityData
                    {
                        ID = Entities.EntityID.Wildpig,
                        Position = playerController == null ? Vector2.zero : playerController.EntityData.Position,
                        Rotation = 0f,
                        NBT = "{}"
                    });

                    UnityEngine.Debug.Log("DebugMessage [" + owner + "]: " + msg.Data);
                }*/

                if ((Name)packet.ID == Name.PlayerUpdate)
                {
                    PlayerUpdate msg = new PlayerUpdate(packet);
                    if (msg.HasProblems) continue;

                    // Load data to player controller
                    EntityController playerController = References.EntityManager.GetPlayerController(owner);
                    if (playerController != null)
                    {
                        playerController.ActivateIfNotActive();
                        Entities.EntityData entityData = playerController.EntityData.ShallowCopy();
                        entityData.Position = msg.Position;
                        entityData.Rotation = msg.Rotation;
                        playerController.UpdateEntityData(entityData);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            broadcastCycleTimer += Time.deltaTime;
            if(ServerConfig.EntityBroadcastPeriod > 0f && broadcastCycleTimer > ServerConfig.EntityBroadcastPeriod)
            {
                broadcastCycleTimer %= ServerConfig.EntityBroadcastPeriod;
                References.EntityDataManager.SendEntityBroadcast(); // must be in LateUpdate()
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
