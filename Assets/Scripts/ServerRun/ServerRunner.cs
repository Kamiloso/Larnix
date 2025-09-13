using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Larnix.Server;

namespace Larnix.ServerRun
{
    public class ServerRunner : MonoBehaviour, IGlobalUnitySingleton
    {
        public bool IsRunning => _server != null;

        private Server.Server _server = null;
        private Thread _thread = null;
        private Exception _threadException = null;

        public volatile bool StopFlag = false;

        public static ServerRunner Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
                throw new System.Exception("Cannot create more than one instance of the server instancer.");

            Instance = this;
        }

        public (string address, string authcode) StartServer(ServerType type, string worldPath, long? seedSuggestion)
        {
            StopServerSync(); // ensure slot emptiness

            _server = new Server.Server(type, worldPath, seedSuggestion, () => StopFlag = true);
            var tuple = (_server.LocalAddress, _server.Authcode);

            _thread = new Thread(() =>
            {
                const float PERIOD = Core.Common.FIXED_TIME;
                Stopwatch sw = Stopwatch.StartNew();
                bool crashed = false;

                try
                {
                    const long MAX_FRAME_DELAY = 5;
                    long frame = 0;

                    while (!StopFlag)
                    {
                        _server.TickFixed();
                        frame++;

                        while (sw.Elapsed.TotalSeconds > (frame + MAX_FRAME_DELAY) * PERIOD)
                        {
                            frame++; // we're behind, skip frame
                        }

                        while (sw.Elapsed.TotalSeconds < frame * PERIOD)
                        {
                            Thread.Sleep(1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    crashed = true;
                    _threadException = ex;
                }
                finally
                {
                    sw.Stop();
                    _server.Dispose(crashed);
                }
            })
            { 
                IsBackground = true
            };
            _thread.Start();

            return tuple;
        }

        private void LateUpdate()
        {
            if (IsRunning)
            {
                if (!_thread.IsAlive)
                {
                    StopServerSync();
                }
            }
        }

        public void StopServerSync()
        {
            if (IsRunning)
            {
                try
                {
                    StopFlag = true;
                    _thread.Join();

                    if (_threadException != null)
                        ExceptionDispatchInfo.Capture(_threadException).Throw();
                }
                finally
                {
                    _server = null;
                    _thread = null;
                    _threadException = null;

                    StopFlag = false;
                }
            }
        }

        private void OnDestroy()
        {
            StopServerSync();
        }
    }
}
