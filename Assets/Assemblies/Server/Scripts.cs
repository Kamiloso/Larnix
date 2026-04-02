#nullable enable
using Larnix.Core;
using System.Linq;

namespace Larnix.Server;

internal interface IScript
{
    void Start() { }

    void EarlyFrameUpdate() { }
    void PostEarlyFrameUpdate() { }
    void FrameUpdate() { }
    void LateFrameUpdate() { }
    void PostLateFrameUpdate() { }
}

internal class Scripts : ITickable
{
    private readonly IScript[] _scripts;
    private bool _startExecuted = false;

    public Scripts(params (int order, IScript[] scripts)[] allScripts)
    {
        _scripts = allScripts
            .OrderBy(t => t.order)
            .SelectMany(t => t.scripts)
            .ToArray();
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i <= 5; i++)
        {
            foreach (IScript singleton in _scripts)
            {
                if (i == 0 && !_startExecuted)
                {
                    singleton.Start();
                }

                if (i == 1) singleton.EarlyFrameUpdate();
                if (i == 2) singleton.PostEarlyFrameUpdate();
                if (i == 3) singleton.FrameUpdate();
                if (i == 4) singleton.LateFrameUpdate();
                if (i == 5) singleton.PostLateFrameUpdate();
            }
        }
        _startExecuted = true;
    }
}
