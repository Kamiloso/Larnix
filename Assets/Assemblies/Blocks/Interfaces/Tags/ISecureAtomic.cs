using System.Collections;
using System.Collections.Generic;

namespace Larnix.Blocks.All
{
    /// <summary>
    /// This interface prevents block events from execution
    /// on not fully loaded secure-atomic contraptions,
    /// for example when it comes to electricity.
    /// </summary>
    public interface ISecureAtomic : IBlockInterface { }
}
