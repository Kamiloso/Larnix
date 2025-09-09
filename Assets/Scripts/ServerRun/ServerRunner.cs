using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Threading;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Larnix.Server;

namespace Larnix.ServerRun
{
    public class ServerRunner : MonoBehaviour, IGlobalUnitySingleton
    {
        public bool IsRunning => _server != null;

        public volatile bool StopFlag = false;
        private Server.Server _server = null;
        private Task _task = null;

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

            _task = Task.Run(() =>
            {
                const float PERIOD = Server.Server.FIXED_TIME;
                Stopwatch sw = Stopwatch.StartNew();
                bool crashed = false;

                try
                {
                    while (!StopFlag)
                    {
                        sw.Restart();
                        _server.TickFixed();
                        sw.Stop();

                        float sleepSeconds = PERIOD - (float)sw.Elapsed.TotalSeconds;
                        if (sleepSeconds > 0)
                        {
                            Thread.Sleep((int)Math.Ceiling(sleepSeconds * 1000f));
                        }
                    }
                }
                catch
                {
                    crashed = true;
                    throw;
                }
                finally
                {
                    sw.Stop();
                    _server.Dispose(crashed);
                }
            });

            return tuple;
        }

        private void LateUpdate()
        {
            if (IsRunning)
            {
                if (_task.IsCompleted)
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
                    _task.Wait();
                }
                catch (AggregateException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                finally
                {
                    _server = null;
                    _task = null;

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
