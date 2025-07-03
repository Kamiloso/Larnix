using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Larnix.Socket;

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
                UnityEngine.Debug.Log("Server received [" + message.Nickname + "] >> " + message.Packet.ID);
            }

            if(Input.GetKeyDown(KeyCode.S))
            {
                Packet packet = new Packet(102, 0, null);
                LarnixServer.Broadcast(packet, true);
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
            UnityEngine.Debug.Log("Server close");
        }
    }
}
