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
            Server.ServerRunner.Instance.Start(ServerType.Remote, worldPath, null);
        }

        private void Update()
        {
            if (!Server.ServerRunner.Instance.IsRunning)
            {
                Application.Quit();
            }
        }
    }
}
