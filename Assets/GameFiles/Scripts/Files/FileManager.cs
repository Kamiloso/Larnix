using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Larnix.Files
{
    public static class FileManager
    {
        public static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static string Read(string path, string filename)
        {
            EnsureDirectory(path);
            string file = Path.Combine(path, filename);

            if (!File.Exists(file))
                return null;

            return File.ReadAllText(file);
        }

        public static void Write(string path, string filename, string text)
        {
            EnsureDirectory(path);
            string file = Path.Combine(path, filename);

            File.WriteAllText(file, text);
        }
    }
}
