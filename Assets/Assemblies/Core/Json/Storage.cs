using System;
using SimpleJSON;

namespace Larnix.Core.Json
{
    public class Storage
    {
        private JSONObject _root;

        private Storage(JSONObject root) => _root = root;
        public Storage() => _root = new();

        public Node this[string key] =>
            IterateNode(key);

        private Node IterateNode(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new Node(_root);
            
            string[] parts = key.Split('.');

            JSONNode current = _root;
            foreach (string part in parts)
            {
                if (current[part] == null)
                    current[part] = new JSONObject();
                
                current = current[part];
            }
            return new Node(current);
        }

        public override string ToString() => _root.ToString();
        public static Storage FromString(string json) => new(JSON.Parse(json).AsObject);
    }
}
