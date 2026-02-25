using Larnix.Core.Vectors;

namespace Larnix.Blocks.All
{
    public interface IFalling : IMovingBehaviour
    {
        void Init()
        {
            This.Subscribe(BlockOrder.Sequential,
                () => Fall());
        }

        int FALL_PERIOD();

        private void Fall()
        {
            if (WorldAPI.ServerTick % FALL_PERIOD() != 0)
                return;

            Vec2Int localpos = This.Position;
            Vec2Int downpos = localpos - new Vec2Int(0, 1);

            if (CanMove(localpos, downpos, This.IsFront))
            {
                Move(localpos, downpos, This.IsFront);
            }
        }
    }
}
