using System;
using System.Linq;
using UnityEngine;

namespace Larnix.Patches
{
    public interface IGlobalUnitySingleton { }

    public class EternalScript : MonoBehaviour
    {
        private static EternalScript instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnStart()
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("EternalScript");
                instance = obj.AddComponent<EternalScript>();
                SpawnUnitySingletons(obj);
                DontDestroyOnLoad(obj);
            }
        }

        private static void SpawnUnitySingletons(GameObject obj)
        {
            var interfaceType = typeof(IGlobalUnitySingleton);

            var assembliesToSearch = new[]
            {
                typeof(EternalScript).Assembly // default assembly
            };

            var types = assembliesToSearch
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) &&
                            interfaceType.IsAssignableFrom(t) &&
                            !t.IsAbstract);

            foreach (var type in types)
            {
                obj.AddComponent(type);
            }
        }
    }
}
