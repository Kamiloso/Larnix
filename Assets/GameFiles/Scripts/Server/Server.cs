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
        public ushort Port { get; private set; } = 27682;
        public ushort MaxClients { get; private set; } = 12;

        Socket.Server LarnixServer = null;

        // Server initialization
        private void Awake()
        {
            if (WorldLoad.LoadType == WorldLoad.LoadTypes.Local)
                Port = 0;

            LarnixServer = new Socket.Server(Port, MaxClients);
            Port = LarnixServer.Port;
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
            }
        }

        public ushort GetRunningPort()
        {
            if (LarnixServer != null)
                return Port;
            else
                throw new Exception("Trying to obtain port of inactive server.");
        }

        // Server destruction
        private void OnDestroy()
        {
            if (LarnixServer != null)
            {
                LarnixServer.KillAllConnections();
                LarnixServer.DisposeUdp();
            }

            UnityEngine.Debug.Log("Server close");
        }
    }
}
