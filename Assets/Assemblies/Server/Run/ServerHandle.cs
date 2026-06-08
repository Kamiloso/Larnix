#nullable enable
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Model;
using Larnix.Server.Run.Records;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Larnix.Server.Run;

internal class ServerHandle : IServerSpy, IDisposable
{
    public bool IsAlive => _thread?.IsAlive ?? false;
    public bool IsCrashed => _handle?.IsCrashed ?? false;
    public RunAnswer? RunAnswer => _answer;

    private readonly FileLock _mutex;
    private readonly ServerRoot _handle;
    private readonly Thread _thread;

    private volatile RunAnswer? _answer;
    private volatile Exception? _exception;
    private volatile bool _stop = false;

    private bool _stopped = false;

    public ServerHandle(RunInfo runInfo)
    {
        try
        {
            long clientKey = GlobRef.GetKey(); // remember client scope
            long serverKey = GlobRef.NewScope(); // create server scope

            string path = runInfo.SavesPath;
            string name = runInfo.WorldName;

            try
            {
                _mutex = FileLock.Acquire(path, $"~{name}.lock", 0);
                _handle = new ServerRoot(runInfo, new RunCallbacks(
                    SetAnswer: answer => _answer = answer,
                    StopSignal: () => _stop = true
                    ));
            }
            finally
            {
                GlobRef.Clear(); // clear server scope
                GlobRef.SetKey(clientKey); // restore client scope
            }

            _thread = new Thread(() => ServerThread(serverKey))
            {
                IsBackground = true,
                Name = $"Larnix::Server_{RandUtils.SecureLong()}"
            };

            _thread.Start();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void ServerThread(long serverKey)
    {
        GlobRef.SetKey(serverKey); // activate server scope

        try
        {
            ServerLoop();
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
        finally
        {
            GlobRef.Clear(); // clear server scope
        }
    }

    private void ServerLoop()
    {
        double PERIOD = GameInfo.FixedTime;
        double MAX_FRAME_DELAY = 5;

        Stopwatch sw = Stopwatch.StartNew();
        long frame = 0;

        double lastTime = sw.Elapsed.TotalSeconds - PERIOD;

        while (!_stop)
        {
            double currentTime = sw.Elapsed.TotalSeconds;
            double deltaTime = currentTime - lastTime;

            _handle.Tick((float)deltaTime + 0.0001f);

            frame++;

            while (sw.Elapsed.TotalSeconds > (frame + MAX_FRAME_DELAY) * PERIOD)
            {
                frame++;
            }

            while (sw.Elapsed.TotalSeconds < frame * PERIOD)
            {
                double sleepTime = frame * PERIOD - sw.Elapsed.TotalSeconds;
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

    public void StopSync()
    {
        if (_stopped) return;
        _stopped = true;

        _stop = true;
        _thread?.Join();

        if (_handle != null)
        {
            _handle.IsCrashed = _exception != null;
            _handle.Dispose();
        }

        _mutex?.Dispose();

        if (_exception != null)
        {
            ExceptionDispatchInfo.Capture(_exception).Throw();
        }
    }

    public void Dispose()
    {
        try { StopSync(); }
        catch
        {
            Echo.LogError($"An error was thrown while disposing the server!");
        }
    }
}
