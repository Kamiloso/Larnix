using System.Collections;
using System.Collections.Generic;

namespace Larnix.Socket.Commands
{
    public enum CmdID : byte
    {
        // Core commands (protocol-critical)
        None = 0,
        AllowConnection = 1,
        Stop = 2,
        DebugMessage = 3, // not used - for debugging
        P_ServerInfo = 4,
        A_ServerInfo = 5,
        P_LoginTry = 6,
        A_LoginTry = 7,

        // User commands
        PlayerInitialize = 8,
        PlayerUpdate = 9,
        EntityBroadcast = 10,
        NearbyEntities = 11,
        CodeInfo = 12,
        ChunkInfo = 13,
        BlockUpdate = 14,
        BlockChange = 15,
        RetBlockChange = 16,
    }
}
