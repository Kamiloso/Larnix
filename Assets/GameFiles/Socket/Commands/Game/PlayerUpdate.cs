using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Unity.Burst.Intrinsics;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class PlayerUpdate : BaseCommand
    {
        public override Name ID => Name.PlayerUpdate;
        public const int SIZE = 3 * sizeof(float) + 1 * sizeof(uint);

        public Vector2 Position { get; private set; } // 4B + 4B
        public float Rotation { get; private set; } // 4B
        public uint FixedFrame { get; private set; } // 4B

        public PlayerUpdate(Vector2 position, float rotation, uint fixedFrame, byte code = 0)
            : base(Name.None, code)
        {
            Position = position;
            Rotation = rotation;
            FixedFrame = fixedFrame;

            DetectDataProblems();
        }

        public PlayerUpdate(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            Position = new Vector2(
                BitConverter.ToSingle(bytes, 0),
                BitConverter.ToSingle(bytes, 4)
                );
            Rotation = BitConverter.ToSingle(bytes, 8);
            FixedFrame = BitConverter.ToUInt32(bytes, 12);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes1 = BitConverter.GetBytes(Position.x);
            byte[] bytes2 = BitConverter.GetBytes(Position.y);
            byte[] bytes3 = BitConverter.GetBytes(Rotation);
            byte[] bytes4 = BitConverter.GetBytes(FixedFrame);

            byte[] bytes = bytes1.Concat(bytes2).Concat(bytes3).Concat(bytes4).ToArray();

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
