using System.Collections;
using System.Collections.Generic;

namespace Larnix.Blocks
{
    public enum ContureType : byte
    {
        Disabled = 0,
        Invisible = 1,
        SemiTransparent = 128,
        Opaque = 255
    }

    public interface IHasConture : IBlockInterface
    {   
        ContureType STATIC_DefinedAlphaEnum(byte variant) => ContureType.SemiTransparent; // override this
        byte STATIC_GetAlphaByte(byte variant) => (byte)STATIC_DefinedAlphaEnum(variant); // use this
    }
}
