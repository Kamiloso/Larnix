#nullable enable
using Larnix.Core;
using System.Linq;

namespace Larnix.Model;

public interface IScript
{
    void Start() { }

    void EarlyUpdate() { }
    void PostEarlyUpdate() { }
    void Update() { }
    void LateUpdate() { }
    void PostLateUpdate() { }
}

public interface IScripts : ITickable { }

public class Scripts : IScripts
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

                if (i == 1) singleton.EarlyUpdate();
                if (i == 2) singleton.PostEarlyUpdate();
                if (i == 3) singleton.Update();
                if (i == 4) singleton.LateUpdate();
                if (i == 5) singleton.PostLateUpdate();
            }
        }
        _startExecuted = true;
    }
}
