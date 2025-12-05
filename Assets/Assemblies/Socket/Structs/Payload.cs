using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Larnix.Socket.Packets;
using System.Linq;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Channel
{
    public enum CmdID : ushort { None = 0 }
}

namespace Larnix.Socket.Structs
{
    public abstract class Payload
    {
        private Packet Packet = null;
        protected virtual bool WarningSuppress => false;

        public CmdID ID => Packet.ID;
        public byte Code => Packet.Code;
        protected byte[] Bytes => Packet.Bytes;

        public static bool TryConstructPayload<T>(Packet packet, out T typed) where T : Payload, new()
        {
            if (packet?.ID == CmdID<T>())
            {
                T payload = new T();
                payload.Packet = packet;

                if (payload.IsValid())
                {
                    typed = payload;
                    return true;
                }
            }
            typed = null;
            return false;
        }

        protected void InitializePayload(byte[] bytes, byte code = 0)
        {
            if (Packet != null)
                throw new InvalidOperationException("Payload can be initialized only once!");

            Packet = new Packet(GetMyCmdID(), code, bytes);

            if (!IsValid())
                throw new FormatException("Payload data is not valid! Couldn't construct " + GetType() + " message.");

            int lngt = bytes.Length;

            if (lngt > 65_536 - 100)
                throw new FormatException($"Message with bytes.Length = {lngt} cannot be sent. The size limit for payload is 65_436.");

            if (!WarningSuppress && lngt > 1500 - 100)
                Core.Debug.LogWarning($"Constructed {GetType()} message may need fragmentation with bytes.Length = {lngt}. " +
                    $"It is recommended to not create payload larger than 1400 bytes to prevent unnecessary packet loss. " +
                    $"You can suppress this warning by overriding property \"WarningSuppress\" to \"true\" in a message class.");
        }

        protected abstract bool IsValid();


        // ===== OPERATORS =====

        public static implicit operator Packet(Payload payload)
        {
            return payload.Packet ?? throw new InvalidOperationException("Cannot convert Payload to Packet, because Payload was never initialized properly. " +
                $"Are you using a default constructor on a {payload.GetType()} object? Only parametric constructors are allowed.");
        }


        // ===== REFLECTIVE SEGMENT =====

        private static readonly Dictionary<Type, CmdID> _dictCmdIDs = new();
        private static bool _staticInitialized = false;
        private static object _locker = new object();

        private static readonly List<Type> _hardCodedCmdIDs = new()
        {
            typeof(None),               // 0
            typeof(AllowConnection),    // 1
            typeof(Stop),               // 2
            typeof(DebugMessage),       // 3
            typeof(P_ServerInfo),       // 4
            typeof(A_ServerInfo),       // 5
            typeof(P_LoginTry),         // 6
            typeof(A_LoginTry),         // 7
            //...
        };

        private static void _DictInitialize()
        {
            // Look for types

            List<Type> allTypes = new();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                allTypes.AddRange(types);
            }

            // Filter and sort

            allTypes = allTypes.Where(t => IsDerived(t, typeof(Payload))).ToList();
            allTypes.Sort((typeA, typeB) =>
            {
                string assemblyA = typeA.Assembly.GetName().Name;
                string assemblyB = typeB.Assembly.GetName().Name;

                string thisAssembly = typeof(Payload).Assembly.GetName().Name;
                if (assemblyA == thisAssembly && assemblyB != thisAssembly) return -1;
                if (assemblyA != thisAssembly && assemblyB == thisAssembly) return 1;

                int cmp = string.Compare(assemblyA, assemblyB, StringComparison.Ordinal);
                if (cmp != 0) return cmp;

                return string.Compare(typeA.Name, typeB.Name, StringComparison.Ordinal);
            });

            // Fill dict

            allTypes.RemoveAll(t => _hardCodedCmdIDs.Contains(t));
            allTypes = _hardCodedCmdIDs.Concat(allTypes).ToList();

            ushort cmdID = 1;
            foreach (Type type in allTypes)
            {
                if (cmdID == 0)
                    throw new OverflowException("Too many classes deriving from QuickNet.Payload! " +
                        "How is this possible that you caused a ushort overflow?!");

                // Checking because it may already have default values set
                if (!_dictCmdIDs.ContainsKey(type))
                {
                    _dictCmdIDs.Add(type, (CmdID)cmdID);
                    cmdID++;
                }
            }
        }

        private static bool IsDerived(Type type, Type baseType)
        {
            return type.IsClass &&
                   !type.IsAbstract &&
                   type != baseType &&
                   baseType.IsAssignableFrom(type);
        }

        private static CmdID CmdIDByType(Type type)
        {
            lock (_locker)
            {
                if (!_staticInitialized)
                {
                    _DictInitialize();
                    _staticInitialized = true;
                }

                return _dictCmdIDs[type];
            }
        }

        public static CmdID CmdID<T>() where T : Payload, new()
        {
            return CmdIDByType(typeof(T));
        }

        private CmdID GetMyCmdID()
        {
            return CmdIDByType(GetType());
        }
    }
}