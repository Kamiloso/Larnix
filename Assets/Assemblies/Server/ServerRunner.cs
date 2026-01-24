using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Larnix.Core.Utils;

namespace Larnix.Server
{
    public enum ServerType
    {
        Local, // pure singleplayer
        Host, // singleplayer published for remote hosts
        Remote, // multiplayer console server
    }

    public sealed class ServerRunner
    {
        public bool HasServer => _server != null;
        public bool IsRunning => HasServer && _thread.IsAlive;

        private Server _server;
        private Thread _thread;
        private Exception _threadException;

        private volatile bool _stopFlag;

        private static ServerRunner _badSingleton;
        public static ServerRunner Instance
        {
            get
            {
                if (_badSingleton == null)
                    _badSingleton = new ServerRunner();

                return _badSingleton;
            }
        }

        public (string address, string authcode) Start(
            ServerType type,
            string worldPath,
            long? seedSuggestion)
        {
            Stop();

            _server = new Server(
                type,
                worldPath,
                seedSuggestion,
                () => _stopFlag = true);

            var result = (_server.LocalAddress, _server.Authcode);

            _thread = new Thread(ServerLoop)
            {
                IsBackground = true,
                Name = "Larnix.ServerThread"
            };

            _thread.Start();
            return result;
        }

        private void ServerLoop()
        {
            const float PERIOD = Common.FIXED_TIME;
            const long MAX_FRAME_DELAY = 5;

            var sw = Stopwatch.StartNew();
            long frame = 0;
            bool crashed = false;

            try
            {
                while (!_stopFlag)
                {
                    _server.TickFixed();
                    frame++;

                    while (sw.Elapsed.TotalSeconds > (frame + MAX_FRAME_DELAY) * PERIOD)
                        frame++;

                    while (sw.Elapsed.TotalSeconds < frame * PERIOD)
                        Thread.Sleep(1);
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
        }

        public async Task<string> ConnectToRelayAsync(string address)
        {
            var server = _server;
            return await server?.EstablishRelayAsync(address);
        }

        public void Stop()
        {
            if (!HasServer)
                return;

            try
            {
                _stopFlag = true;
                _thread.Join();

                if (_threadException != null)
                    ExceptionDispatchInfo.Capture(_threadException).Throw();
            }
            finally
            {
                _server = null;
                _thread = null;
                _threadException = null;
                _stopFlag = false;
            }
        }
    }
}
