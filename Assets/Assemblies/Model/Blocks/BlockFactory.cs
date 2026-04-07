#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Larnix.Core;
using Larnix.Model.Blocks.All;

namespace Larnix.Model.Blocks;

public static class BlockFactory
{
    private static readonly Dictionary<BlockID, BlockInfo> BlockCache = new();
    private static readonly object _locker = new();

    private static readonly string Namespace = typeof(Air).Namespace;
    private static readonly string AsmName = typeof(Air).Assembly.GetName().Name;

    private class BlockInfo
    {
        public readonly Func<BlockInits, Block> Constructor;
        public readonly List<Action<Block>> Inits = new();
        public readonly Block SlaveInstance;

        public BlockInfo(BlockID ID)
        {
            string Name = ID.ToString();
            Type? Type = Type.GetType(Namespace + "." + Name + ", " + AsmName);

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
                Echo.LogWarning($"Class {Namespace}.{Name} must exist. Loading base class instead...");

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
                BlockInfo info = new(ID);
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

    public static TIface? GetSlaveInstance<TIface>(BlockID ID) where TIface : class, IBlockInterface
    {
        BlockInfo info = GetBlockInfo(ID);
        return info.SlaveInstance as TIface;
    }
}
