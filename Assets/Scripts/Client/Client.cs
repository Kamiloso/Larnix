using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Larnix.Socket.Packets;
using System.Threading.Tasks;
using Larnix.Core.Physics;
using Larnix.Server;
using Larnix.Socket.Frontend;
using Larnix.Patches;
using Larnix.Packets;
using Larnix.Client.UI;
using Larnix.Client.Terrain;
using Larnix.Client.Entities;
using Larnix.Core;

namespace Larnix.Client
{
    public class Client : MonoBehaviour
    {
        private record DelayedPacket(Payload Packet, bool Safemode);

        private QuickClient _larnixClient = null;
        private Queue<DelayedPacket> _delayedPackets = new();
        private Receiver _receiver = null;

        private Loading Loading => GlobRef.Get<Loading>();
        private Inventory Inventory => GlobRef.Get<Inventory>();
        private TileSelector TileSelector => GlobRef.Get<TileSelector>();
        private EntityProjections EntityProjections => GlobRef.Get<EntityProjections>();
        private MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();
        private Screenshots Screenshots => GlobRef.Get<Screenshots>();

        public string Address { get; private set; }
        public string Authcode { get; private set; }
        public string Nickname { get; private set; }
        public string Password { get; private set; }
        public string WorldPath { get; private set; }
        public bool IsMultiplayer { get; private set; }

        private uint _fixedFrame = 0;
        public uint FixedFrame => _fixedFrame;

        void Awake()
        {
            if (!WorldLoad.PlayedAlready)
            {
                BackToMenu();
                return;
            }

            EarlyUpdateInjector.InjectEarlyUpdate(this.EarlyUpdate);

            GlobRef.Set(this);
            GlobRef.Set(new PhysicsManager());

            WorldPath = WorldLoad.WorldPath;
            IsMultiplayer = WorldLoad.IsMultiplayer;

            StartCoroutine(CreateClient());
        }

        private void Start()
        {
            Loading.StartLoading("Connecting...");
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
                _larnixClient = connecting.Result;
                _receiver = new Receiver(_larnixClient);
                Core.Debug.Log($"{(IsMultiplayer ? "Remote" : "Local")} world on address {Address}");
            }
            else
            {
                Core.Debug.LogError("Failed creating client! Returning to menu...");
                BackToMenu();
            }
        }

        public void Send(Payload packet, bool safemode = true)
        {
            if (_larnixClient != null && _delayedPackets.Count == 0)
                _larnixClient.Send(packet, safemode);
            else
                _delayedPackets.Enqueue(new DelayedPacket(packet, safemode));
        }

        private void FixedUpdate()
        {
            _fixedFrame++;

            if (MainPlayer.IsAlive) // 1
                MainPlayer.FixedPlayerUpdate();
        }

        private void EarlyUpdate() // Executes BEFORE default Update() time
        {
            if(_larnixClient != null)
            {
                while (_delayedPackets.Count > 0)
                {
                    DelayedPacket pack = _delayedPackets.Dequeue();
                    _larnixClient.Send(pack.Packet, pack.Safemode);
                }
                
                _larnixClient.Tick(Time.deltaTime);

                if (_larnixClient.IsDead())
                {
                    BackToMenu();
                    return;
                }
            }

            EntityProjections.EarlyUpdate1();
            MainPlayer.EarlyUpdate2();
        }

        private void Update()
        {
            // debug input
            if (Input.GetKeyDown(KeyCode.R)) // temporary respawn using R
            {
                if (!MainPlayer.gameObject.activeInHierarchy)
                {
                    Payload packet = new CodeInfo(CodeInfo.Info.RespawnMe);
                    Send(packet);

                    Loading.StartLoading("Respawning...");
                }
            }

            Inventory.Update1();
            TileSelector.Update2();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                BackToMenu();
            }
        }

        public float GetPing() => _larnixClient?.GetPing() ?? 0f;

        public void BackToMenu()
        {
            SceneManager.LoadScene("Menu");

            if (WorldPath != null)
            {
                if (MainPlayer.IsAlive)
                    Screenshots.CaptureTitleImage();
            }
        }

        private void OnDestroy()
        {
            _larnixClient?.Dispose();
            WorldLoad.SetStartingScreen(IsMultiplayer ? "Multiplayer" : "Singleplayer");
            EarlyUpdateInjector.ClearEarlyUpdate();

            ServerRunner.Instance.Stop(); // if any
        }
    }
}
