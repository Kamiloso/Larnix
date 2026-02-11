using Larnix.Core;
using System.Collections.Generic;
using System;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public interface IElectricPipe : IPipe, IElectricPropagator
    {
        private static Vec2Int[] CARDINAL_DIRECTIONS = new[] {
            Vec2Int.Up, Vec2Int.Right, Vec2Int.Down, Vec2Int.Left
            };

        new void Init()
        {
            This.FrameEventElectricFinalize += (sender, args) => RethinkLitState();
        }

        BlockID ID_UNLIT();
        BlockID ID_LIT();
        string ELECTRIC_PIPE_ID();
        bool IS_LIT() => This.BlockData.ID == ID_LIT();

        double IPipe.WIDTH() => 6.0 / 16.0;
        IEnumerable<Type> IPipe.CONNECT_TO_TYPES() => new Type[] {
            typeof(IElectricPipe),
            typeof(IElectricSource),
            typeof(IElectricDevice),
            };

        bool IPlaceable.ALLOW_PLACE_BACK() => true;

        ITool.Type IBreakable.MATERIAL_TYPE() => ITool.Type.Normal;
        ITool.Tier IBreakable.MATERIAL_TIER() => ITool.Tier.Copper;

        bool IBreakable.STATIC_IsBreakableItemMatch(BlockData1 block, BlockData1 item)
        {
            string id1 = BlockFactory.GetSlaveInstance<IElectricPipe>(block.ID)?.ELECTRIC_PIPE_ID();
            string id2 = BlockFactory.GetSlaveInstance<IElectricPipe>(item.ID)?.ELECTRIC_PIPE_ID();

            return id1 != null && id2 != null && id1 == id2;
        }

        void RethinkLitState()
        {
            bool shouldBeLit = Data["electric_propagator.recursion"].Int > 0;
            if (shouldBeLit != IS_LIT())
            {
                BlockData1 blockTemplate = new(
                    id: shouldBeLit ? ID_LIT() : ID_UNLIT(),
                    variant: This.BlockData.Variant,
                    data: This.BlockData.Data
                );
                WorldAPI.ReplaceBlock(This.Position, This.IsFront, blockTemplate, IWorldAPI.BreakMode.Weak);
            }
        }

        void IElectricPropagator.ElectricPropagate(Vec2Int POS_src, int recursion)
        {
            Data["electric_propagator.recursion"].Int = recursion;

            Vec2Int POS = This.Position;
            int nextRecursion = recursion - 1;

            foreach (Vec2Int dir in CARDINAL_DIRECTIONS)
            {
                Vec2Int POS_other = POS + dir;
                if (POS_other == POS_src) continue;

                bool? canPropagate = CanPropagateInto(POS_other, out int pipeRecursion);
                if (canPropagate == false) continue;

                if (canPropagate == true)
                {
                    if (nextRecursion > pipeRecursion)
                    {
                        IElectricPropagator pipe = (IElectricPropagator)WorldAPI.GetBlock(POS + dir, This.IsFront);
                        pipe.ElectricPropagate(POS, nextRecursion);
                    }
                }

                if (canPropagate == null)
                {
                    // TODO: fix later, should load chunks or something
                    Core.Debug.LogWarning("Electric propagation reached unloaded block!");
                    //ElectricPropagate(recursion);
                }
            }
        }

        private bool? CanPropagateInto(Vec2Int POS_other, out int recursion)
        {
            recursion = default;

            BlockServer block = WorldAPI.GetBlock(POS_other, This.IsFront);
            if (block == null) return null;

            if (block is IElectricPipe pipe)
            {
                if (ELECTRIC_PIPE_ID() != pipe.ELECTRIC_PIPE_ID())
                    return false;
                
                recursion = pipe.Data["electric_propagator.recursion"].Int;
                return true;
            }
            if (block is IElectricDevice device)
            {
                recursion = device.Data["electric_propagator.recursion"].Int;
                return true;
            }
            return false;
        }
    }
}
