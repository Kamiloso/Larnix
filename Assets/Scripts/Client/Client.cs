using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Larnix.Socket.Packets;
using System.Threading.Tasks;
using Larnix.Model.Physics;
using Larnix.Server;
using Larnix.Socket.Frontend;
using Larnix.Patches;
using Larnix.Server.Packets;
using Larnix.Client.UI;
using Larnix.Client.Terrain.Selector;
using Larnix.Client.Entities;
using Larnix.Scoping;
using Larnix.Core;
using Larnix.Model;

namespace Larnix.Client
{
    public class Client : MonoBehaviour
    {
        private record DelayedPacket(Payload_Legacy Packet, bool Safemode);

        private QuickClient _larnixClient;
        private Task<QuickClient> _connectingTask;
        private Queue<DelayedPacket> _delayedPackets = new();

        private Loading Loading => GlobRef.Get<Loading>();
        private Inventory Inventory => GlobRef.Get<Inventory>();
        private TileSelector TileSelector => GlobRef.Get<TileSelector>();
        private EntityProjections EntityProjections => GlobRef.Get<EntityProjections>();
        private MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();
        private Screenshots Screenshots => GlobRef.Get<Screenshots>();

        // --- CONSTANT VALUES ---
        public string Address { get; private set; }
        public string Authcode { get; private set; }
        public string Nickname { get; private set; }
        public string Password { get; private set; }
        public string WorldPath { get; private set; }
        public bool IsMultiplayer { get; private set; }

        // --- CHANGABLE PROPERTIES ---
        public bool IsGameFocused { get; private set; } = true; // start focused
        public uint FixedFrame { get; private set; }
        public float Ping => _larnixClient?.GetPing() ?? 0f;

        void Awake()
        {
            if (!WorldLoad.PlayedAlready)
            {
                BackToMenu();
                return;
            }

            EarlyUpdateInjector.InjectEarlyUpdate(EarlyUpdate, order: 0);

            GlobRef.Set(this);
            GlobRef.Set<IPhysicsManager>(
                new PhysicsManager(Common.PhysicsSectorSize)
                );

            WorldPath = WorldLoad.WorldPath;
            IsMultiplayer = WorldLoad.IsMultiplayer;

            Scopes.Reset();

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

            _connectingTask = Task.Run(() =>
                QuickClient.CreateClientAsync(Address, Authcode, Nickname, Password).Result);

            while (!_connectingTask.IsCompleted)
            {
                yield return null;
            }

            if (_connectingTask.Result != null)
            {
                _larnixClient = _connectingTask.Result;
                _ = new Receiver(_larnixClient);
                Echo.Log($"{(IsMultiplayer ? "Remote" : "Local")} world on address {Address}");
            }
            else
            {
                Echo.LogError("Failed creating client! Returning to menu...");
                BackToMenu();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            IsGameFocused = hasFocus;
        }

        private void FixedUpdate()
        {
            FixedFrame++;

            if (MainPlayer.Alive) // 1
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
            if (MyInput.GetKeyDown(KeyCode.R)) // temporary respawn using R
            {
                if (!MainPlayer.gameObject.activeInHierarchy)
                {
                    Payload_Legacy packet = new CodeInfo(CodeInfo.Info.RespawnMe);
                    Send(packet);

                    Loading.StartLoading("Respawning...");
                }
            }

            Inventory.Update1();
            TileSelector.Update2();

            if (MyInput.GetKeyDown(KeyCode.Escape))
            {
                BackToMenu();
            }
        }

        public void Send(Payload_Legacy packet, bool safemode = true)
        {
            if (_larnixClient != null && _delayedPackets.Count == 0)
                _larnixClient.Send(packet, safemode);
            else
                _delayedPackets.Enqueue(new DelayedPacket(packet, safemode));
        }

        public void BackToMenu()
        {
            Screenshots.TryCaptureTitleImage();
            SceneManager.LoadScene("Menu");
        }

        private void OnDestroy()
        {
            if (_connectingTask != null && _larnixClient == null)
            {
                _larnixClient = _connectingTask.Result; // wait for finish if still running
            }

            _larnixClient?.Dispose();
            WorldLoad.SetStartingScreen(IsMultiplayer ? "Multiplayer" : "Singleplayer");
            EarlyUpdateInjector.UninjectEarlyUpdate(EarlyUpdate);

            ServerRunner.Instance.Stop(); // if any
        }
    }
}
