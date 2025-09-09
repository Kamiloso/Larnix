using System.Linq;
using UnityEngine;

namespace Larnix
{
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

            var types = typeof(EternalScript).Assembly.GetTypes()
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) &&
                            interfaceType.IsAssignableFrom(t));

            foreach (var type in types)
            {
                obj.AddComponent(type);
            }
        }

        private void Update()
        {
            Debug.FlushLogs();
        }
    }

}
