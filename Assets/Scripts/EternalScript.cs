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
                obj.AddComponent<Server.ServerInstancer>();
                DontDestroyOnLoad(obj);
            }
        }

        private void Update()
        {
            Debug.FlushLogs();
        }
    }

}
