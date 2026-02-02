using System;

namespace Larnix.Core.Json
{
    public static class Tags
    {
        public const char TO_BE_KILLED = 'K';

        public static void Apply(Storage storage, string key, char flag)
        {
            storage[key].String += flag;
        }

        public static bool TryConsume(Storage storage, string key, char flag)
        {
            string flags = storage[key].String;
            if (flags.Contains(flag))
            {
                int idx = flags.IndexOf(flag);
                if (idx >= 0)
                    storage[key].String = flags.Remove(idx, 1);
                return true;
            }
            return false;
        }
    }
}
