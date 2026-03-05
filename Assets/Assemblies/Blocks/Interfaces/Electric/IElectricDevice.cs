using Larnix.Core;
using Larnix.Core.Vectors;

namespace Larnix.Blocks.All
{
    public interface IElectricDevice : IElectricPropagator
    {
        protected static byte UP => 0b0001;
        protected static byte RIGHT => 0b0010;
        protected static byte DOWN => 0b0100;
        protected static byte LEFT => 0b1000;

        new void Init()
        {
            This.Subscribe(BlockOrder.PreFrame, () => {
                Data["electric_device.ticked"].Bool = false;
                Data["electric_device.tick_byte"].Int = 0;
            });

            This.Subscribe(BlockOrder.ElectricFinalize, () => {
                Data["electric_device.ticked_before"].Bool = Data["electric_device.ticked"].Bool;
            });

            This.Subscribe(BlockOrder.ElectricDevices, () => {
                bool active = Data["electric_device.ticked"].Bool;
                bool wasActive = Data["electric_device.ticked_before"].Bool;
                byte srcByte = (byte)Data["electric_device.tick_byte"].Int;

                DeviceTick(active,
                    wentOn: active && !wasActive,
                    wentOff: !active && wasActive,
                    srcByte: srcByte
                    );
            });
        }

        void DeviceTick(bool active, bool wentOn, bool wentOff, byte srcByte);

        void IElectricPropagator.OnElectricSignal(Vec2Int POS_src, int recursion)
        {
            int oldRecursion = Data["electric_propagator.recursion"].Int;
            if (recursion > oldRecursion)
            {
                Data["electric_propagator.recursion"].Int = recursion;
                Data["electric_device.ticked"].Bool = true;
            }

            if (recursion > 0)
            {
                Vec2Int dir = POS_src - This.Position;
                if (dir == Vec2Int.Up) Data["electric_device.tick_byte"].Int |= UP;
                else if (dir == Vec2Int.Right) Data["electric_device.tick_byte"].Int |= RIGHT;
                else if (dir == Vec2Int.Down) Data["electric_device.tick_byte"].Int |= DOWN;
                else if (dir == Vec2Int.Left) Data["electric_device.tick_byte"].Int |= LEFT;
            }
        }
    }
}
