using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Larnix.Socket.Packets;
using System.Linq;
using Larnix.Core.Serialization;
using Larnix.Core.Utils;

namespace Larnix.Socket.Structs
{
    public enum CmdID : ushort { None = 0 }

    public abstract class Payload
    {
        internal const int BASE_HEADER_SIZE = 2 + 1 + 4;

        public CmdID ID { get; private set; } // [0..2]
        public byte Code { get; private set; } // [2..3] + SeqSecure: [3..7] (read in PayloadBox)
        protected byte[] Bytes { get; private set; } // [7..]

        protected virtual bool WarningSuppress => false;
        protected abstract bool IsValid();

        internal static bool TryConstructPayload<T>(HeaderSpan headerSpan, out T output) where T : Payload, new()
        {
            return TryConstructPayload(headerSpan.AllBytes, out output);
        }

        internal static bool TryConstructPayload<T>(byte[] rawBytes, out T output) where T : Payload, new()
        {
            if (rawBytes.Length >= BASE_HEADER_SIZE)
            {
                CmdID id = EndianUnsafe.FromBytes<CmdID>(rawBytes, 0);
                if (id == CmdID<T>())
                {
                    T payload = new T();

                    payload.ID = payload.GetMyCmdID();
                    payload.Code = EndianUnsafe.FromBytes<byte>(rawBytes, 2);
                    payload.Bytes = rawBytes[7..];

                    if (payload.IsValid())
                    {
                        output = payload;
                        return true;
                    }
                }
            }

            output = null;
            return false;
        }

        public byte[] Serialize(int seqSecure)
        {
            return ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(ID),
                EndianUnsafe.GetBytes(Code),
                EndianUnsafe.GetBytes(seqSecure), // SeqNum signature
                Bytes
                );
        }

        protected void InitializePayload(byte[] bytes, byte code = 0)
        {
            ID = GetMyCmdID();
            Code = code;
            Bytes = bytes;

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