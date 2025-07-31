using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Larnix.Server.Terrain;
using Larnix.Server;

namespace Larnix.Blocks
{
    public static class BlockFactory
    {
        private static HashSet<string> WarningsDone = new(); // May be a micro memory leak, but it prevents spam in crisis situations.

        private static Dictionary<string, Type> StringToType = new();
        private static Dictionary<Type, BlockServer> SlaveInstances = new();
        private const int MAX_CACHE = 256;

        private static Type GetTypeByName(string name)
        {
            if (StringToType.ContainsKey(name))
                return StringToType[name];
            else
            {
                if (StringToType.Count >= MAX_CACHE)
                    StringToType.Clear();

                Type type = Type.GetType(name + ", Assembly-CSharp");
                StringToType[name] = type;
                return type;
            }
        }

        public static BlockServer ConstructBlockObject(Vector2Int POS, SingleBlockData block, bool isFront)
        {
            string blockName = block.ID.ToString();
            string className = $"Larnix.Modules.Blocks.{blockName}";

            Type type = GetTypeByName(className);
            if (
                type == null ||
                !typeof(BlockServer).IsAssignableFrom(type) ||
                type.GetConstructor(new Type[] { typeof(Vector2Int), typeof(SingleBlockData), typeof(bool) }) == null
                )
            {
                type = typeof(BlockServer);

                string warning = $"Class {className} cannot be loaded! Loading base class instead...";
                if (!WarningsDone.Contains(warning))
                {
                    if (References.Server.IsLocal) UnityEngine.Debug.LogWarning(warning);
                    else Server.Console.LogWarning(warning);
                    WarningsDone.Add(warning);
                }
            }

            BlockServer blockserv = (BlockServer)Activator.CreateInstance(type, POS, block, isFront);
            InvokeInitOnInterfaces(blockserv);
            return blockserv;
        }

        public static TIface GetSlaveInstance<TIface>(BlockID blockID) where TIface : class
        {
            if (!HasInterface<TIface>(blockID))
                return null;

            Type type = GetBlockTypeByID(blockID);
            if (type == null)
                return null;

            object instance;
            if (SlaveInstances.ContainsKey(type))
            {
                instance = SlaveInstances[type];
            }
            else
            {
                if (SlaveInstances.Count >= MAX_CACHE)
                    SlaveInstances.Clear(); // kill or free (depends who you ask)

                // Block types always have constructor (Vector2Int, SingleBlockData, bool)
                // It is guaranteed by this condidtion: HasInterface<TIface>(blockID) == true

                instance = Activator.CreateInstance(type, new Vector2Int(0, 0), null, false);
                SlaveInstances[type] = (BlockServer)instance;
            }
            return instance as TIface;
        }

        public static bool HasInterface<TIface>(BlockID blockID)
        {
            Type type = GetBlockTypeByID(blockID);
            return type != null && typeof(TIface).IsAssignableFrom(type);
        }

        public static Type GetBlockTypeByID(BlockID blockID)
        {
            string blockname = blockID.ToString();
            return GetTypeByName($"Larnix.Modules.Blocks.{blockname}");
        }

        private static void InvokeInitOnInterfaces(BlockServer block)
        {
            Type type = block.GetType();

            var declaredInterfaces = type.GetInterfaces();

            foreach (var iface in declaredInterfaces)
            {
                if (iface.Namespace == null || !iface.Namespace.StartsWith("Larnix.Modules.Blocks"))
                    continue;

                MethodInfo initMethod = iface.GetMethod("Init", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (initMethod != null && initMethod.GetParameters().Length == 0)
                {
                    initMethod.Invoke(block, null);
                }
            }
        }
    }
}
