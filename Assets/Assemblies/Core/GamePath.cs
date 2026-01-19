using System;
using System.IO;

namespace Larnix.Core
{
    public static class GamePath
    {
        private static readonly object _lock = new();
        private static string _persistentDataPath;

        public static string SavesPath => Path.Combine(GetDataPath(), "Saves");

        private static string GetDataPath()
        {
            var path = _persistentDataPath;
            if (path == null)
                throw new InvalidOperationException(
                    "GamePath not initialized. Call GamePath.InitPath() first.");

            return path;
        }

        public static void InitPath(string persistentDataPath)
        {
            if (persistentDataPath == null)
                throw new ArgumentNullException(nameof(persistentDataPath));

            lock (_lock)
            {
                if (_persistentDataPath != null)
                    throw new InvalidOperationException("GamePath already initialized.");

                _persistentDataPath = persistentDataPath;
            }
        }
    }
}
