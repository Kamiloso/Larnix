using System.Collections;
using System.Collections.Generic;

namespace Larnix.Blocks.All
{
    public interface IHasConture : IBlockInterface
    {   
        ContureType STATIC_DefinedAlphaEnum(byte variant) => ContureType.SemiTransparent; // override this
        byte STATIC_GetAlphaByte(byte variant) => (byte)STATIC_DefinedAlphaEnum(variant); // use this
    }

    public enum ContureType : byte
    {
        Disabled = 0,
        Invisible = 1,
        LightSemiTransparent = 64,
        SemiTransparent = 128,
        DarkSemiTransparent = 192,
        Opaque = 255
    }
}
