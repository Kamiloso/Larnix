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
        public const int BASE_SIZE = 1 * sizeof(uint);
        public const int ENTRY_SIZE = 1 * sizeof(ulong) + 1 * sizeof(ushort) + 3 * sizeof(float);

        public uint PacketIndex { get; private set; } // 4B
        public Dictionary<ulong, EntityData> EntityTransforms { get; private set; } // ENTRIES * 22B

        public EntityBroadcast(uint packetIndex, Dictionary<ulong, EntityData> entityTransforms, byte code = 0)
            : base(Name.None, code)
        {
            PacketIndex = packetIndex;
            EntityTransforms = entityTransforms;

            DetectDataProblems();
        }

        public EntityBroadcast(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length < BASE_SIZE || (bytes.Length - BASE_SIZE) % ENTRY_SIZE != 0) {
                HasProblems = true;
                return;
            }

            PacketIndex = BitConverter.ToUInt32(bytes, 0);
            EntityTransforms = new Dictionary<ulong, EntityData>();

            int lngt = (bytes.Length - BASE_SIZE) / ENTRY_SIZE;
            for (int i = 0; i < lngt; i++)
            {
                EntityData entityData = new EntityData();
                entityData.DeserializeTransform(bytes[(BASE_SIZE + ENTRY_SIZE * i + 8)..(BASE_SIZE + ENTRY_SIZE * i + ENTRY_SIZE)]);

                EntityTransforms.Add(
                    BitConverter.ToUInt64(bytes, BASE_SIZE + ENTRY_SIZE * i),
                    entityData
                    );
            }

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = new byte[BASE_SIZE + EntityTransforms.Count * ENTRY_SIZE];

            Buffer.BlockCopy(BitConverter.GetBytes(PacketIndex), 0, bytes, 0, 4);

            int i = 0;
            foreach(var kvp in EntityTransforms)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(kvp.Key), 0, bytes, BASE_SIZE + i * ENTRY_SIZE, 8);
                Buffer.BlockCopy(kvp.Value.SerializeTransform(), 0, bytes, BASE_SIZE + i * ENTRY_SIZE + 8, ENTRY_SIZE - 8);
                i++;
            }

            return new Packet((byte)ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                EntityTransforms.Count < 2048
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
