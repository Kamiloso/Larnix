using System;
using System.Collections;
using System.Linq;
using Larnix.Core.Files;
using System.Reflection;
using SimpleJSON;
using System.Collections.Generic;

namespace Larnix.GameCore.Json
{
    public abstract class Config
    {
        public const string DEFAULT_FILE = "config.json";
        public virtual void Update() { }
        
        public static T FromString<T>(string str) where T : Config, new()
        {
            JSONObject json = str.AsJsonObject();
            List<PropertyInfo> props = AllProperties<T>();

            T config = new();

            foreach (PropertyInfo prop in props)
            {
                string[] parts = prop.Name.Split('_');
                string lastPart = parts[^1];
                
                JSONObject traversed = json.TraversePath(parts[..^1]);
                JSONNode node = traversed[lastPart];

                if (TryConvertNode(node, prop.PropertyType, out object parsedValue))
                {
                    prop.SetValue(config, parsedValue);
                }
            }
            
            config.Update();
            return config;
        }

        public static string AsString<T>(T config) where T : Config
        {
            JSONObject json = new();
            List<PropertyInfo> props = AllProperties<T>();

            foreach (PropertyInfo prop in props)
            {
                string[] parts = prop.Name.Split('_');
                string lastPart = parts[^1];

                JSONObject traversed = json.TraversePath(parts[..^1]);
                object value = prop.GetValue(config);
                traversed[lastPart] = ToNode(value);
            }
            return json.ToString(4);
        }

        public static T DeepCopy<T>(T config) where T : Config, new()
        {
            string str = AsString(config);
            return FromString<T>(str);
        }

        public static T FromFile<T>(string path, string file) where T : Config, new()
        {
            string data = FileManager.Read(path, file);
            T config = FromString<T>(data ?? string.Empty);
            ToFile(path, file, config); // update data
            return config;
        }

        public static void ToFile<T>(string path, string file, T config) where T : Config
        {
            string data = AsString(config);
            FileManager.Write(path, file, data);
        }

        public static T FromDirectory<T>(string path) where T : Config, new()
        {
            return FromFile<T>(path, DEFAULT_FILE);
        }

        public static void ToDirectory<T>(string path, T config) where T : Config
        {
            ToFile(path, DEFAULT_FILE, config);
        }

        protected static List<PropertyInfo> AllProperties<T>() where T : Config
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(prop => prop.MetadataToken)
                .ToList();
        }

        private static bool TryConvertNode(JSONNode node, Type type, out object value)
        {
            if (node == null)
            {
                value = default;
                return false;
            }

            try
            {
                if (type == typeof(string))
                {
                    value = node.Value;
                    return true;
                }
                
                if (type == typeof(bool))
                {
                    value = node.AsBool;
                    return true;
                }
                
                if (type.IsPrimitive) 
                {
                    value = Convert.ChangeType(node.AsDouble, type);
                    return true;
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type itemType = type.GetGenericArguments()[0];
                    if (node is JSONArray array)
                    {
                        IList list = (IList)Activator.CreateInstance(type);
                        foreach (JSONNode item in array)
                        {
                            if (TryConvertNode(item, itemType, out object itemValue))
                            {
                                list.Add(itemValue);
                            }
                        }
                        value = list;
                        return true;
                    }
                }
            }
            catch (InvalidCastException) { }
            catch (FormatException) { }

            value = default;
            return false;
        }

        private static JSONNode ToNode(object obj)
        {
            if (obj == null) 
                return JSONNull.CreateOrGet();

            Type type = obj.GetType();

            if (type == typeof(string))
                return new JSONString((string)obj);
            
            if (type == typeof(bool))
                return new JSONBool((bool)obj);
            
            if (type.IsPrimitive)
                return new JSONNumber(Convert.ToDouble(obj));

            if (obj is IList list)
            {
                JSONArray array = new JSONArray();
                foreach (object item in list)
                {
                    array.Add(ToNode(item));
                }
                return array;
            }

            throw new NotImplementedException($"Type {type} is unsupported!");
        }
    }
}
