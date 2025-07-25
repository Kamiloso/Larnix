using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;
using Larnix.Entities;

namespace Larnix.Socket.Commands
{
    public class EntityBroadcast : BaseCommand
    {
        public override Name ID => Name.EntityBroadcast;
        public const int BASE_SIZE = 1 * sizeof(uint) + 2 * sizeof(ushort);
        public const int ENTRY_SIZE = 1 * sizeof(ulong) + 1 * sizeof(ushort) + 3 * sizeof(float); // entity transforms entry
        public const int PLAYER_SIZE = 1 * sizeof(ulong) + 1 * sizeof(uint); // player fixed indexes entry

        public const int MAX_RECORDS = 40;

        public uint PacketFixedIndex { get; private set; } // 4B
        public Dictionary<ulong, EntityData> EntityTransforms { get; private set; } // size[2B] + ENTRIES * 22B
        public Dictionary<ulong, uint> PlayerFixedIndexes { get; private set; } // size[2B] + PLAYER_ENTRIES * 12B

        public EntityBroadcast(uint packetFixedIndex, Dictionary<ulong, EntityData> entityTransforms, Dictionary<ulong, uint> playerFixedIndexes, byte code = 0)
            : base(Name.None, code)
        {
            PacketFixedIndex = packetFixedIndex;
            EntityTransforms = entityTransforms;
            PlayerFixedIndexes = playerFixedIndexes;

            DetectDataProblems();
        }

        public EntityBroadcast(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length < BASE_SIZE) { // too short
                HasProblems = true;
                return;
            }

            PacketFixedIndex = BitConverter.ToUInt32(bytes, 0);

            ushort sizeET = BitConverter.ToUInt16(bytes, 4);
            ushort sizePF = BitConverter.ToUInt16(bytes, 6);

            int BASE1_SIZE = BASE_SIZE;
            int BASE2_SIZE = BASE_SIZE + sizeET * ENTRY_SIZE;

            if (bytes.Length != BASE_SIZE + sizeET * ENTRY_SIZE + sizePF * PLAYER_SIZE) { // wrong size
                HasProblems = true;
                return;
            }

            EntityTransforms = new Dictionary<ulong, EntityData>();
            for (int i = 0; i < sizeET; i++)
            {
                ulong uid_record = BitConverter.ToUInt64(bytes, BASE1_SIZE + i * ENTRY_SIZE);

                EntityData entityData = new EntityData();
                entityData.DeserializeTransform(bytes[(BASE1_SIZE + i * ENTRY_SIZE + 8)..(BASE1_SIZE + (i + 1) * ENTRY_SIZE)]);

                EntityTransforms.Add(uid_record, entityData);
            }

            PlayerFixedIndexes = new Dictionary<ulong, uint>();
            for (int i = 0; i < sizePF; i++)
            {
                ulong uid_record = BitConverter.ToUInt64(bytes, BASE2_SIZE + i * PLAYER_SIZE);
                uint fixed_value = BitConverter.ToUInt32(bytes, BASE2_SIZE + i * PLAYER_SIZE + 8);

                PlayerFixedIndexes.Add(uid_record, fixed_value);
            }

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = new byte[BASE_SIZE + EntityTransforms.Count * ENTRY_SIZE + PlayerFixedIndexes.Count * PLAYER_SIZE];

            Buffer.BlockCopy(BitConverter.GetBytes(PacketFixedIndex), 0, bytes, 0, 4);

            Buffer.BlockCopy(BitConverter.GetBytes((ushort)EntityTransforms.Count), 0, bytes, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)PlayerFixedIndexes.Count), 0, bytes, 6, 2);

            int POS = BASE_SIZE;
            
            foreach(var kvp in EntityTransforms)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(kvp.Key), 0, bytes, POS, 8);
                POS += 8;

                Buffer.BlockCopy(kvp.Value.SerializeTransform(), 0, bytes, POS, ENTRY_SIZE - 8);
                POS += ENTRY_SIZE - 8;
            }

            foreach (var kvp in PlayerFixedIndexes)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(kvp.Key), 0, bytes, POS, 8);
                POS += 8;

                Buffer.BlockCopy(BitConverter.GetBytes(kvp.Value), 0, bytes, POS, 4);
                POS += 4;
            }

            return new Packet((byte)ID, Code, bytes);
        }

        protected override void DetectDataProblems()
        {
            bool ok = (
                EntityTransforms != null && EntityTransforms.Count <= MAX_RECORDS &&
                PlayerFixedIndexes != null && PlayerFixedIndexes.Count <= MAX_RECORDS
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
