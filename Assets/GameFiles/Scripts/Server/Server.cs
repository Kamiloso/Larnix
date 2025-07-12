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
using Unity.VisualScripting;

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

        private Dictionary<string, EntityController> PlayerControllers = new Dictionary<string, EntityController>();

        private float saveCycleTimer = 0f;
        private bool updateStartDone = false;
        private float broadcastCycleTimer = 0f;
        private uint entityBroadcastCounter = 0;

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

        private void EarlyUpdate() // Executes BEFORE default Update() time
        {
            if(!updateStartDone)
            {
                UnityEngine.Debug.Log("Done! Server started on port " + LarnixServer.Port);
                if (!IsLocal && CompleteRSA != null)
                    UnityEngine.Debug.Log("AuthCodeRSA (copy to connect): " + KeyObtainer.ProduceAuthCodeRSA(KeyToPublicBytes(CompleteRSA)));
                updateStartDone = true;
            }

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
                    PlayerControllers[owner] = EntityController.CreatePlayerController(owner);

                    // Construct and send answer
                    PlayerInitialize answer = new PlayerInitialize(
                        PlayerControllers[owner].EntityData.Position,
                        PlayerControllers[owner].uID
                    );
                    if(!answer.HasProblems)
                    {
                        Send(owner, answer.GetPacket());
                    }

                    // Info to console
                    UnityEngine.Debug.Log("Player [" + owner + "] joined.");
                }

                if((Name)packet.ID == Name.Stop)
                {
                    Stop msg = new Stop(packet);
                    if(msg.HasProblems) continue;

                    // Remove player controller
                    PlayerControllers[owner].UnloadEntity();
                    PlayerControllers.Remove(owner);

                    // Info to console
                    UnityEngine.Debug.Log("Player [" + owner + "] disconnected.");
                }

                if((Name)packet.ID == Name.DebugMessage)
                {
                    DebugMessage msg = new DebugMessage(packet);
                    if (msg.HasProblems) continue;

                    UnityEngine.Debug.Log("DebugMessage [" + owner + "]: " + msg.Data);
                }

                if ((Name)packet.ID == Name.PlayerUpdate)
                {
                    PlayerUpdate msg = new PlayerUpdate(packet);
                    if (msg.HasProblems) continue;

                    // Load data to player controller
                    EntityController playerController = PlayerControllers[owner];
                    playerController.ActivateIfNotActive();
                    playerController.EntityData = playerController.EntityData.ShallowCopy();
                    playerController.EntityData.Position = msg.Position;
                    playerController.EntityData.Rotation = msg.Rotation;
                }
            }
        }

        private void LateUpdate()
        {
            broadcastCycleTimer += Time.deltaTime;
            if(broadcastCycleTimer > ServerConfig.EntityBroadcastPeriod)
            {
                broadcastCycleTimer %= ServerConfig.EntityBroadcastPeriod;
                References.EntityDataManager.SendEntityBroadcast(++entityBroadcastCounter);
            }

            saveCycleTimer += Time.deltaTime;
            if (saveCycleTimer > ServerConfig.DataSavingPeriod)
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
            LarnixServer.KillConnection(nickname);
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

            if ((Name)packet.ID == Name.P_PasswordChange)
            {
                P_PasswordChange prompt = new P_PasswordChange(packet);
                if (prompt.HasProblems)
                    return null;

                string checkNickname = prompt.Nickname;
                string oldPassword = prompt.OldPassword;
                string newPassword = prompt.NewPassword;

                A_PasswordChange answer = new A_PasswordChange(
                    Database.ChangePassword(checkNickname, oldPassword, newPassword)
                    );
                if (answer.HasProblems)
                    throw new Exception("Error making password change answer.");

                return answer.GetPacket();
            }

            return null;
        }

        private bool TryLogin(string username, string password)
        {
            if (!IsLocal && username == "Player")
                return false;

            return Database.AllowUser(username, password);
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
            LarnixServer?.KillAllConnections();
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
