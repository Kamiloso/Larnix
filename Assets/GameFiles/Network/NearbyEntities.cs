using System.Collections;
using System.Collections.Generic;
using System;
using QuickNet.Channel;
using QuickNet;
using QuickNet.Commands;

namespace Larnix.Network
{
    public class NearbyEntities : BaseCommand
    {
        public const int BASE_SIZE = 1 * sizeof(uint) + 2 * sizeof(ushort);
        public const int ENTRY_SIZE = 1 * sizeof(ulong);

        public const int MAX_RECORDS = 85;

        public uint FixedFrame { get; private set; } // 4B
        public List<ulong> AddEntities { get; private set; } // size[2B] + ENTRIES * 8B
        public List<ulong> RemoveEntities { get; private set; } // size[2B] + PLAYER_ENTRIES * 8B

        public NearbyEntities(uint fixedFrame, List<ulong> addEntities, List<ulong> removeEntities, byte code = 0)
            : base(code)
        {
            FixedFrame = fixedFrame;
            AddEntities = addEntities;
            RemoveEntities = removeEntities;

            DetectDataProblems();
        }

        public NearbyEntities(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length < BASE_SIZE) { // too short
                HasProblems = true;
                return;
            }

            FixedFrame = EndianUnsafe.FromBytes<uint>(bytes, 0);

            ushort sizeAE = EndianUnsafe.FromBytes<ushort>(bytes, 4);
            ushort sizeRE = EndianUnsafe.FromBytes<ushort>(bytes, 6);

            int BASE1_SIZE = BASE_SIZE;
            int BASE2_SIZE = BASE_SIZE + sizeAE * ENTRY_SIZE;

            if (bytes.Length != BASE_SIZE + (sizeAE + sizeRE) * ENTRY_SIZE) { // wrong size
                HasProblems = true;
                return;
            }

            AddEntities = new List<ulong>();
            for (int i = 0; i < sizeAE; i++)
            {
                ulong uid = EndianUnsafe.FromBytes<ulong>(bytes, BASE1_SIZE + i * ENTRY_SIZE);
                AddEntities.Add(uid);
            }

            RemoveEntities = new List<ulong>();
            for (int i = 0; i < sizeRE; i++)
            {
                ulong uid = EndianUnsafe.FromBytes<ulong>(bytes, BASE2_SIZE + i * ENTRY_SIZE);
                RemoveEntities.Add(uid);
            }

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = new byte[BASE_SIZE + (AddEntities.Count + RemoveEntities.Count) * ENTRY_SIZE];

            Buffer.BlockCopy(EndianUnsafe.GetBytes(FixedFrame), 0, bytes, 0, 4);
            Buffer.BlockCopy(EndianUnsafe.GetBytes((ushort)AddEntities.Count), 0, bytes, 4, 2);
            Buffer.BlockCopy(EndianUnsafe.GetBytes((ushort)RemoveEntities.Count), 0, bytes, 6, 2);

            int POS = BASE_SIZE;
            
            foreach(ulong uid in AddEntities)
            {
                Buffer.BlockCopy(EndianUnsafe.GetBytes(uid), 0, bytes, POS, 8);
                POS += 8;
            }

            foreach (ulong uid in RemoveEntities)
            {
                Buffer.BlockCopy(EndianUnsafe.GetBytes(uid), 0, bytes, POS, 8);
                POS += 8;
            }

            return new Packet(ID, Code, bytes);
        }

        protected override void DetectDataProblems()
        {
            bool ok = (
                AddEntities != null && AddEntities.Count <= MAX_RECORDS &&
                RemoveEntities != null && RemoveEntities.Count <= MAX_RECORDS
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
