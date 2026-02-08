using System.Collections;
using System.IO;
using System;

namespace Larnix.Core.Files
{
    public static class FileManager
    {
        public static string Read(string path, string filename)
        {
            FilenameCheck(filename);
            EnsureDirectory(path);

            string file = Path.Combine(path, filename);
            string _file = Path.Combine(path, "_" + filename);
            string __file = Path.Combine(path, "__" + filename);

            if (File.Exists(__file))
                File.Delete(__file);

            if (File.Exists(file))
            {
                if (File.Exists(_file))
                    File.Delete(_file);

                return File.ReadAllText(file);
            }
            else if (File.Exists(_file))
            {
                File.Move(_file, file);
                return File.ReadAllText(file);
            }

            return null;
        }

        public static void Write(string path, string filename, string text)
        {
            FilenameCheck(filename);
            EnsureDirectory(path);

            string file = Path.Combine(path, filename);
            string _file = Path.Combine(path, "_" + filename);
            string __file = Path.Combine(path, "__" + filename);

            using (var fs = new FileStream(__file, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(text);
                sw.Flush();
                fs.Flush(true);
            }

            File.Move(__file, _file);

            if (File.Exists(file))
                File.Delete(file);

            File.Move(_file, file);
        }

        public static byte[] ReadBinary(string path, string filename)
        {
            FilenameCheck(filename);
            EnsureDirectory(path);

            string file = Path.Combine(path, filename);
            string _file = Path.Combine(path, "_" + filename);
            string __file = Path.Combine(path, "__" + filename);

            if (File.Exists(__file))
                File.Delete(__file);

            if (File.Exists(file))
            {
                if (File.Exists(_file))
                    File.Delete(_file);

                return File.ReadAllBytes(file);
            }
            else if (File.Exists(_file))
            {
                File.Move(_file, file);
                return File.ReadAllBytes(file);
            }

            return null;
        }

        public static void WriteBinary(string path, string filename, byte[] bytes)
        {
            FilenameCheck(filename);
            EnsureDirectory(path);

            string file = Path.Combine(path, filename);
            string _file = Path.Combine(path, "_" + filename);
            string __file = Path.Combine(path, "__" + filename);

            using (var fs = new FileStream(__file, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var sw = new BinaryWriter(fs))
            {
                sw.Write(bytes);
                sw.Flush();
                fs.Flush(true);
            }

            File.Move(__file, _file);

            if (File.Exists(file))
                File.Delete(file);

            File.Move(_file, file);
        }

        public static void Delete(string path, string filename)
        {
            string file = Path.Combine(path, filename);
            string _file = Path.Combine(path, "_" + filename);
            string __file = Path.Combine(path, "__" + filename);

            if (File.Exists(__file))
                File.Delete(__file);

            if (File.Exists(_file))
                File.Delete(_file);

            if (File.Exists(file))
                File.Delete(file);
        }

        public static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static void FilenameCheck(string filename)
        {
            if (string.IsNullOrEmpty(filename)) throw new NullReferenceException();
            if (filename.StartsWith('_')) throw new ArgumentException("Filename cannot start with '_'!");
        }
    }
}
