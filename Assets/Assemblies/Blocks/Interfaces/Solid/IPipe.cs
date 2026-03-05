using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Core;
using System.Linq;
using System;
using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public interface IPipe : IHasCollider, IPlaceable, IBreakable, IFragile
    {
        void Init()
        {
            This.Subscribe(BlockOrder.PreFrameSelfMutations,
                 () => MutateNearbyPipes());
        }

        Collider[] IHasCollider.STATIC_GetAllColliders(BlockID ID, byte variant)
        {
            double W = WIDTH();
            List<Collider> colliders = new()
            {
                new Collider(
                    Offset: new Vec2(0, 0),
                    Size: new Vec2(W, W)
                    )
            };

            bool up = (variant & (1 << 0)) != 0;
            if (up) colliders.Add(new Collider(
                Offset: new Vec2(0, 0.25),
                Size: new Vec2(W, 0.5)
                )
            );

            bool right = (variant & (1 << 1)) != 0;
            if (right) colliders.Add(new Collider(
                Offset: new Vec2(0.25, 0),
                Size: new Vec2(0.5, W)
                )
            );

            bool down = (variant & (1 << 2)) != 0;
            if (down) colliders.Add(new Collider(
                Offset: new Vec2(0, -0.25),
                Size: new Vec2(W, 0.5)
                )
            );
            
            bool left = (variant & (1 << 3)) != 0;
            if (left) colliders.Add(new Collider(
                Offset: new Vec2(-0.25, 0),
                Size: new Vec2(0.5, W)
                )
            );

            return colliders.ToArray();
        }

        double WIDTH();
        Type[] CONNECT_TO_TYPES();

        private void MutateNearbyPipes()
        {
            byte nearby = (byte)(
                PartialConnectionBit(Vec2Int.Up, 0) |
                PartialConnectionBit(Vec2Int.Right, 1) |
                PartialConnectionBit(Vec2Int.Down, 2) |
                PartialConnectionBit(Vec2Int.Left, 3)
            );

            if (This.BlockData.Variant != nearby)
            {
                SelfChangeVariant(nearby);
            }
        }

        private int PartialConnectionBit(Vec2Int direction, int offset)
        {
            Vec2Int POS = This.Position + direction;
            byte variant = This.BlockData.Variant;

            bool? connectable = ConnectableTo(POS);
            return connectable switch
            {
                true => 1 << offset,
                false => 0,
                null => variant & (1 << offset)
            };
        }

        private bool? ConnectableTo(Vec2Int POS_other)
        {
            Block block = WorldAPI.GetBlock(POS_other, This.IsFront);
            if (block == null) return null;

            foreach (Type type in CONNECT_TO_TYPES())
            {
                if (type.IsAssignableFrom(block.GetType()))
                    return true;
            }
            return false;
        }
    }
}
