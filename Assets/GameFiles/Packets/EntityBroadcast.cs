using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Entities;
using QuickNet;
using QuickNet.Channel;

namespace Larnix.Packets
{
    public class EntityBroadcast : Payload
    {
        private const int HEADER_SIZE = 4 + 2 + 2;
        private const int ENTRY_A_SIZE = 8 + 14; // entity transforms entry
        private const int ENTRY_B_SIZE = 8 + 4; // player fixed indexes entry
        public const int MAX_RECORDS = 40;

        public uint PacketFixedIndex => EndianUnsafe.FromBytes<uint>(Bytes, 0); // 4B
        public ushort EntityLength => EndianUnsafe.FromBytes<ushort>(Bytes, 4); // 2B
        public ushort PlayerFixedLength => EndianUnsafe.FromBytes<ushort>(Bytes, 6); // 2B
        public Dictionary<ulong, EntityData> EntityTransforms => GetDictionaryA(Bytes, EntityLength, HEADER_SIZE); // n * 22B
        public Dictionary<ulong, uint> PlayerFixedIndexes => GetDictionaryB(Bytes, PlayerFixedLength, HEADER_SIZE + EntityLength * ENTRY_A_SIZE); // n * 12B

        public EntityBroadcast() { }

        /// <summary>
        /// Warning: Fragmentation not supported! Must be done by hand.
        /// </summary>
        public EntityBroadcast(uint packetFixedIndex, Dictionary<ulong, EntityData> entityTransforms, Dictionary<ulong, uint> playerFixedIndexes, byte code = 0)
        {
            if (entityTransforms == null) entityTransforms = new();
            if (playerFixedIndexes == null) playerFixedIndexes = new();

            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(packetFixedIndex),
                EndianUnsafe.GetBytes((ushort)entityTransforms.Count),
                EndianUnsafe.GetBytes((ushort)playerFixedIndexes.Count),
                SerializeDictionaryA(entityTransforms),
                SerializeDictionaryB(playerFixedIndexes)
                ), code);
        }

        private static Dictionary<ulong, EntityData> GetDictionaryA(byte[] bytes, int count, int offset = 0)
        {
            Dictionary<ulong, EntityData> result = new();
            for (int i = 0; i < count; i++)
            {
                ulong key = EndianUnsafe.FromBytes<ulong>(bytes, i * ENTRY_A_SIZE + 0 + offset);
                EntityData value = EntityData.Deserialize(bytes, i * ENTRY_A_SIZE + 8 + offset);
                result[key] = value;
            }
            return result;
        }

        private static Dictionary<ulong, uint> GetDictionaryB(byte[] bytes, int count, int offset = 0)
        {
            Dictionary<ulong, uint> result = new();
            for (int i = 0; i < count; i++)
            {
                ulong key = EndianUnsafe.FromBytes<ulong>(bytes, i * ENTRY_B_SIZE + 0 + offset);
                uint value = EndianUnsafe.FromBytes<uint>(bytes, i * ENTRY_B_SIZE + 8 + offset);
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
                byte[] keyBytes = EndianUnsafe.GetBytes(vkp.Key);
                byte[] valueBytes = vkp.Value.Serialize();

                Buffer.BlockCopy(keyBytes, 0, buffer, 0 + i * ENTRY_A_SIZE, 8);
                Buffer.BlockCopy(valueBytes, 0, buffer, 8 + i * ENTRY_A_SIZE, 14);

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
                byte[] keyBytes = EndianUnsafe.GetBytes(vkp.Key);
                byte[] valueBytes = EndianUnsafe.GetBytes(vkp.Value);

                Buffer.BlockCopy(keyBytes, 0, buffer, 0 + i * ENTRY_B_SIZE, 8);
                Buffer.BlockCopy(valueBytes, 0, buffer, 8 + i * ENTRY_B_SIZE, 4);

                i++;
            }
            return buffer;
        }

        protected override bool IsValid()
        {
            return Bytes != null &&
                   Bytes.Length >= HEADER_SIZE &&
                   Bytes.Length == HEADER_SIZE + EntityLength * ENTRY_A_SIZE + PlayerFixedLength * ENTRY_B_SIZE &&
                   EntityLength <= MAX_RECORDS &&
                   PlayerFixedLength <= MAX_RECORDS;
        }
    }
}
