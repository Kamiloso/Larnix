using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Threading;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Larnix.Server
{
    public class ServerInstancer : MonoBehaviour
    {
        private readonly bool THREADED = true;

        public bool IsRunning => _server != null;

        public volatile bool StopFlag = false;
        private Server _server = null;
        private Task _task = null;

        public static ServerInstancer Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
                throw new System.Exception("Cannot create more than one instance of the server instancer.");

            Instance = this;
        }

        public (string address, string authcode) StartServer(ServerType type, string worldPath, long? seedSuggestion)
        {
            StopServerSync(); // ensure slot emptiness

            _server = new Server(type, worldPath, seedSuggestion, () => StopFlag = true);
            var tuple = (_server.LocalAddress, _server.Authcode);

            if (THREADED)
            {
                _task = Task.Run(() =>
                {
                    const float PERIOD = Server.FIXED_TIME;
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
            }
            else
            {
                _task = null;
            }

            return tuple;
        }

        private void FixedUpdate()
        {
            if (IsRunning)
            {
                if (THREADED)
                {
                    if (_task.IsCompleted)
                    {
                        StopServerSync();
                    }
                }
                else
                {
                    if (!StopFlag)
                    {
                        _server?.TickFixed();
                    }
                    else
                    {
                        StopServerSync();
                    }
                }
            }
        }

        public void StopServerSync()
        {
            if (IsRunning)
            {
                if (THREADED)
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
                        ResetServerSlot();
                    }
                }
                else
                {
                    _server?.Dispose(false);
                    ResetServerSlot();
                }
            }
        }

        private void ResetServerSlot()
        {
            _server = null;
            _task = null;

            StopFlag = false;
        }

        private void OnDestroy()
        {
            StopServerSync();
        }
    }
}
