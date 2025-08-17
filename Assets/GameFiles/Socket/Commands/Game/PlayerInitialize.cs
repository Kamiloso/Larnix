using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class PlayerInitialize : BaseCommand
    {
        public override Name ID => Name.PlayerInitialize;
        public const int SIZE = 2 * sizeof(float) + 1 * sizeof(ulong) + 1 * sizeof(uint);

        public Vector2 Position { get; private set; } // 4B + 4B
        public ulong MyUid { get; private set; } // 8B
        public uint LastFixedFrame { get; private set; } // 4B

        public PlayerInitialize(Vector2 position, ulong myUid, uint lastFixedFrame, byte code = 0)
            : base(Name.None, code)
        {
            Position = position;
            MyUid = myUid;
            LastFixedFrame = lastFixedFrame;

            DetectDataProblems();
        }

        public PlayerInitialize(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            Position = new Vector2(
                EndianUnsafe.FromBytes<float>(bytes, 0),
                EndianUnsafe.FromBytes<float>(bytes, 4)
                );
            MyUid = EndianUnsafe.FromBytes<ulong>(bytes, 8);
            LastFixedFrame = EndianUnsafe.FromBytes<uint>(bytes, 16);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(Position.x),
                EndianUnsafe.GetBytes(Position.y),
                EndianUnsafe.GetBytes(MyUid),
                EndianUnsafe.GetBytes(LastFixedFrame)
            );

            return new Packet((byte)ID, Code, bytes);
        }

        protected override void DetectDataProblems()
        {
            bool ok = (
                true
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
