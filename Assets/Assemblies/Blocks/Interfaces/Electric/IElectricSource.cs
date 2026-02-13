using Larnix.Core;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public interface IElectricSource : IBlockInterface
    {
        void Init()
        {
            This.Subscribe(BlockEvent.ElectricPropagation,
                (_, _) => StartPropagation());
        }

        byte ElectricEmissionMask(); // up right down left

        private void StartPropagation()
        {
            byte mask = ElectricEmissionMask();

            bool up = (mask & 0b0001) != 0;
            if (up) SendSignal(This.Position + Vec2Int.Up);

            bool right = (mask & 0b0010) != 0;
            if (right) SendSignal(This.Position + Vec2Int.Right);

            bool down = (mask & 0b0100) != 0;
            if (down) SendSignal(This.Position + Vec2Int.Down);

            bool left = (mask & 0b1000) != 0;
            if (left) SendSignal(This.Position + Vec2Int.Left);
        }

        private void SendSignal(Vec2Int POS_other)
        {
            BlockServer block = WorldAPI.GetBlock(POS_other, This.IsFront);
            if (block is null)
            {
                Core.Debug.LogWarning("TODO: Fix electric signal sending to non-loaded blocks.");
                // SendSignal(POS_other)
                return;
            }

            if (block is IElectricPropagator prop)
            {
                int recursion = IElectricPropagator.RECURSION_LIMIT;
                int propRecursion = prop.Data["electric_propagator.recursion"].Int;

                if (recursion > propRecursion)
                {
                    prop.ElectricPropagate(This.Position, recursion);
                }
            }
        }
    }
}
