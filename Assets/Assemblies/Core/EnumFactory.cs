using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.ObjectModel;

namespace Larnix.Core
{
    public static class EnumFactory<TEnum, TClass> where TEnum : struct, IConvertible where TClass : class
    {
        public static ReadOnlyDictionary<TEnum, TClass> CreateDictionary(params (Type Type, object Object)[] args)
        {
            if (args.Any(elm => elm.Type is null || elm.Object is null))
                throw new ArgumentNullException("Arguments cannot be null!");
            
            var type = typeof(TClass);
            var assembly = Assembly.GetAssembly(type);

            List<TEnum> enumValues = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .ToList();

            List<TClass> objInstances = assembly.GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                type.IsAssignableFrom(t)
            )
            .Where(t => t.GetConstructor(
                bindingAttr:
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                binder: null,
                types: args.Select(a => a.Type).ToArray(),
                modifiers: null) != null
            )
            .Select(t =>
            {
                object[] parameters = args.Select(a => a.Object).ToArray();
                return (TClass)Activator.CreateInstance(t, parameters);
            })
            .ToList();

            Dictionary<TEnum, TClass> enumDict = enumValues
                .Select(id =>
                {
                    IEnumerable<TClass> matchInsts = objInstances
                        .Where(inst => Enum.TryParse<TEnum>(inst.GetType().Name, out var parsed) &&
                            parsed.Equals(id));
                    
                    if (matchInsts.Count() == 0)
                        throw new InvalidOperationException($"No class found for enum value {id}!");

                    if (matchInsts.Count() > 1)
                        throw new InvalidOperationException($"Multiple classes found for enum value {id}!");

                    return ((TEnum Enum, TClass Class))(id, matchInsts.First());
                })
                .ToDictionary(kvp => kvp.Enum, kvp => kvp.Class);

            return new ReadOnlyDictionary<TEnum, TClass>(enumDict);
        }
    }
}
