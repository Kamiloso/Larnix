using Larnix.Blocks.Structs;

namespace Larnix.Blocks
{
    public interface ILamp : IElectricDevice
    {
        byte LAMP_LIT_BYTE() => 0b0001;

        void IElectricDevice.StartDevice()
        {
            ;
        }

        void IElectricDevice.FrameDevice()
        {
            ;
        }

        void IElectricDevice.FinalizeFrameDevice(bool isPowered)
        {
            bool isLit = (This.BlockData.Variant & LAMP_LIT_BYTE()) != 0;

            if (isPowered != isLit)
            {
                BlockData1 blockTemplate = new(
                    id: This.BlockData.ID,
                    variant: (byte)(This.BlockData.Variant ^ LAMP_LIT_BYTE()),
                    data: This.BlockData.Data
                );
                WorldAPI.ReplaceBlock(This.Position, This.IsFront, blockTemplate, IWorldAPI.BreakMode.Weak);
            }
        }
    }
}
