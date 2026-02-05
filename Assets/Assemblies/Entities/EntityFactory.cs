using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Larnix.Core;
using Larnix.Core.Physics;
using Larnix.Entities.Structs;

namespace Larnix.Entities
{
    public static class EntityFactory
    {
        private static Dictionary<EntityID, EntityInfo> EntityCache = new();
        private static object _locker = new();

        private static readonly string Namespace = typeof(EntityServer).Namespace;
        private static readonly string AsmName = typeof(EntityServer).Assembly.GetName().Name;

        private class EntityInfo
        {
            public string Name;
            public Type Type;
            public Func<ulong, EntityData, EntityServer> Constructor;
            public List<Action<EntityServer>> Inits = new();

            public EntityServer SlaveInstance { get; init; }

            public EntityInfo(EntityID ID)
            {
                Name = ID.ToString();
                Type = Type.GetType(Namespace + "." + Name + ", " + AsmName);

                var ctorInfo = Type?.GetConstructor(new Type[] { typeof(ulong), typeof(EntityData) });
                var ctor = Type != null ? RuntimeCompilation.CompileConstructor(ctorInfo) : null;
                if (ctor != null && typeof(EntityServer).IsAssignableFrom(Type))
                {
                    Constructor = (a, b) => (EntityServer)ctor(new object[] { a, b });
                    SlaveInstance = (EntityServer)FormatterServices.GetUninitializedObject(Type);

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
                        $"with arguments: (ulong, EntityData). Loading base class instead...");

                    Constructor = (a, b) => new EntityServer(a, b);
                    SlaveInstance = (EntityServer)FormatterServices.GetUninitializedObject(typeof(EntityServer));
                }
            }
        }

        private static EntityInfo GetEntityInfo(EntityID ID)
        {
            lock (_locker)
            {
                if (EntityCache.ContainsKey(ID))
                {
                    return EntityCache[ID];
                }
                else
                {
                    EntityInfo info = new EntityInfo(ID);
                    EntityCache[ID] = info;
                    return info;
                }
            }
        }

        public static EntityServer ConstructEntityObject(ulong uid, EntityData entity, PhysicsManager physics)
        {
            EntityInfo info = GetEntityInfo(entity.ID);
            EntityServer entityserv = info.Constructor(uid, entity);
            entityserv.InitializePhysics(physics);
            foreach (var init in info.Inits)
            {
                init(entityserv);
            }
            return entityserv;
        }

        public static bool HasInterface<TIface>(EntityID ID) where TIface : class, IEntityInterface
        {
            return GetSlaveInstance<TIface>(ID) != null;
        }

        public static TIface GetSlaveInstance<TIface>(EntityID ID) where TIface : class, IEntityInterface
        {
            EntityInfo info = GetEntityInfo(ID);
            return info.SlaveInstance as TIface;
        }
    }
}
