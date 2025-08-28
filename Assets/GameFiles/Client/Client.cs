using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;
using Larnix.Socket.Commands;
using System.Security.Cryptography;
using Larnix.Socket.Data;
using Larnix.Socket.Channel;
using Larnix.Socket.Frontend;
using System.Threading.Tasks;

namespace Larnix.Client
{
    public class Client : MonoBehaviour
    {
        private Socket.Frontend.Client LarnixClient = null;
        private Queue<(Packet packet, bool safemode)> delayedPackets = new();
        private Receiver Receiver = null;

        private string Address;
        private string Authcode;
        private string Nickname;
        private string Password;

        public bool IsMultiplayer;
        public ulong MyUID = 0;

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
                StartCoroutine(CreateClient());
            }
            else
            {
                StartCoroutine(CreateServerAndClient());
            }
        }

        private void Start()
        {
            References.Loading.StartLoading("Connecting...");
        }

        // Server creation
        private IEnumerator CreateServerAndClient()
        {
            AsyncOperation serverCreation = SceneManager.LoadSceneAsync("Server", LoadSceneMode.Additive);
            yield return new WaitUntil(() => serverCreation.isDone);

            StartCoroutine(CreateClient());
        }

        private IEnumerator CreateClient()
        {
            Address = WorldLoad.Address;
            Authcode = WorldLoad.Authcode;
            Nickname = WorldLoad.Nickname;
            Password = WorldLoad.Password;

            Task<Socket.Frontend.Client> connecting = Socket.Frontend.Client.CreateClientAsync(Address, Authcode, Nickname, Password);
            yield return new WaitUntil(() => connecting.IsCompleted);

            if (connecting.Result != null)
            {
                LarnixClient = connecting.Result;
                Receiver = new Receiver(LarnixClient);
                UnityEngine.Debug.Log($"{(IsMultiplayer ? "Remote" : "Local")} world on address {Address}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("Failed creating client! Returning to menu...");
                BackToMenu();
            }
        }

        public void Send(Packet packet, bool safemode = true)
        {
            if (LarnixClient != null && delayedPackets.Count == 0)
                LarnixClient.Send(packet, safemode);
            else
                delayedPackets.Enqueue((packet, safemode));
        }

        private void EarlyUpdate() // Executes BEFORE default Update() time
        {
            if(LarnixClient != null)
            {
                while (delayedPackets.Count > 0)
                {
                    var pack = delayedPackets.Dequeue();
                    LarnixClient.Send(pack.packet, pack.safemode);
                }

                LarnixClient.ClientTick(Time.deltaTime);

                if (LarnixClient.IsDead())
                {
                    BackToMenu();
                    return;
                }
            }

            References.EntityProjections.AfterBroadcasts();
        }

        private void Update()
        {
            // Base input

            if (Input.GetKeyDown(KeyCode.R)) // temporary respawn using R
            {
                if(!References.MainPlayer.gameObject.activeInHierarchy)
                {
                    CodeInfo codeInfo = new CodeInfo((byte)CodeInfo.Info.RespawnMe);
                    if (codeInfo.HasProblems)
                        throw new System.Exception("Wrong respawn ask packet!");
                    
                    Send(codeInfo.GetPacket());
                    References.Loading.StartLoading("Respawning...");
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                BackToMenu();
            }
        }

        public void BackToMenu()
        {
            SceneManager.LoadScene("Menu");

            if (WorldLoad.LoadType != WorldLoad.LoadTypes.None && !IsMultiplayer)
            {
                if (References.MainPlayer.IsAlive)
                    References.Screenshots.CaptureTitleImage();
                //else
                //    References.Screenshots.RemoveTitleImage();
            }
        }

        private void OnDestroy()
        {
            LarnixClient?.Dispose();
            WorldLoad.ScreenLoad = IsMultiplayer ? "Multiplayer" : "Singleplayer";
            EarlyUpdateInjector.ClearEarlyUpdate();
        }
    }
}
