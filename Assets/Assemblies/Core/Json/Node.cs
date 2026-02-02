using System;
using System.Collections.Generic;
using System.Globalization;
using SimpleJSON;

namespace Larnix.Core.Json
{
    public class Node
    {
        private JSONNode _node;
        internal Node(JSONNode node) => _node = node;

        public string String
        {
            get => _node.Value;
            set => _node.Value = value;
        }

        public int Int
        {
            get => int.TryParse(_node.Value, out int result) ? result : default;
            set => _node.Value = value.ToString();
        }

        public float Float
        {
            get => float.TryParse(_node.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : default;
            set => _node.Value = value.ToString(CultureInfo.InvariantCulture);
        }

        public bool Bool
        {
            get => bool.TryParse(_node.Value, out bool result) ? result : default;
            set => _node.Value = value.ToString();
        }
    }
}
