using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Larnix.Server;

namespace Larnix.ServerRun
{
    public class RemoteServerHolder : MonoBehaviour
    {
        private void Start()
        {
            string worldPath = Path.Combine(".", "World");
            ServerInstancer.Instance.StartServer(ServerType.Remote, worldPath, null);
        }

        private void Update()
        {
            if (!ServerInstancer.Instance.IsRunning)
            {
                Application.Quit();
            }
        }
    }
}
