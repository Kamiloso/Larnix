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
        public const int SIZE = 2 * sizeof(float) + 1 * sizeof(ulong);

        public Vector2 Position { get; private set; } // 4B + 4B
        public ulong MyUid { get; private set; } // 8B

        public PlayerInitialize(Vector2 position, ulong myUid, byte code = 0)
            : base(Name.None, code)
        {
            Position = position;
            MyUid = myUid;

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
                BitConverter.ToSingle(bytes, 0),
                BitConverter.ToSingle(bytes, 4)
                );
            MyUid = BitConverter.ToUInt64(bytes, 8);

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes1 = BitConverter.GetBytes(Position.x);
            byte[] bytes2 = BitConverter.GetBytes(Position.y);
            byte[] bytes3 = BitConverter.GetBytes(MyUid);

            byte[] bytes = bytes1.Concat(bytes2).Concat(bytes3).ToArray();

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
