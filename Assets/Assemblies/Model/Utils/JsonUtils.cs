#nullable enable
using SimpleJSON;

namespace Larnix.Model;

public static class JsonUtils
{
    public static JSONObject ToJsonObject(string? json)
    {
        try
        {
            return JSON.Parse(json)
                .AsObject ?? new JSONObject();
        }
        catch
        {
            return new JSONObject();
        }
    }

    public static JSONObject TraversePath(JSONObject jsonObj, params string[] path)
    {
        JSONObject current = jsonObj;
        foreach (string part in path)
        {
            if (current[part] is not JSONObject)
                current[part] = new JSONObject();
            current = current[part].AsObject;
        }
        return current;
    }
}
