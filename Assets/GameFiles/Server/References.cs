using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Terrain;
using Larnix.Server.Entities;
using Larnix.Server.Worldgen;

namespace Larnix.Server
{
    public static class References // SERVER GLOBAL REFERENCES (set in Awake())
    {
        // Mono Behaviours
        public static Server Server;
        public static EntityDataManager EntityDataManager;
        public static EntityManager EntityManager;
        public static ChunkLoading ChunkLoading;
        public static PlayerManager PlayerManager;
        public static BlockDataManager BlockDataManager;
        public static BlockSender BlockSender;

        // Normal Classes
        public static Generator Generator;
    }
}
