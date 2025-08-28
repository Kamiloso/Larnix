using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;
using Larnix.Entities;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Commands
{
    public class EntityBroadcast : BaseCommand
    {
        public const int BASE_SIZE = 1 * sizeof(uint) + 2 * sizeof(ushort);
        public const int ENTRY_SIZE = 1 * sizeof(ulong) + 1 * sizeof(ushort) + 3 * sizeof(float); // entity transforms entry
        public const int PLAYER_SIZE = 1 * sizeof(ulong) + 1 * sizeof(uint); // player fixed indexes entry

        public const int MAX_RECORDS = 40;

        public uint PacketFixedIndex { get; private set; } // 4B
        public Dictionary<ulong, EntityData> EntityTransforms { get; private set; } // size[2B] + ENTRIES * 22B
        public Dictionary<ulong, uint> PlayerFixedIndexes { get; private set; } // size[2B] + PLAYER_ENTRIES * 12B

        public EntityBroadcast(uint packetFixedIndex, Dictionary<ulong, EntityData> entityTransforms, Dictionary<ulong, uint> playerFixedIndexes, byte code = 0)
            : base(code)
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

            PacketFixedIndex = EndianUnsafe.FromBytes<uint>(bytes, 0);

            ushort sizeET = EndianUnsafe.FromBytes<ushort>(bytes, 4);
            ushort sizePF = EndianUnsafe.FromBytes<ushort>(bytes, 6);

            int BASE1_SIZE = BASE_SIZE;
            int BASE2_SIZE = BASE_SIZE + sizeET * ENTRY_SIZE;

            if (bytes.Length != BASE_SIZE + sizeET * ENTRY_SIZE + sizePF * PLAYER_SIZE) { // wrong size
                HasProblems = true;
                return;
            }

            EntityTransforms = new Dictionary<ulong, EntityData>();
            for (int i = 0; i < sizeET; i++)
            {
                ulong uid_record = EndianUnsafe.FromBytes<ulong>(bytes, BASE1_SIZE + i * ENTRY_SIZE);

                EntityData entityData = new EntityData();
                entityData.DeserializeTransform(bytes[(BASE1_SIZE + i * ENTRY_SIZE + 8)..(BASE1_SIZE + (i + 1) * ENTRY_SIZE)]);

                EntityTransforms.Add(uid_record, entityData);
            }

            PlayerFixedIndexes = new Dictionary<ulong, uint>();
            for (int i = 0; i < sizePF; i++)
            {
                ulong uid_record = EndianUnsafe.FromBytes<ulong>(bytes, BASE2_SIZE + i * PLAYER_SIZE);
                uint fixed_value = EndianUnsafe.FromBytes<uint>(bytes, BASE2_SIZE + i * PLAYER_SIZE + 8);

                PlayerFixedIndexes.Add(uid_record, fixed_value);
            }

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = new byte[BASE_SIZE + EntityTransforms.Count * ENTRY_SIZE + PlayerFixedIndexes.Count * PLAYER_SIZE];

            Buffer.BlockCopy(EndianUnsafe.GetBytes(PacketFixedIndex), 0, bytes, 0, 4);

            Buffer.BlockCopy(EndianUnsafe.GetBytes((ushort)EntityTransforms.Count), 0, bytes, 4, 2);
            Buffer.BlockCopy(EndianUnsafe.GetBytes((ushort)PlayerFixedIndexes.Count), 0, bytes, 6, 2);

            int POS = BASE_SIZE;
            
            foreach(var kvp in EntityTransforms)
            {
                Buffer.BlockCopy(EndianUnsafe.GetBytes(kvp.Key), 0, bytes, POS, 8);
                POS += 8;

                Buffer.BlockCopy(kvp.Value.SerializeTransform(), 0, bytes, POS, ENTRY_SIZE - 8);
                POS += ENTRY_SIZE - 8;
            }

            foreach (var kvp in PlayerFixedIndexes)
            {
                Buffer.BlockCopy(EndianUnsafe.GetBytes(kvp.Key), 0, bytes, POS, 8);
                POS += 8;

                Buffer.BlockCopy(EndianUnsafe.GetBytes(kvp.Value), 0, bytes, POS, 4);
                POS += 4;
            }

            return new Packet(ID, Code, bytes);
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
