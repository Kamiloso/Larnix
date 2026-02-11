using System.Collections;
using System.Collections.Generic;

namespace Larnix.Blocks
{
    public interface ITool : IBlockInterface
    {
        public Type TOOL_TYPE(); // what type of material it can mine
        public Tier TOOL_TIER(); // what tier of material it can mine
        public int TOOL_MAX_DURABILITY(); // for how long in can mine
        public double TOOL_SPEED(); // how fast it can mine

        public enum Type
        {
            Normal,
            Ultimate, // type bypass (on tools)
        }

        public enum Tier
        {
            None,
            Wood,
            Stone,
            Copper,
            Steel,
            Ultimate // tier bypass (on tools)
        }
    }
}
