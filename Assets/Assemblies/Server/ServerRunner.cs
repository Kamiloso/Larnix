#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Larnix.Core;
using Larnix.Model;

namespace Larnix.Server;

public enum ServerType
{
    Local, // pure singleplayer
    Host, // singleplayer published for remote hosts
    Remote, // multiplayer console server
}

public sealed class ServerRunner : IDisposable
{
    private static float Period => GameInfo.FixedTime;
    private static long MaxFrameDelay => 5;

    public record ServerAnswer(
        string Address,
        string Authcode,
        Task<string?>? RelayTask = null
    );

    public record RunSuggestions(
        long? Seed = null,
        string? RelayAddress = null
    );

    public bool HasServer => _server != null;
    public bool IsRunning => _thread?.IsAlive ?? false;

    private IServerHandle? _server;
    private Thread? _thread;
    private Exception? _threadException;

    private volatile bool _stop;

    public ServerRunner() { } // can be instantiated and managed
    public static ServerRunner Instance { get; } = new(); // legacy singleton

    public ServerAnswer Start(ServerType type, string worldPath, RunSuggestions suggestions)
    {
        Stop();

        long clientKey = GlobRef.GetKey(); // remember client scope
        long serverKey = GlobRef.NewScope(); // create server scope

        ServerAnswer answer;

        try
        {
            void StopSignal() => _stop = true;

            _server = new ServerHandle(
                type, worldPath, suggestions, StopSignal);

            answer = _server.Answer;
        }
        finally
        {
            GlobRef.SetKey(clientKey); // restore client scope
        }

        _thread = new Thread(() =>
        {
            GlobRef.SetKey(serverKey); // activate server scope
            try
            {
                ServerLoop();
            }
            finally
            {
                GlobRef.Clear(); // clear server scope
            }
        })
        {
            IsBackground = true,
            Name = "Larnix::ServerThread"
        };

        _thread.Start();
        return answer;
    }

    private void ServerLoop()
    {
        var sw = Stopwatch.StartNew();
        long frame = 0;
        bool crashed = false;

        try
        {
            double lastTime = sw.Elapsed.TotalSeconds - Period;

            while (!_stop)
            {
                double currentTime = sw.Elapsed.TotalSeconds;
                double deltaTime = currentTime - lastTime;

                _server!.Tick((float)deltaTime + float.Epsilon);
                frame++;

                while (sw.Elapsed.TotalSeconds > (frame + MaxFrameDelay) * Period)
                {
                    frame++;
                }

                while (sw.Elapsed.TotalSeconds < frame * Period)
                {
                    double sleepTime = frame * Period - sw.Elapsed.TotalSeconds;
                    if (sleepTime > 0.015)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        Thread.Sleep(0);
                    }
                }

                lastTime = currentTime;
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
            _server!.Dispose(crashed);
        }
    }

    public void Stop()
    {
        if (HasServer)
        {
            try
            {
                _stop = true;
                _thread!.Join();

                if (_threadException != null)
                    ExceptionDispatchInfo.Capture(_threadException).Throw();
            }
            finally
            {
                _server = null;
                _thread = null;
                _threadException = null;
                _stop = false;
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
