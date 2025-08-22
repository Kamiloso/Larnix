using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Socket.Commands
{
    public enum Name : byte
    {
        // Connection commands
        None,
        AllowConnection,
        Stop,
        DebugMessage,

        // Prompts and answers
        P_ServerInfo,
        A_ServerInfo,
        P_LoginTry,
        A_LoginTry,

        // Game commands
        PlayerInitialize,
        PlayerUpdate,
        EntityBroadcast,
        NearbyEntities,
        CodeInfo,
        ChunkInfo,
        BlockUpdate,
        BlockChange,
        RetBlockChange,
    }
}
