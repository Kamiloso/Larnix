using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;
using Larnix.Socket;
using UnityEditor.VersionControl;
using Larnix.Socket.Commands;

namespace Larnix.Client
{
    public class Client : MonoBehaviour
    {
        public const string Nickname = "PlayerTester";
        public const string Password = "Haslo123";

        Socket.Client LarnixClient = null;
        IPEndPoint EndPoint = null;
        Queue<Socket.PacketAndOwner> delayedPackets = new Queue<Socket.PacketAndOwner>();

        // Client initialization
        void Awake()
        {
            var load_type = WorldLoad.LoadType;

            if (load_type == WorldLoad.LoadTypes.Local)
            {
                StartCoroutine(CreateServer());
            }
            else if (load_type == WorldLoad.LoadTypes.Remote)
            {
                CreateClient();
                UnityEngine.Debug.Log("Remote world [???] on address " + EndPoint.ToString());
            }
            else
            {
                SceneManager.LoadScene("Menu");
                return;
            }
        }

        // Server creation
        private IEnumerator CreateServer()
        {
            AsyncOperation serverCreation = SceneManager.LoadSceneAsync("Server", LoadSceneMode.Additive);
            while(!serverCreation.isDone)
                yield return null;

            WorldLoad.GenerateLocalAddress();
            CreateClient();
            UnityEngine.Debug.Log("Local world [" + WorldLoad.WorldDirectory + "] on address " + EndPoint.ToString());
        }

        private void CreateClient()
        {
            EndPoint = Socket.DnsResolver.ResolveString(WorldLoad.ServerAddress);
            LarnixClient = new Socket.Client(EndPoint, Nickname, Password);
        }

        private void Send(Packet packet, bool safemode = true)
        {
            if (LarnixClient != null && delayedPackets.Count == 0)
                LarnixClient.Send(packet, safemode);
            else
                delayedPackets.Enqueue(new PacketAndOwner(safemode ? "SAFE" : "FAST", packet));
        }

        private void Update()
        {
            if(LarnixClient != null)
            {
                while (delayedPackets.Count > 0)
                {
                    PacketAndOwner pack = delayedPackets.Dequeue();
                    LarnixClient.Send(pack.Packet, pack.Nickname == "SAFE");
                }

                Queue<Packet> packets = LarnixClient.ClientTickAndReceive(Time.deltaTime);
                foreach (Packet packet in packets)
                {
                    //
                }

                if (LarnixClient.IsDead())
                    BackToMenu();
            }

            if(Input.GetKeyDown(KeyCode.Escape))
            {
                BackToMenu();
            }
        }

        public void BackToMenu()
        {
            if (LarnixClient != null)
                LarnixClient.KillConnection();

            SceneManager.LoadScene("Menu");
        }
        private void OnDestroy()
        {
            if (LarnixClient != null)
                LarnixClient.DisposeUdp();
        }
    }
}
