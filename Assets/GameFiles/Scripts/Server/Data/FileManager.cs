using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Larnix.Socket.Data
{
    public static class FileManager
    {
        public static volatile bool DontSave = false; // This flag can be set to true to prevent saving actions

        public static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
