using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;

namespace Larnix.Packets
{
    public sealed class NearbyEntities : Payload
    {
        private const int HEADER_SIZE = sizeof(uint) + sizeof(ushort) + sizeof(ushort);
        private const int ENTRY_SIZE = sizeof(ulong);
        private const int MAX_RECORDS = (1400 - HEADER_SIZE) / ENTRY_SIZE;

        public uint FixedFrame => Primitives.FromBytes<uint>(Bytes, 0); // sizeof(uint)
        public ushort AddLength => Primitives.FromBytes<ushort>(Bytes, 4); // sizeof(ushort)
        public ushort RemoveLength => Primitives.FromBytes<ushort>(Bytes, 6); // sizeof(ushort)
        public ulong[] AddEntities => Primitives.ArrayFromBytes<ulong>(Bytes, AddLength, HEADER_SIZE); // n * ENTRY_SIZE
        public ulong[] RemoveEntities => Primitives.ArrayFromBytes<ulong>(Bytes, RemoveLength, HEADER_SIZE + AddLength * ENTRY_SIZE); // n * ENTRY_SIZE

        public NearbyEntities() { }
        private NearbyEntities(uint fixedFrame, ulong[] addEntities, ulong[] removeEntities, byte code = 0)
        {
            addEntities = addEntities ?? Array.Empty<ulong>();
            removeEntities = removeEntities ?? Array.Empty<ulong>();

            InitializePayload(ArrayUtils.MegaConcat(
                Primitives.GetBytes(fixedFrame),
                Primitives.GetBytes((ushort)addEntities.Length),
                Primitives.GetBytes((ushort)removeEntities.Length),
                Primitives.ArrayGetBytes(addEntities),
                Primitives.ArrayGetBytes(removeEntities)
                ), code);
        }

        public static NearbyEntities CreateBootstrap(uint fixedFrame)
        {
            return new NearbyEntities(fixedFrame, Array.Empty<ulong>(), Array.Empty<ulong>());
        }

        public static List<NearbyEntities> CreateList(uint fixedFrame, ulong[] addEntities, ulong[] removeEntities, byte code = 0)
        {
            addEntities = addEntities ?? Array.Empty<ulong>();
            removeEntities = removeEntities ?? Array.Empty<ulong>();

            int estimatedLength = Math.Max(addEntities.Length, removeEntities.Length) / MAX_RECORDS + 1;
            List<NearbyEntities> result = new(estimatedLength);

            int eyes = 0;
            while (eyes < addEntities.Length || eyes < removeEntities.Length)
            {
                ulong[] add = eyes < addEntities.Length ?
                    addEntities[eyes..Math.Min(addEntities.Length, eyes + MAX_RECORDS)] :
                    Array.Empty<ulong>();

                ulong[] remove = eyes < removeEntities.Length ?
                    removeEntities[eyes..Math.Min(removeEntities.Length, eyes + MAX_RECORDS)] :
                    Array.Empty<ulong>();

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
