using Larnix.Core;
using Larnix.Core.Vectors;

namespace Larnix.Blocks.All
{
    public interface IElectricSource : ISecureAtomic
    {
        void Init()
        {
            This.Subscribe(BlockOrder.ElectricPropagation,
                () => StartPropagation());
        }

        byte ElectricEmissionMask(); // up right down left
        bool ElectricTranslayerEmission() => false;

        private void StartPropagation()
        {
            byte mask = ElectricEmissionMask();
            bool translayer = ElectricTranslayerEmission();

            bool up = (mask & 0b0001) != 0;
            if (up) SendSignal(This.Position + Vec2Int.Up);

            bool right = (mask & 0b0010) != 0;
            if (right) SendSignal(This.Position + Vec2Int.Right);

            bool down = (mask & 0b0100) != 0;
            if (down) SendSignal(This.Position + Vec2Int.Down);

            bool left = (mask & 0b1000) != 0;
            if (left) SendSignal(This.Position + Vec2Int.Left);

            if (translayer)
            {
                SendSignal(This.Position, true);
            }
        }

        private void SendSignal(Vec2Int POS_other, bool translayer = false)
        {
            Block block = WorldAPI.GetBlock(POS_other, This.IsFront ^ translayer);
            if (block is null) return;

            if (block is IElectricPropagator prop)
            {
                int recursion = IElectricPropagator.RECURSION_LIMIT;
                int propRecursion = prop.Data["electric_propagator.recursion"].Int;

                if (recursion > propRecursion)
                {
                    prop.ElectricSignalSend(This.Position, recursion);
                }
            }
        }
    }
}
