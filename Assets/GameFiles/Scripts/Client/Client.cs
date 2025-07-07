using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;
using Larnix.Socket;
using Larnix.Socket.Commands;
using System.Security.Cryptography;

namespace Larnix.Client
{
    public class Client : MonoBehaviour
    {
        public /*const*/ string Nickname = "Player" + (new System.Random()).Next(0, 100);
        public const string Password = "Haslo123";

        private Socket.Client LarnixClient = null;
        private IPEndPoint EndPoint = null;
        private Queue<Socket.PacketAndOwner> delayedPackets = new Queue<Socket.PacketAndOwner>();
        private RSA MyRSA = null;

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
                UnityEngine.Debug.Log("Remote world on address " + EndPoint.ToString());
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

            // Here server is already created and WorldLoad.ServerAddress has been set

            if (!CreateClient())
                yield break;
        }

        private bool CreateClient()
        {
            EndPoint = Socket.Resolver.ResolveString(WorldLoad.ServerAddress);
            if(EndPoint == null)
            {
                BackToMenu();
                return false;
            }

            if(WorldLoad.RsaPublicKey != null)
            {
                RSAParameters rsaParameters = new RSAParameters
                {
                    Modulus = WorldLoad.RsaPublicKey[0..256],
                    Exponent = WorldLoad.RsaPublicKey[256..264],
                };

                MyRSA = RSA.Create();
                MyRSA.ImportParameters(rsaParameters);
            }

            LarnixClient = new Socket.Client(EndPoint, Nickname, Password, MyRSA);
            return true;
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

            if(Input.GetKeyDown(KeyCode.Z))
            {
                DebugMessage debugMessage = new DebugMessage("Test message :)");
                if (!debugMessage.HasProblems)
                {
                    UnityEngine.Debug.Log("SENDING " + debugMessage.Data);
                    Send(debugMessage.GetPacket());
                }
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
                LarnixClient.Dispose();

            if(MyRSA != null)
                MyRSA.Dispose();

            WorldLoad.RsaPublicKey = null;
        }
    }
}
