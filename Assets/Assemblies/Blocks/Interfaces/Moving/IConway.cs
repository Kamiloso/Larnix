using System;
using System.Collections.Generic;
using System.Linq;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;

namespace Larnix.Blocks.All
{
    public interface IConway : IMovingBehaviour
    {
        void Init()
        {
            This.Subscribe(BlockOrder.PreFrame,
                () => ConwayPrepare());

            This.Subscribe(BlockOrder.Conway,
                () => ConwayFinalize());
        }

        int CONWAY_PERIOD();

        private void ConwayPrepare()
        {
            if (WorldAPI.ServerTick % CONWAY_PERIOD() != 0)
                return;

            // clean state
            This.BlockData.Data["conway"].String = "";

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vec2Int delta = new Vec2Int(dx, dy);
                    PrepareAt(This.Position + delta, This.IsFront);
                }
        }

        private void PrepareAt(Vec2Int POS, bool isFront)
        {
            bool isAlive = IsAliveAt(POS, isFront) == true;
            Vec2Int delta = POS - This.Position;

            List<Block> blocksAround = WorldAPI.GetBlocksAround(POS, isFront);
            int aliveNeighbors = blocksAround.Count(block => IsAliveAt(block.Position, block.IsFront) == true);

            if (!isAlive)
            {
                if (aliveNeighbors == 3)
                    This.BlockData.Data[$"conway.{delta.x}_{delta.y}"].String = "BIRTH";
            }
            else
            {
                if (aliveNeighbors < 2 || aliveNeighbors > 3)
                    This.BlockData.Data[$"conway.{delta.x}_{delta.y}"].String = "DEATH";
            }
        }

        private void ConwayFinalize()
        {
            if (WorldAPI.ServerTick % CONWAY_PERIOD() != 0)
                return;

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    Vec2Int delta = new Vec2Int(dx, dy);
                    FinalizeAt(This.Position + delta, This.IsFront);
                }

            FinalizeAt(This.Position, This.IsFront);
        }

        private void FinalizeAt(Vec2Int POS, bool isFront)
        {
            Vec2Int delta = POS - This.Position;
            string action = This.BlockData.Data[$"conway.{delta.x}_{delta.y}"].String;

            switch (action)
            {
                case "BIRTH":
                    if (IsAliveAt(POS, isFront) == false && CanMove(This.Position, POS, isFront))
                        Move(This.Position, POS, isFront, clone: true);
                    break;
    
                case "DEATH":
                    if (IsAliveAt(POS, isFront) == true)
                        WorldAPI.ReplaceBlock(POS, isFront, BlockData1.Air);
                    break;
            }
        }

        private bool? IsAliveAt(Vec2Int POS, bool isFront)
        {
            Block block = WorldAPI.GetBlock(POS, isFront);
            if (block == null) return null;

            return block.GetType() == This.GetType();
        }
    }
}
