using System;
using SimpleJSON;

namespace Larnix.Core.Json
{
    public class Storage
    {
        private JSONObject _root;
        private JSONObject Root => _root ??= new JSONObject();

        public Storage() {}
        private Storage(JSONObject root) => _root = root;


        public Node this[string key] => IterateNode(key);

        private Node IterateNode(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new Node(Root);
            
            string[] parts = key.Split('.');

            JSONNode current = Root;
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
            return new Storage(Root.Clone().AsObject);
        }

        public override string ToString() => Root.ToString();
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
