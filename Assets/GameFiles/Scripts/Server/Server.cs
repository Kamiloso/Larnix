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

namespace Larnix.Server
{
    public class Server : MonoBehaviour
    {
        private const float DATA_SAVING_PERIOD = 15f; // seconds

        private Socket.Server LarnixServer = null;
        private RSA CompleteRSA = null;

        public bool IsLocal { get; private set; } = false;
        public Config ServerConfig = null;

        // Server initialization
        private void Awake()
        {
            if(WorldLoad.LoadType == WorldLoad.LoadTypes.None)
                WorldLoad.StartServer();
            IsLocal = WorldLoad.LoadType == WorldLoad.LoadTypes.Local;

            ServerConfig = Config.Obtain(IsLocal);
            CompleteRSA = KeyObtainer.ObtainKeyRSA(IsLocal);

            LarnixServer = new Socket.Server(
                ServerConfig.Port,
                ServerConfig.MaxPlayers,
                ServerConfig.AllowRemoteClients,
                CompleteRSA,
                AnswerToNcnPacket
            );
            WorldLoad.ServerAddress = "localhost:" + LarnixServer.Port;

            if (IsLocal)
                WorldLoad.RsaPublicKey = KeyToPublicBytes(CompleteRSA);
        }

        private bool updateStartDone = false;
        private float saveCycleTimer = 0f;

        private void Update()
        {
            if(!updateStartDone)
            {
                UnityEngine.Debug.Log("Done! Server started on port " + LarnixServer.Port);
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

                    UnityEngine.Debug.Log("Player [" + owner + "] joined.");
                }

                if((Name)packet.ID == Name.Stop)
                {
                    Stop msg = new Stop(packet);
                    if(msg.HasProblems) continue;

                    UnityEngine.Debug.Log("Player [" + owner + "] disconnected.");
                }

                if((Name)packet.ID == Name.DebugMessage)
                {
                    DebugMessage msg = new DebugMessage(packet);
                    if (msg.HasProblems) continue;

                    UnityEngine.Debug.Log("DebugMessage [" + owner + "]: " + msg.Data);
                }
            }

            saveCycleTimer += Time.deltaTime;
            if(saveCycleTimer > DATA_SAVING_PERIOD)
            {
                saveCycleTimer = 0;
                SaveAllNow();
            }
        }

        public void Send(string nickname, Packet packet)
        {
            LarnixServer.Send(nickname, packet);
        }
        public void Broadcast(Packet packet)
        {
            LarnixServer.Broadcast(packet);
        }

        public Packet AnswerToNcnPacket(Packet packet)
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
                    0
                    );
                if (answer.HasProblems)
                    throw new Exception("Error making server info answer.");

                return answer.GetPacket();
            }

            if ((Name)packet.ID == Name.P_PasswordChange)
            {
                UnityEngine.Debug.LogWarning("Password system not implemented yet.");
                return null;
            }

            return null;
        }

        private byte[] KeyToPublicBytes(RSA rsa)
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
            Config.Save(ServerConfig);
        }

        private void OnDestroy()
        {
            if (LarnixServer != null) {
                LarnixServer.KillAllConnections();
                LarnixServer.Dispose();
            }

            if (CompleteRSA != null) {
                CompleteRSA.Dispose();
            }

            SaveAllNow();
            UnityEngine.Debug.Log("Server close");
        }
    }
}
