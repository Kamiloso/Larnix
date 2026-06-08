#nullable enable
using Larnix.Model.Json;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Larnix.Model.Configs;

public abstract class BaseConfig<T> where T : BaseConfig<T>, new()
{
    public virtual void Migrate() { }
    
    protected BaseConfig() { }

    protected BaseConfig(string json)
    {
        var jsonObj = JsonHelpers.ToJsonObject(json);

        List<PropertyInfo> props = AllProperties();
        foreach (var prop in props)
        {
            Type propType = prop.PropertyType;

            string[] parts = prop.Name.Split('_');
            string lastPart = parts[^1];

            var traversed = JsonHelpers.TraversePath(jsonObj, parts[..^1]);
            var node = traversed[lastPart];

            if (TryConvertNode(node, propType, out object value))
            {
                prop.SetValue((T)this, value);
            }
        }

        Migrate();
    }

    protected BaseConfig(T original) : this(original.ToJson()) { }

    public string ToJson(int shift)
    {
        JSONObject json = new();
        List<PropertyInfo> props = AllProperties();

        foreach (PropertyInfo prop in props)
        {
            string[] parts = prop.Name.Split('_');
            string lastPart = parts[^1];

            JSONObject traversed = JsonHelpers.TraversePath(json, parts[..^1]);
            object value = prop.GetValue(this);
            traversed[lastPart] = ToNode(value);
        }

        return json.ToString(shift);
    }

    public string ToJson()
    {
        return ToJson(4);
    }

    protected static List<PropertyInfo> AllProperties()
    {
        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(prop => prop.MetadataToken) // heuristic, but it works in practice
            .ToList();
    }

    protected static void MoveProperties(T source, T target)
    {
        List<PropertyInfo> props = AllProperties();
        foreach (var prop in props)
        {
            prop.SetValue(target, prop.GetValue(source));
        }
    }

    private static bool TryConvertNode(JSONNode node, Type type, out object value)
    {
        value = null!;

        try
        {
            if (node == null)
            {
                return false;
            }

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
            JSONArray array = new();
            foreach (object item in list)
            {
                JSONNode node = ToNode(item);
                array.Add(node);
            }
            return array;
        }

        throw new NotImplementedException($"Type '{type}' is unsupported!");
    }
}
