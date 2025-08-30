using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using QuickNet.Channel;

namespace QuickNet.Commands
{
    public abstract class BaseCommand
    {
        private static readonly Dictionary<Type, CmdID> DictIDs = new();
        private static readonly Dictionary<Type, Func<Packet, BaseCommand>> Constructors = new();
        private static object locker = new();

        protected BaseCommand(byte code)
        {
            Code = code;
        }

        protected BaseCommand(Packet packet)
        {
            HasProblems = HasProblems || packet.ID != ID;
            Code = packet.Code;
        }

        public abstract Packet GetPacket();
        protected abstract void DetectDataProblems();

        public bool HasProblems { get; protected set; } = false;
        public CmdID ID => GetCommandID(GetType());
        public byte Code { get; private set; } = 0;

        public static CmdID GetCommandID(Type type)
        {
            lock (locker)
            {
                if (DictIDs.TryGetValue(type, out CmdID id1))
                {
                    return id1;
                }
                else
                {
                    string name = type.Name;
                    if (Enum.TryParse(name, out CmdID id2))
                    {
                        DictIDs[type] = id2;
                        return id2;
                    }
                    else throw new Exception("All command classes should be listed in 'Commands.CmdID' enum!");
                }
            }
        }

        public static T CreateGeneric<T>(Packet packet) where T : BaseCommand
        {
            lock(locker)
            {
                var type = typeof(T);
                if (!Constructors.TryGetValue(type, out var ctor))
                {
                    ctor = BuildConstructor<T>();
                    Constructors[type] = ctor;
                }
                return (T)ctor(packet);
            }
        }

        private static Func<Packet, BaseCommand> BuildConstructor<T>() where T : BaseCommand
        {
            var ctorInfo = typeof(T).GetConstructor(new[] { typeof(Packet) });
            if (ctorInfo == null)
                throw new Exception($"Class '{typeof(T).Name}' must have a constructor with a single parameter of type Packet!");

            var param = Expression.Parameter(typeof(Packet), "packet");
            var newExpr = Expression.New(ctorInfo, param);
            var lambda = Expression.Lambda<Func<Packet, BaseCommand>>(newExpr, param);
            return lambda.Compile();
        }
    }
}
