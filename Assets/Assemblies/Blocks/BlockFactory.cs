using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Larnix.Core.Vectors;
using Larnix.Core;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public static class BlockFactory
    {
        private static Dictionary<BlockID, BlockInfo> BlockCache = new();
        private static object _locker = new();

        private static readonly string Namespace = typeof(BlockServer).Namespace;
        private static readonly string AsmName = typeof(BlockServer).Assembly.GetName().Name;

        private class BlockInfo
        {
            public string Name;
            public Type Type;
            public Func<Vec2Int, BlockData1, bool, BlockServer> Constructor;
            public List<Action<BlockServer>> Inits = new();

            public BlockServer SlaveInstance { get; init; }

            public BlockInfo(BlockID ID)
            {
                Name = ID.ToString();
                Type = Type.GetType(Namespace + "." + Name + ", " + AsmName);

                var ctorInfo = Type?.GetConstructor(new Type[] { typeof(Vec2Int), typeof(BlockData1), typeof(bool) });
                var ctor = Type != null ? RuntimeCompilation.CompileConstructor(ctorInfo) : null;
                if (ctor != null && typeof(BlockServer).IsAssignableFrom(Type))
                {
                    Constructor = (a, b, c) => (BlockServer)ctor(new object[] { a, b, c });
                    SlaveInstance = (BlockServer)FormatterServices.GetUninitializedObject(Type);

                    var ifaces = Type?.GetInterfaces() ?? Array.Empty<Type>();
                    foreach (var iface in ifaces)
                    {
                        if (iface.Namespace == null || !iface.Namespace.StartsWith(Namespace))
                            continue;

                        MethodInfo minfo = iface.GetMethod("Init", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (minfo != null && minfo.GetParameters().Length == 0)
                        {
                            var mcompiled = RuntimeCompilation.CompileMethod(minfo);
                            Inits.Add(b => mcompiled(b, Array.Empty<object>()));
                        }
                    }
                }
                else
                {
                    Core.Debug.LogWarning($"Class {Namespace}.{Name} must exist and have a constructor " +
                        $"with arguments: (Vec2Int, BlockData1, bool). Loading base class instead...");

                    Constructor = (a, b, c) => new BlockServer(a, b, c);
                    SlaveInstance = (BlockServer)FormatterServices.GetUninitializedObject(typeof(BlockServer));
                }
            }
        }

        private static BlockInfo GetBlockInfo(BlockID ID)
        {
            lock (_locker)
            {
                if (BlockCache.ContainsKey(ID))
                {
                    return BlockCache[ID];
                }
                else
                {
                    BlockInfo info = new BlockInfo(ID);
                    BlockCache[ID] = info;
                    return info;
                }
            }
        }

        public static BlockServer ConstructBlockObject(Vec2Int POS, BlockData1 block, bool isFront, IWorldAPI worldAPI)
        {
            BlockInfo info = GetBlockInfo(block.ID);
            BlockServer blockserv = info.Constructor(POS, block, isFront);
            blockserv.InitializeWorldAPI(worldAPI);
            foreach (var init in info.Inits)
            {
                init(blockserv);
            }
            return blockserv;
        }

        public static bool HasInterface<TIface>(BlockID ID) where TIface : class, IBlockInterface
        {
            return GetSlaveInstance<TIface>(ID) != null;
        }

        public static TIface GetSlaveInstance<TIface>(BlockID ID) where TIface : class, IBlockInterface
        {
            BlockInfo info = GetBlockInfo(ID);
            return info.SlaveInstance as TIface;
        }
    }
}
