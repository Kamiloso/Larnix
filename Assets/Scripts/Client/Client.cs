using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Larnix.Model.Physics;
using Larnix.Patches;
using Larnix.Model.Packets;
using Larnix.Client.UI;
using Larnix.Client.Terrain.Selector;
using Larnix.Client.Entities;
using Larnix.Scoping;
using Larnix.Core;
using Larnix.Model;
using Larnix.Socket.Client;
using System;
using Larnix.Socket.Client.Records;
using Larnix.Core.Serialization;
using Larnix.Server.Run;

namespace Larnix.Client
{
    public class Client : MonoBehaviour
    {
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
        public float Ping => _larnixClient?.AvgRtt ?? 0f;

        private Loading Loading => GlobRef.Get<Loading>();
        private Inventory Inventory => GlobRef.Get<Inventory>();
        private TileSelector TileSelector => GlobRef.Get<TileSelector>();
        private EntityProjections EntityProjections => GlobRef.Get<EntityProjections>();
        private MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();
        private Screenshots Screenshots => GlobRef.Get<Screenshots>();

        private readonly Queue<Action> _delayedActions = new();
        private Task<QuickClient> _connectingTask;
        private QuickClient _larnixClient;

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

            FullLoginData loginData = new(
                Address: Address,
                Authcode: Authcode,
                Nickname: new FixedString32(Nickname),
                Password: new FixedString64(Password)
                );

            _connectingTask = Task.Run(() =>
                QuickClient.CreateClientAsync(loginData).Result);

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
                while (_delayedActions.Count > 0)
                {
                    _delayedActions.Dequeue().Invoke();
                }
                
                _larnixClient.Tick(Time.deltaTime);

                if (_larnixClient.IsDead)
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
                    CodeInfo payload = new(CodeInfo.Info.RespawnMe);
                    Send(payload);

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

        public void Send<T>(in T payload) where T : unmanaged
        {
            if (_larnixClient != null && _delayedActions.Count == 0)
            {
                _larnixClient.Send(payload);
            }
            else
            {
                T payloadCopy = payload;
                _delayedActions.Enqueue(
                    () => _larnixClient.Send(payloadCopy));
            }
        }

        public void SendUnreliable<T>(in T payload) where T : unmanaged
        {
            if (_larnixClient != null && _delayedActions.Count == 0)
            {
                _larnixClient.SendUnreliable(payload);
            }
            else
            {
                T payloadCopy = payload;
                _delayedActions.Enqueue(
                    () => _larnixClient.SendUnreliable(payloadCopy));
            }
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
