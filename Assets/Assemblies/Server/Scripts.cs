using System;
using System.Collections.Generic;
using Larnix.Core;

namespace Larnix.Server
{
    internal class Scripts : ITickable
    {
        private readonly IScript[] _scripts;
        private bool _startExecuted = false;

        public Scripts(params IScript[] scripts)
        {
            _scripts = scripts[..];
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
}
