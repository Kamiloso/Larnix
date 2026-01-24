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
                    "Appdata path not initialized. Call 'Larnix.Core.GamePath.InitAppdata(...)' to be able to access it.");

            return path;
        }

        public static void InitAppdata(string persistentDataPath)
        {
            if (persistentDataPath == null)
                throw new ArgumentNullException(nameof(persistentDataPath));

            lock (_lock)
            {
                if (_persistentDataPath != null)
                    throw new InvalidOperationException("Appdata path already initialized.");

                _persistentDataPath = persistentDataPath;
            }
        }
    }
}
