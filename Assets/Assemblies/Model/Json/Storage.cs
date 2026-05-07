#nullable enable
using SimpleJSON;

namespace Larnix.Model.Json;

public partial class Storage
{
    private JSONObject? _root;
    private JSONObject Root => _root ??= new JSONObject();

    public Node this[string key] => IterateNode(key);

    public Storage() {}
    private Storage(JSONObject root) => _root = root;

    public static Storage FromString(string? json)
    {
        JSONObject jsonObject = JsonUtils.ToJsonObject(json);
        return new Storage(jsonObject);
    }

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
        return _root != null
            ? new Storage(Root.Clone().AsObject)
            : new Storage();
    }

    public override string ToString()
    {
        return _root?.ToString() ?? "{}";
    }
}
