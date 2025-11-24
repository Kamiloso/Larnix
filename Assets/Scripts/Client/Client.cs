using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Larnix.Socket.Packets;
using Larnix.Socket.Channel;
using System.Threading.Tasks;
using Larnix.Core.Physics;
using Larnix.ServerRun;
using Larnix.Socket.Frontend;

namespace Larnix.Client
{
    public class Client : MonoBehaviour
    {
        public QuickClient LarnixClient = null;
        private Queue<(Packet packet, bool safemode)> delayedPackets = new();
        private Receiver Receiver = null;

        private string Address;
        private string Authcode;
        private string Nickname;
        private string Password;

        public string WorldPath;
        public bool IsMultiplayer;

        public ulong MyUID = 0;

        // Client initialization
        void Awake()
        {
            if (!WorldLoad.PlayedAlready)
            {
                BackToMenu();
                return;
            }

            EarlyUpdateInjector.InjectEarlyUpdate(this.EarlyUpdate);

            Ref.Client = this;
            Ref.PhysicsManager = new PhysicsManager();

            WorldPath = WorldLoad.WorldPath;
            IsMultiplayer = WorldLoad.IsMultiplayer;

            StartCoroutine(CreateClient());
        }

        private void Start()
        {
            Ref.Loading.StartLoading("Connecting...");
        }

        private IEnumerator CreateClient()
        {
            Address = WorldLoad.Address;
            Authcode = WorldLoad.Authcode;
            Nickname = WorldLoad.Nickname;
            Password = WorldLoad.Password;

            Task<QuickClient> connecting = Task.Run(() =>
                QuickClient.CreateClientAsync(Address, Authcode, Nickname, Password).Result);

            while (!connecting.IsCompleted)
                yield return null;

            if (connecting.Result != null)
            {
                LarnixClient = connecting.Result;
                Receiver = new Receiver(LarnixClient);
                Core.Debug.Log($"{(IsMultiplayer ? "Remote" : "Local")} world on address {Address}");
            }
            else
            {
                Core.Debug.LogError("Failed creating client! Returning to menu...");
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

            Ref.EntityProjections.EarlyUpdate1();
            Ref.MainPlayer.EarlyUpdate2();
        }

        private void Update()
        {
            // Debug input
            if (Input.GetKeyDown(KeyCode.R)) // temporary respawn using R
            {
                if (!Ref.MainPlayer.gameObject.activeInHierarchy)
                {
                    Packet packet = new CodeInfo(CodeInfo.Info.RespawnMe);
                    Send(packet);

                    Ref.Loading.StartLoading("Respawning...");
                }
            }

            // Ordered updates
            Ref.Inventory.Update1();
            Ref.TileSelector.Update2();

            // Escape input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                BackToMenu();
            }
        }

        public void BackToMenu()
        {
            SceneManager.LoadScene("Menu");

            if (WorldPath != null)
            {
                if (Ref.MainPlayer.IsAlive)
                    Ref.Screenshots.CaptureTitleImage();
                //else
                //    References.Screenshots.RemoveTitleImage();
            }
        }

        private void OnDestroy()
        {
            LarnixClient?.Dispose();
            WorldLoad.SetStartingScreen(IsMultiplayer ? "Multiplayer" : "Singleplayer");
            EarlyUpdateInjector.ClearEarlyUpdate();

            // Close server if running any
            ServerRunner.Instance.StopServerSync();
        }
    }
}
