using Larnix.Blocks;
using Larnix.Core;
using Larnix.Core.Files;
using Larnix.Core.Physics;
using Larnix.Server.Commands;
using Larnix.Server.Data;
using Larnix.Server.Entities;
using Larnix.Server.Terrain;
using Larnix.Socket.Backend;
using Larnix.Worldgen;

namespace Larnix.Server
{
    internal class Ref
    {
        private static T Get<T>() where T : class => GlobRef.Get<T>();
        
        // --- SERVER SINGLETONS ---
        public static Server Server => Get<Server>();
        public static Locker Locker => Get<Locker>();
        public static Config Config => Get<Config>();
        public static Database Database => Get<Database>();
        public static EntityManager EntityManager => Get<EntityManager>();
        public static PlayerManager PlayerManager => Get<PlayerManager>();
        public static EntityDataManager EntityDataManager => Get<EntityDataManager>();
        public static BlockDataManager BlockDataManager => Get<BlockDataManager>();
        public static UserManager UserManager => Get<UserManager>();
        public static Receiver Receiver => Get<Receiver>();
        public static QuickServer QuickServer => Get<QuickServer>();
        public static Chunks Chunks => Get<Chunks>();
        public static AtomicChunks AtomicChunks => Get<AtomicChunks>();
        public static BlockSender BlockSender => Get<BlockSender>();
        public static Generator Generator => Get<Generator>();
        public static CmdManager CmdManager => Get<CmdManager>();
        public static PhysicsManager PhysicsManager => Get<PhysicsManager>();
        public static IWorldAPI IWorldAPI => Get<IWorldAPI>();
    }
}
