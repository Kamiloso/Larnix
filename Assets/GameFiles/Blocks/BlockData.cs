using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larnix.Blocks
{
    public class BlockData
    {
        public SingleBlockData Front = new();
        public SingleBlockData Back = new();

        public BlockData() { }
        public BlockData(SingleBlockData front, SingleBlockData back)
        {
            Front = front;
            Back = back;
        }

        public BlockData ShallowCopy()
        {
            return new BlockData
            (
                new SingleBlockData
                {
                    ID = Front.ID,
                    Variant = Front.Variant,
                    NBT = Front.NBT
                },
                new SingleBlockData
                {
                    ID = Back.ID,
                    Variant = Back.Variant,
                    NBT = Back.NBT
                }
            );
        }

        public byte[] SerializeBaseData()
        {
            byte[] bytes1 = BitConverter.GetBytes((ushort)Front.ID);
            byte[] bytes2 = BitConverter.GetBytes((ushort)Back.ID);
            byte[] bytes3 = { (byte)(16 * Front.Variant + Back.Variant) };

            return bytes1.Concat(bytes2).Concat(bytes3).ToArray();
        }

        public void DeserializeBaseData(byte[] bytes)
        {
            Front.ID = (BlockID)BitConverter.ToInt16(bytes[0..2]);
            Back.ID = (BlockID)BitConverter.ToInt16(bytes[2..4]);

            byte variants = bytes[4];
            Front.Variant = (byte)(variants / 16);
            Back.Variant = (byte)(variants % 16);
        }
    }
}
