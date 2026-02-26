using System.Collections;
using System.IO;
using System;

namespace Larnix.Core.Files
{
    public static class FileManager
    {
        private const string TOMBSTONE_FILE = ".tombstone.tmp";

        public static string Read(string path, string filename) => Read(path, filename, false);
        private static string Read(string path, string filename, bool ignoreTombstone)
        {
            FilenameCheck(filename, ignoreTombstone);
            EnsureDirectory(path);

            if (!ignoreTombstone)
            {
                TombstoneCheck(path);
            }

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

        public static void Write(string path, string filename, string text) => Write(path, filename, text, false);
        private static void Write(string path, string filename, string text, bool ignoreTombstone)
        {
            FilenameCheck(filename, ignoreTombstone);
            EnsureDirectory(path);

            if (!ignoreTombstone)
            {
                TombstoneCheck(path);
            }

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

        public static void Delete(string path, params string[] filenames)
        {
            TombstoneCreate(path, filenames);
            TombstoneCheck(path);
        }

        public static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static void TombstoneCreate(string path, params string[] filenames)
        {
            string fileList = string.Join("/", filenames);
            Write(path, TOMBSTONE_FILE, fileList, true);
        }

        private static void TombstoneCheck(string path)
        {
            string fileList = Read(path, TOMBSTONE_FILE, true);
            if (fileList != null)
            {
                string[] filenames = fileList.Split('/');
                foreach (string filename in filenames)
                {
                    string file = Path.Combine(path, filename);
                    string _file = Path.Combine(path, "_" + filename);
                    string __file = Path.Combine(path, "__" + filename);

                    if (File.Exists(file))
                        File.Delete(file);

                    if (File.Exists(_file))
                        File.Delete(_file);

                    if (File.Exists(__file))
                        File.Delete(__file);
                }

                string tombfile = Path.Combine(path, TOMBSTONE_FILE);
                if (File.Exists(tombfile))
                    File.Delete(tombfile);
            }
        }

        private static void FilenameCheck(string filename, bool ignoreTombstone)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));
            
            if (filename.StartsWith('_'))
                throw new ArgumentException("Filename cannot start with '_'!", nameof(filename));

            if (filename.Contains('/') || filename.Contains('\\'))
                throw new ArgumentException("Filename cannot contain path separators!", nameof(filename));            
            
            if (!ignoreTombstone && filename == TOMBSTONE_FILE)
                throw new ArgumentException($"Filename \"{TOMBSTONE_FILE}\" is reserved!", nameof(filename));
        }
    }
}
