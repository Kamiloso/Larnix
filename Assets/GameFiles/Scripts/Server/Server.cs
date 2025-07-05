using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Larnix.Socket;
using UnityEditor;
using Larnix.Socket.Commands;

namespace Larnix.Server
{
    public class Server : MonoBehaviour
    {
        private Socket.Server LarnixServer = null;
        public Config ServerConfig { get; private set; } = null;
        public ushort RealPort { get; private set; } = 0;

        // Server initialization
        private void Awake()
        {
            ServerConfig = new Config();
            LarnixServer = new Socket.Server(
                ServerConfig.Port,
                ServerConfig.MaxPlayers,
                ServerConfig.CompleteRSA
            );
            RealPort = LarnixServer.Port;
            UnityEngine.Debug.Log("Server started");
        }

        private void Update()
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

                    UnityEngine.Debug.Log("RECEIVED " + msg.Data);
                }
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
        private void OnDestroy()
        {
            if (LarnixServer != null) {
                LarnixServer.KillAllConnections();
                LarnixServer.Dispose();
            }

            if (ServerConfig != null) {
                ServerConfig.Dispose();
            }

            UnityEngine.Debug.Log("Server close");
        }
    }
}
