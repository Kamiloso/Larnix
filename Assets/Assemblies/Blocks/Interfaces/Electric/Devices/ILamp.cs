using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public interface ILamp : IElectricDevice
    {
        byte LAMP_LIT_BIT() => 0b0001;

        void IElectricDevice.DeviceTick(bool active, bool wentOn, bool wentOff, byte srcByte)
        {
            bool isLit = (This.BlockData.Variant & LAMP_LIT_BIT()) != 0;
            
            if (active != isLit)
            {
                byte newVariant = (byte)(This.BlockData.Variant ^ LAMP_LIT_BIT());
                SelfChangeVariant(newVariant);
            }
        }
    }
}
