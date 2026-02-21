using Larnix.Core;
using System.Collections.Generic;
using System;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;

namespace Larnix.Blocks.All
{
    public interface IElectricPipe : IPipe, IElectricPropagator
    {
        new void Init()
        {
            This.Subscribe(BlockOrder.ElectricFinalize,
                () => RethinkLitState());
        }

        BlockID ID_UNLIT();
        BlockID ID_LIT();
        string ELECTRIC_PIPE_ID();
        bool IS_LIT() => This.BlockData.ID == ID_LIT();

        double IPipe.WIDTH() => 6.0 / 16.0;
        Type[] IPipe.CONNECT_TO_TYPES() => new Type[] {
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
            // 0 - completely unlit
            // 1 - cannot propagate
            
            bool shouldBeLit = Data["electric_propagator.recursion"].Int > 1;
            if (shouldBeLit != IS_LIT())
            {
                BlockID newID = shouldBeLit ? ID_LIT() : ID_UNLIT();
                BlockData1 blockTemplate = new BlockData1(
                    newID, This.BlockData.Variant, Data);

                WorldAPI.ReplaceBlock(This.Position, This.IsFront, blockTemplate,
                    IWorldAPI.BreakMode.Weak);
            }
        }

        void IElectricPropagator.OnElectricSignal(Vec2Int POS_src, int recursion)
        {
            int oldRecursion = Data["electric_propagator.recursion"].Int;
            if (oldRecursion >= recursion) return;

            Data["electric_propagator.recursion"].Int = recursion;

            Vec2Int POS = This.Position;
            int nextRecursion = recursion - 1;

            foreach (Vec2Int dir in CARDINAL_DIRECTIONS)
            {
                Vec2Int POS_other = POS + dir;
                bool? canPropagate = CanPropagateInto(POS_other);
                if (canPropagate == false) continue;

                if (canPropagate == true)
                {
                    IElectricPropagator pipe = (IElectricPropagator)WorldAPI.GetBlock(POS + dir, This.IsFront);
                    pipe.ElectricSignalSend(POS, nextRecursion);
                }

                if (canPropagate == null)
                {
                    Core.Debug.LogWarning($"Tried sending electric signal to unloaded block at {POS_other}!");
                }
            }
        }

        private bool? CanPropagateInto(Vec2Int POS_other)
        {
            Block block = WorldAPI.GetBlock(POS_other, This.IsFront);
            if (block == null) return null;

            if (block is IElectricPropagator pipe)
            {
                return true;
            }
            return false;
        }
    }
}
