using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public interface IElectricPropagator : IBlockInterface
    {
        void Init()
        {
            This.PreFrameEvent += (sender, args) => Data["electric_propagator.recursion"].Int = 0;
        }

        void ElectricPropagate(Vec2Int POS_src, int recursion);
    }
}
