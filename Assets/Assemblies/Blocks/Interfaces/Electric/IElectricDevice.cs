using Larnix.Core;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public interface IElectricDevice : IElectricPropagator
    {
        new void Init()
        {
            This.PreFrameEvent += (sender, args) => Data["electric_device.ticked"].Bool = false;
            This.FrameEventElectricFinalize += (sender, args) => {
                RememberDeviceState();
                FinalizeFrameDevice(Data["electric_device.ticked"].Bool);
            };
        }

        void StartDevice(); // executes on signal start (ElectricPropagation)
        void FrameDevice(); // executes when signal present, once per frame (ElectricPropagation)
        void FinalizeFrameDevice(bool isPowered); // executes once per frame (ElectricFinalize)

        private void RememberDeviceState() =>
            Data["electric_device.ticked_before"].Bool = Data["electric_device.ticked"].Bool;

        void IElectricPropagator.ElectricPropagate(Vec2Int POS_src, int recursion)
        {
            Data["electric_propagator.recursion"].Int = recursion;

            if (!Data["electric_device.ticked"].Bool)
            {
                Data["electric_device.ticked"].Bool = true;

                if (!Data["electric_device.ticked_before"].Bool)
                {
                    StartDevice();
                }

                FrameDevice();
            }
        }
    }
}
