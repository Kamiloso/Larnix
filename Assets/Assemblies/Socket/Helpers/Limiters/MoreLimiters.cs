using System;

namespace Larnix.Socket.Helpers.Limiters
{
    public readonly struct MoreLimiters
    {
        private readonly (Func<bool> TryAdd, Action Remove)[] _limiters;

        public MoreLimiters(params (Func<bool> TryAdd, Action Remove)[] limiters)
        {
            _limiters = limiters;
        }

        public bool TryAdd()
        {
            for (int i = 0; i < _limiters.Length; i++)
            {
                if (!_limiters[i].TryAdd())
                {
                    // Rollback limits
                    for (i -= 1; i >= 0; i--)
                    {
                        _limiters[i].Remove();
                    }
                    return false;
                }
            }
            return true;
        }

        public void Remove()
        {
            foreach (var limiter in _limiters)
            {
                limiter.Remove();
            }
        }

        public void RemoveOnly(params int[] indexes)
        {
            foreach (int i in indexes)
            {
                _limiters[i].Remove();
            }
        }
    }
}