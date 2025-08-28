using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;
using Larnix.Socket.Channel;

namespace Larnix.Socket.Commands
{
    public class PlayerUpdate : BaseCommand
    {
        public const int SIZE = 3 * sizeof(float) + 1 * sizeof(uint);

        public Vector2 Position { get; private set; } // 4B + 4B
        public float Rotation { get; private set; } // 4B
        public uint FixedFrame { get; private set; } // 4B

        public PlayerUpdate(Vector2 position, float rotation, uint fixedFrame, byte code = 0)
            : base(code)
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
                EndianUnsafe.FromBytes<float>(bytes, 0),
                EndianUnsafe.FromBytes<float>(bytes, 4)
                );
            Rotation = EndianUnsafe.FromBytes<float>(bytes, 8);
            FixedFrame = EndianUnsafe.FromBytes<uint>(bytes, 12);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(Position.x),
                EndianUnsafe.GetBytes(Position.y),
                EndianUnsafe.GetBytes(Rotation),
                EndianUnsafe.GetBytes(FixedFrame)
            );

            return new Packet(ID, Code, bytes);
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
