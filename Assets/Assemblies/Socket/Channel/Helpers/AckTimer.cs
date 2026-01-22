using System;

namespace Larnix.Socket.Channel.Helpers
{
    internal class AckTimer
    {
        private readonly float PERIOD_SECONDS;
        private float _accumulator;
        private readonly Action _onTick;

        public bool Enabled { get; set; } = true;

        public AckTimer(float periodSeconds, Action onTick)
        {
            PERIOD_SECONDS = periodSeconds > 0f ?
                periodSeconds : throw new ArgumentException(nameof(periodSeconds));

            _accumulator = 0f;
            _onTick = onTick ?? (() => {});
        }

        public void Tick(float deltaTime)
        {
            if (!Enabled) return;

            _accumulator += deltaTime;
            if (_accumulator > PERIOD_SECONDS)
            {
                _onTick();
                _accumulator %= PERIOD_SECONDS;
            }
        }
    }
}
