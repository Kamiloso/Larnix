using System;
using Larnix.Blocks;
using Larnix.Blocks.Structs;
using Larnix.Core.Utils;

namespace Larnix.Server.Commands
{
    public static class Parsing
    {
        public static bool TryParseFront(string input, out bool front)
        {
            string lower = input.ToLowerInvariant();
            front = lower == "front";
            return front || lower == "back";
        }

        public static bool TryParseUid(string input, out ulong uid)
        {
            if (ulong.TryParse(input, out ulong u_uid))
            {
                uid = u_uid;
                return true;
            }
            
            if (long.TryParse(input, out long s_uid))
            {
                uid = (ulong)s_uid;
                return true;
            }

            uid = default;
            return false;
        }

        public static bool TryParseBlock(string input, out BlockID blockID, out byte variant)
        {
            string[] parts = input.Split(':');
            if (parts.Length == 1 || parts.Length == 2)
            {
                if (Enum.TryParse(parts[0], ignoreCase: true, out blockID) &&
                    Enum.IsDefined(typeof(BlockID), blockID))
                {
                    if (parts.Length == 2)
                    {
                        if (byte.TryParse(parts[1], out variant) &&
                            variant <= BlockData1.MAX_VARIANT)
                        {
                            return true;
                        }
                        else
                        {
                            blockID = default;
                            variant = default;
                            return false;
                        }
                    }
                    else
                    {
                        variant = 0;
                        return true;
                    }
                }
            }

            blockID = default;
            variant = default;
            return false;
        }
    }
}
