using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;

namespace Larnix.Blocks.All
{
    public interface IElectricPropagator : ISecureAtomic
    {
        protected static Vec2Int[] CARDINAL_DIRECTIONS = new[] {
            Vec2Int.Up, Vec2Int.Right, Vec2Int.Down, Vec2Int.Left
            };
        
        public static int RECURSION_LIMIT => 16;

        void Init()
        {
            This.Subscribe(BlockOrder.PreFrame,
                 () => Data["electric_propagator.recursion"].Int = 0);
        }

        void ElectricSignalSend(Vec2Int POS_src, int recursion)
        {
            if (This.EventFlag)
            {
                OnElectricSignal(POS_src, recursion);
            }
        }

        void OnElectricSignal(Vec2Int POS_src, int recursion);
    }
}
