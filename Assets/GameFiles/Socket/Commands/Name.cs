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

        // Game commands
        PlayerInitialize,
        PlayerUpdate,
        EntityBroadcast,
        NearbyEntities,
        CodeInfo,
        ChunkInfo,
    }
}
