using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Larnix.Core;
using Larnix.Blocks.All;
using BlockInits = Larnix.Blocks.Block.BlockInits;

namespace Larnix.Blocks
{
    public static class BlockFactory
    {
        private static Dictionary<BlockID, BlockInfo> BlockCache = new();
        private static object _locker = new();

        private static readonly string Namespace = typeof(Air).Namespace;
        private static readonly string AsmName = typeof(Air).Assembly.GetName().Name;

        private class BlockInfo
        {
            public string Name;
            public Type Type;
            public Func<BlockInits, Block> Constructor;
            public List<Action<Block>> Inits = new();

            public Block SlaveInstance { get; init; }

            public BlockInfo(BlockID ID)
            {
                Name = ID.ToString();
                Type = Type.GetType(Namespace + "." + Name + ", " + AsmName);

                var ctorInfo = Type?.GetConstructor(Array.Empty<Type>());
                var ctor = Type != null ? RuntimeCompilation.CompileConstructor(ctorInfo) : null;

                if (ctor != null && typeof(Block).IsAssignableFrom(Type))
                {
                    Constructor = inits =>
                    {
                        var obj = (Block)ctor(Array.Empty<object>());
                        obj.Construct(inits);
                        return obj;
                    };
                    SlaveInstance = (Block)FormatterServices.GetUninitializedObject(Type);

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
                    Core.Debug.LogWarning($"Class {Namespace}.{Name} must exist. Loading base class instead...");

                    Constructor = inits =>
                    {
                        var obj = new Block();
                        obj.Construct(inits);
                        return obj;
                    };
                    SlaveInstance = (Block)FormatterServices.GetUninitializedObject(typeof(Block));
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

        public static Block ConstructBlockObject(BlockInits blockInits)
        {
            BlockID id = blockInits.BlockData.ID;
            BlockInfo info = GetBlockInfo(id);
            Block blockserv = info.Constructor(blockInits);

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
