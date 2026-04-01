#nullable enable
using System;
using System.IO;

namespace Larnix.GameCore
{
    public static class GamePath
    {
        private static string? _persistentDataPath;
        private static readonly object _lock = new();

        public static string SavesPath
        {
            get
            {
                string? path = _persistentDataPath;
                return path != null ? Path.Combine(path, "Saves") : throw new InvalidOperationException(
                    "Persistent data path not initialized. Call 'Larnix.GameCore.GamePath.InitPath(...)' to be able to access it.");
            }
        }

        public static void InitPath(string persistentDataPath)
        {
            if (persistentDataPath == null)
                throw new ArgumentNullException(nameof(persistentDataPath));

            lock (_lock)
            {
                if (_persistentDataPath != null)
                    throw new InvalidOperationException("Persistent data path already initialized.");

                _persistentDataPath = persistentDataPath;
            }
        }
    }
}
