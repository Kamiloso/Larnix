using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Modules.Blocks
{
    public interface ITool
    {
        void Init()
        {

        }

        public Type TOOL_TYPE(); // what type of material it can mine
        public Tier TOOL_TIER(); // what tier of material it can mine
        public int TOOL_MAX_DURABILITY(); // for how long in can mine
        public double TOOL_SPEED(); // how fast it can mine

        public enum Type
        {
            Normal,
        }

        public enum Tier
        {
            None,
            Wood,
            Stone,
            Copper,
        }
    }
}
