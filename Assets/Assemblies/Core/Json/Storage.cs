using System;
using SimpleJSON;

namespace Larnix.Core.Json
{
    public class Storage
    {
        private JSONObject _root;

        public Storage() => _root = new JSONObject();
        private Storage(JSONObject root) => _root = root ?? new JSONObject();


        public Node this[string key] => IterateNode(key);

        private Node IterateNode(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new Node(_root);
            
            string[] parts = key.Split('.');

            JSONNode current = _root;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                JSONNode next = current[part];

                if (i == parts.Length - 1) // last node
                {
                    if (next is not JSONString)
                        current[part] = new JSONString(string.Empty);
                }
                else // intermediate node
                {
                    if (next is not JSONObject)
                        current[part] = new JSONObject();
                }
                current = current[part];
            }

            return new Node(current);
        }

        public Storage DeepCopy()
        {
            return new Storage(_root.Clone().AsObject);
        }

        public override string ToString() => _root.ToString();
        public static Storage FromString(string json)
        {
            try
            {
                return new Storage(JSON.Parse(json).AsObject);
            }
            catch
            {
                return new Storage();
            }
        }
    }
}
