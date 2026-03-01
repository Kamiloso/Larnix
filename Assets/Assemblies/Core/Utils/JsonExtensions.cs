using System;
using SimpleJSON;

namespace Larnix.Core.Utils
{
    public static class JsonExtensions
    {
        public static JSONObject AsJsonObject(this string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            try
            {
                return JSON.Parse(str)
                    .AsObject ?? new JSONObject();
            }
            catch
            {
                return new JSONObject();
            }
        }

        public static JSONObject TraversePath(this JSONObject jsonObj, params string[] path)
        {
            if (jsonObj == null)
                throw new ArgumentNullException(nameof(jsonObj));

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
}
