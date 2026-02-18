using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Larnix.Core;
using Larnix.Core.Physics;
using Larnix.Entities.Structs;
using Larnix.Entities.All;
using EntityInits = Larnix.Entities.Entity.EntityInits;

namespace Larnix.Entities
{
    public static class EntityFactory
    {
        private static Dictionary<EntityID, EntityInfo> EntityCache = new();
        private static object _locker = new();

        private static readonly string Namespace = typeof(None).Namespace;
        private static readonly string AsmName = typeof(None).Assembly.GetName().Name;

        private class EntityInfo
        {
            public string Name;
            public Type Type;
            public Func<EntityInits, Entity> Constructor;
            public List<Action<Entity>> Inits = new();

            public Entity SlaveInstance { get; init; }

            public EntityInfo(EntityID ID)
            {
                Name = ID.ToString();
                Type = Type.GetType(Namespace + "." + Name + ", " + AsmName);

                var ctorInfo = Type?.GetConstructor(Array.Empty<Type>());
                var ctor = Type != null ? RuntimeCompilation.CompileConstructor(ctorInfo) : null;

                if (ctor != null && typeof(Entity).IsAssignableFrom(Type))
                {
                    Constructor = inits =>
                    {
                        var obj = (Entity)ctor(Array.Empty<object>());
                        obj.Construct(inits);
                        return obj;
                    };
                    SlaveInstance = (Entity)FormatterServices.GetUninitializedObject(Type);

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
                        var obj = new Entity();
                        obj.Construct(inits);
                        return obj;
                    };
                    SlaveInstance = (Entity)FormatterServices.GetUninitializedObject(typeof(Entity));
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

        public static Entity ConstructEntityObject(EntityInits entityInits)
        {
            EntityID id = entityInits.EntityData.ID;
            EntityInfo info = GetEntityInfo(id);
            Entity entityserv = info.Constructor(entityInits);

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
