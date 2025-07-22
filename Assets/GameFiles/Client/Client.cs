using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;
using Larnix.Socket;
using Larnix.Socket.Commands;
using System.Security.Cryptography;
using Larnix.Entities;

namespace Larnix.Client
{
    public class Client : MonoBehaviour
    {
        private Socket.Client LarnixClient = null;
        private IPEndPoint EndPoint = null;
        private Queue<Socket.PacketAndOwner> delayedPackets = new Queue<Socket.PacketAndOwner>();
        private RSA MyRSA = null;

        public bool IsMultiplayer { get; private set; }
        public ulong MyUID { get; private set; } = 0;

        private bool loadingEnded = false;

        // Client initialization
        void Awake()
        {
            if(WorldLoad.LoadType == WorldLoad.LoadTypes.None)
            {
                BackToMenu();
                return;
            }

            EarlyUpdateInjector.InjectEarlyUpdate(this.EarlyUpdate);

            References.Client = this;
            IsMultiplayer = WorldLoad.LoadType == WorldLoad.LoadTypes.Remote;

            if (IsMultiplayer)
            {
                if (!CreateClient())
                    return;

                UnityEngine.Debug.Log("Remote world on address " + EndPoint.ToString());
            }
            else
            {
                StartCoroutine(CreateServer());
            }
        }

        private void Start()
        {
            References.LoadingScreen.Enable();
            References.LoadingScreen.info = "Connecting...";
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

            LarnixClient = new Socket.Client(EndPoint, WorldLoad.Nickname, WorldLoad.Password, MyRSA);
            return true;
        }

        public void Send(Packet packet, bool safemode = true)
        {
            if (LarnixClient != null && delayedPackets.Count == 0)
                LarnixClient.Send(packet, safemode);
            else
                delayedPackets.Enqueue(new PacketAndOwner(safemode ? "SAFE" : "FAST", packet));
        }

        private void EarlyUpdate() // Executes BEFORE default Update() time
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
                    if((Name)packet.ID == Name.PlayerInitialize)
                    {
                        PlayerInitialize msg = new PlayerInitialize(packet);
                        if (msg.HasProblems) continue;

                        References.MainPlayer.LoadPlayerData(msg);
                        MyUID = msg.MyUid;
                    }

                    if((Name)packet.ID == Name.EntityBroadcast)
                    {
                        EntityBroadcast msg = new EntityBroadcast(packet);
                        if (msg.HasProblems) continue;

                        if(MyUID != 0)
                        {
                            References.EntityProjections.InterpretEntityBroadcast(msg);
                            if(!loadingEnded)
                            {
                                References.LoadingScreen.Disable();
                                loadingEnded = true;
                            }
                        }
                    }
                }

                if (LarnixClient.IsDead())
                {
                    BackToMenu();
                    return;
                }
            }

            /*if(Input.GetKeyDown(KeyCode.Z))
            {
                DebugMessage debugMessage = new DebugMessage("Test message :)");
                if (!debugMessage.HasProblems)
                {
                    UnityEngine.Debug.Log("SENDING " + debugMessage.Data);
                    Send(debugMessage.GetPacket());
                }
            }*/

            if(Input.GetKeyDown(KeyCode.Escape))
            {
                BackToMenu();
            }
        }

        public void BackToMenu()
        {
            SceneManager.LoadScene("Menu");
        }

        private void OnDestroy()
        {
            LarnixClient?.Dispose();
            MyRSA?.Dispose();

            WorldLoad.RsaPublicKey = null;
            WorldLoad.ScreenLoad = IsMultiplayer ? "Multiplayer" : "Singleplayer";

            EarlyUpdateInjector.ClearEarlyUpdate();
        }
    }
}
