using System;
using Larnix.Core;

namespace Larnix.Server
{
    internal class Clock : ITickable
    {
        public long ServerTick { get; private set; }
        public uint FixedFrame { get; private set; }

        private float? _deltaTime = null;
        private InvalidOperationException NoDeltaTimeException =>
            new("Delta time uninitialized.");
        
        public float DeltaTime => _deltaTime != null ?
            _deltaTime.Value : throw NoDeltaTimeException;
        public float TPS => _deltaTime != null ?
            (1f / _deltaTime.Value) : throw NoDeltaTimeException;

        public Clock(long serverTick)
        {
            ServerTick = serverTick;
            FixedFrame = 1;
        }

        public void Tick(float deltaTime)
        {
            _deltaTime = deltaTime;
            FixedFrame++;
            ServerTick++;
        }
    }
}
