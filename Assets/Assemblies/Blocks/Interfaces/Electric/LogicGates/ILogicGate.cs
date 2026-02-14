using Larnix.Blocks.Structs;

namespace Larnix.Blocks.All
{
    public interface ILogicGate : ISolidElectric, IElectricSource, IElectricDevice, IRotational
    {
        byte LogicInToOut(byte input);
        bool EMITS_TRANSLAYER() => false;
        byte LOGIC_LIT_BIT() => 0b0100;

        byte IElectricSource.ElectricEmissionMask() =>
            (byte)Data["logic_gate.emission"].Int;

        bool IElectricSource.ElectricTranslayerEmission() =>
            EMITS_TRANSLAYER() && Data["logic_gate.ticked"].Bool;

        void IElectricDevice.DeviceTick(bool active, bool wentOn, bool wentOff, byte srcByte)
        {
            byte outByte = RealInToOut(srcByte);

            Data["logic_gate.emission"].Int = outByte;
            Data["logic_gate.ticked"].Bool = active;
            
            bool isLit = (This.BlockData.Variant & LOGIC_LIT_BIT()) != 0;
            bool shouldBeLit = outByte != 0 || (EMITS_TRANSLAYER() && active);
            
            if (isLit != shouldBeLit)
            {
                byte newVariant = (byte)(This.BlockData.Variant ^ LOGIC_LIT_BIT());
                WorldAPI.SetBlockVariant(This.Position, This.IsFront, newVariant, IWorldAPI.BreakMode.Weak);
            }
        }

        private byte RealInToOut(byte input)
        {
            int shift = This.BlockData.Variant & ROTATION_MASK;
            byte rolled = RollByte4(input, -shift);
            byte processed = LogicInToOut(rolled);
            byte unrolled = RollByte4(processed, shift);
            return unrolled;
        }

        protected static byte RollByte4(byte b, int shift)
        {
            shift %= 4;
            if (shift < 0) shift += 4;
            return (byte)(((b << shift) | (b >> (4 - shift))) & 0b1111);
        }
    }
}
