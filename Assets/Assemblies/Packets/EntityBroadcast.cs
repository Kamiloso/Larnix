using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Core.Utils;
using Larnix.Entities.Structs;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;

namespace Larnix.Packets
{
    public sealed class EntityBroadcast : Payload
    {
        private const int HEADER_SIZE = sizeof(uint) + sizeof(ushort) + sizeof(ushort);
        private const int ENTRY_A_SIZE = sizeof(ulong) + EntityDataCompressed.SIZE; // entity transforms entry
        private const int ENTRY_B_SIZE = sizeof(ulong) + sizeof(uint); // player fixed indexes entry
        private const int MAX_RECORDS = (1400 - HEADER_SIZE) / (ENTRY_A_SIZE + ENTRY_B_SIZE); // max records that can fit in one packet

        public uint PacketFixedIndex => Primitives.FromBytes<uint>(Bytes, 0); // 4B
        public ushort EntityLength => Primitives.FromBytes<ushort>(Bytes, 4); // 2B
        public ushort PlayerFixedLength => Primitives.FromBytes<ushort>(Bytes, 6); // 2B
        public Dictionary<ulong, EntityData> EntityTransforms => GetDictionaryA(Bytes, EntityLength, HEADER_SIZE); // n * ENTRY_A_SIZE
        public Dictionary<ulong, uint> PlayerFixedIndexes => GetDictionaryB(Bytes, PlayerFixedLength, HEADER_SIZE + EntityLength * ENTRY_A_SIZE); // n * ENTRY_B_SIZE

        public EntityBroadcast() { }

        private EntityBroadcast(uint packetFixedIndex, Dictionary<ulong, EntityData> entityTransforms, Dictionary<ulong, uint> playerFixedIndexes, byte code = 0)
        {
            if (entityTransforms == null) entityTransforms = new();
            if (playerFixedIndexes == null) playerFixedIndexes = new();

            InitializePayload(ArrayUtils.MegaConcat(
                Primitives.GetBytes(packetFixedIndex),
                Primitives.GetBytes((ushort)entityTransforms.Count),
                Primitives.GetBytes((ushort)playerFixedIndexes.Count),
                SerializeDictionaryA(entityTransforms),
                SerializeDictionaryB(playerFixedIndexes)
                ), code);
        }

        public static List<EntityBroadcast> CreateList(uint packetFixedIndex, Dictionary<ulong, EntityData> entityTransforms, Dictionary<ulong, uint> playerFixedIndexes, byte code = 0)
        {
            if (entityTransforms == null) entityTransforms = new();
            if (playerFixedIndexes == null) playerFixedIndexes = new();

            List<EntityBroadcast> result = new();
            List<ulong> sendUIDs = entityTransforms.Keys.ToList();
            for (int pos = 0; pos < sendUIDs.Count; pos += MAX_RECORDS)
            {
                int sizeEnt = Math.Clamp(sendUIDs.Count - pos, 0, MAX_RECORDS);
                if (sizeEnt == 0) break;
                var fragmentedUIDs = sendUIDs.GetRange(pos, sizeEnt).ToHashSet();

                Dictionary<ulong, EntityData> fragmentEntities = new();
                foreach (ulong uid in fragmentedUIDs)
                {
                    if (entityTransforms.TryGetValue(uid, out EntityData data))
                        fragmentEntities[uid] = data;
                }

                Dictionary<ulong, uint> fragmentFixed = new();
                foreach (ulong uid in fragmentedUIDs)
                {
                    if (playerFixedIndexes.TryGetValue(uid, out uint fixedIndex))
                        fragmentFixed[uid] = fixedIndex;
                }

                result.Add(new EntityBroadcast(packetFixedIndex, fragmentEntities, fragmentFixed, code));
            }

            return result;
        }

        private static Dictionary<ulong, EntityData> GetDictionaryA(byte[] bytes, int count, int offset = 0)
        {
            Dictionary<ulong, EntityData> result = new();
            for (int i = 0; i < count; i++)
            {
                ulong key = Primitives.FromBytes<ulong>(bytes, i * ENTRY_A_SIZE + 0 + offset);
                EntityData value = Structures.FromBytes<EntityDataCompressed>(bytes, i * ENTRY_A_SIZE + sizeof(ulong) + offset).Contents;
                result[key] = value;
            }
            return result;
        }

        private static Dictionary<ulong, uint> GetDictionaryB(byte[] bytes, int count, int offset = 0)
        {
            Dictionary<ulong, uint> result = new();
            for (int i = 0; i < count; i++)
            {
                ulong key = Primitives.FromBytes<ulong>(bytes, i * ENTRY_B_SIZE + 0 + offset);
                uint value = Primitives.FromBytes<uint>(bytes, i * ENTRY_B_SIZE + sizeof(ulong) + offset);
                result[key] = value;
            }
            return result;
        }

        private static byte[] SerializeDictionaryA(Dictionary<ulong, EntityData> dictA)
        {
            byte[] buffer = new byte[dictA.Count * ENTRY_A_SIZE];
            int i = 0;
            foreach (var vkp in dictA)
            {
                byte[] keyBytes = Primitives.GetBytes(vkp.Key);
                byte[] valueBytes = Structures.GetBytes(new EntityDataCompressed(vkp.Value));

                Buffer.BlockCopy(keyBytes, 0, buffer, 0 + i * ENTRY_A_SIZE, sizeof(ulong));
                Buffer.BlockCopy(valueBytes, 0, buffer, sizeof(ulong) + i * ENTRY_A_SIZE, EntityDataCompressed.SIZE);

                i++;
            }
            return buffer;
        }

        private static byte[] SerializeDictionaryB(Dictionary<ulong, uint> dictB)
        {
            byte[] buffer = new byte[dictB.Count * ENTRY_B_SIZE];
            int i = 0;
            foreach (var vkp in dictB)
            {
                byte[] keyBytes = Primitives.GetBytes(vkp.Key);
                byte[] valueBytes = Primitives.GetBytes(vkp.Value);

                Buffer.BlockCopy(keyBytes, 0, buffer, 0 + i * ENTRY_B_SIZE, sizeof(ulong));
                Buffer.BlockCopy(valueBytes, 0, buffer, sizeof(ulong) + i * ENTRY_B_SIZE, sizeof(uint));

                i++;
            }
            return buffer;
        }

        protected override bool IsValid()
        {
            return Bytes.Length >= HEADER_SIZE &&
                   Bytes.Length == HEADER_SIZE + EntityLength * ENTRY_A_SIZE + PlayerFixedLength * ENTRY_B_SIZE &&
                   EntityLength <= MAX_RECORDS &&
                   PlayerFixedLength <= MAX_RECORDS;
        }
    }
}
