using System;
using System.Collections.Generic;
using SimpleJSON;

namespace Larnix.Core
{
    public class Node
    {
        private JSONNode _node;
        internal Node(JSONNode node) => _node = node;

        public int Int
        {
            get => _node.AsInt;
            set => _node.AsInt = value;
        }

        public float Float
        {
            get => _node.AsFloat;
            set => _node.AsFloat = value;
        }

        public string String
        {
            get => _node.Value;
            set => _node.Value = value;
        }

        public bool Bool
        {
            get => _node.AsBool;
            set => _node.AsBool = value;
        }
    }
}
