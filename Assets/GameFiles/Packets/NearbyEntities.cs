using System;
using System.Collections;
using System.Collections.Generic;
using QuickNet;
using QuickNet.Channel;
using Unity.VisualScripting;
using UnityEngine;

namespace Larnix.Packets
{
    public class NearbyEntities : Payload
    {
        private const int HEADER_SIZE = 4 + 2 + 2;
        private const int ENTRY_SIZE = 8;
        private const int MAX_RECORDS = 85;

        public uint FixedFrame => EndianUnsafe.FromBytes<uint>(Bytes, 0); // 4B
        public ushort AddLength => EndianUnsafe.FromBytes<ushort>(Bytes, 4); // 2B
        public ushort RemoveLength => EndianUnsafe.FromBytes<ushort>(Bytes, 6); // 2B
        public ulong[] AddEntities => EndianUnsafe.ArrayFromBytes<ulong>(Bytes, AddLength, HEADER_SIZE); // n * 8B
        public ulong[] RemoveEntities => EndianUnsafe.ArrayFromBytes<ulong>(Bytes, RemoveLength, HEADER_SIZE + AddLength * ENTRY_SIZE); // n * 8B

        public NearbyEntities() { }
        public NearbyEntities(uint fixedFrame, ulong[] addEntities, ulong[] removeEntities, byte code = 0)
        {
            if (addEntities == null) addEntities = new ulong[0];
            if (removeEntities == null) removeEntities = new ulong[0];

            InitializePayload(ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(fixedFrame),
                EndianUnsafe.GetBytes((ushort)addEntities.Length),
                EndianUnsafe.GetBytes((ushort)removeEntities.Length),
                EndianUnsafe.ArrayGetBytes(addEntities),
                EndianUnsafe.ArrayGetBytes(removeEntities)
                ), code);
        }

        public static List<NearbyEntities> CreateList(uint fixedFrame, ulong[] addEntities, ulong[] removeEntities, byte code = 0)
        {
            if (addEntities == null) addEntities = new ulong[0];
            if (removeEntities == null) removeEntities = new ulong[0];

            int estimatedLength = Math.Max(addEntities.Length, removeEntities.Length) / MAX_RECORDS + 1;
            List<NearbyEntities> result = new(estimatedLength);

            int eyes = 0;
            while (eyes < addEntities.Length || eyes < removeEntities.Length)
            {
                ulong[] add = eyes < addEntities.Length ?
                    addEntities[eyes..Math.Min(addEntities.Length, eyes + MAX_RECORDS)] :
                    null;

                ulong[] remove = eyes < removeEntities.Length ?
                    removeEntities[eyes..Math.Min(removeEntities.Length, eyes + MAX_RECORDS)] :
                    null;

                result.Add(new NearbyEntities(fixedFrame, add, remove, code));
                eyes += MAX_RECORDS;
            }

            return result;
        }

        protected override bool IsValid()
        {
            return Bytes != null &&
                   Bytes.Length >= HEADER_SIZE &&
                   Bytes.Length == HEADER_SIZE + (AddLength + RemoveLength) * ENTRY_SIZE &&
                   AddLength <= MAX_RECORDS &&
                   RemoveLength <= MAX_RECORDS;
        }
    }
}
