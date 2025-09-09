using System.Collections;
using System.Collections.Generic;
using Larnix.Server.Terrain;
using Larnix.Server.Entities;
using Larnix.Worldgen;
using Larnix.Core.Physics;
using QuickNet.Backend;
using Larnix.Server.Data;

namespace Larnix.Server
{
    internal static class Ref // SERVER GLOBAL REFERENCES (set in Awake())
    {
        // Mono Behaviours
        public static Server Server;

        // Normal Classes
        public static Generator Generator;
        public static PhysicsManager PhysicsManager;
        public static QuickServer QuickServer;
        public static Config Config;
        public static Database Database;

        // Server Scripts
        public static EntityDataManager EntityDataManager;
        public static EntityManager EntityManager;
        public static ChunkLoading ChunkLoading;
        public static PlayerManager PlayerManager;
        public static BlockDataManager BlockDataManager;
        public static BlockSender BlockSender;
    }
}
