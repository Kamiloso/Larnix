#nullable enable
using System;
using System.IO;

namespace Larnix.Core.Files;

public static class FileManager
{
    private static readonly object _lock = new();

    public static void Write(string path, string filename, string text)
    {
        lock (_lock)
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
    }

    public static string? Read(string path, string filename)
    {
        lock (_lock)
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
    }

    public static void Delete(string path, string filename)
    {
        lock (_lock)
        {
            FilenameCheck(filename);
            EnsureDirectory(path);

            string file = Path.Combine(path, filename);
            string _file = Path.Combine(path, "_" + filename);
            string __file = Path.Combine(path, "__" + filename);

            foreach (string delfile in new[] { __file, _file, file })
            {
                if (File.Exists(delfile))
                    File.Delete(delfile);
            }
        }
    }

    private static void FilenameCheck(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            throw new ArgumentNullException(nameof(filename));

        if (filename.StartsWith('_'))
            throw new ArgumentException("Filename cannot start with '_'!", nameof(filename));

        if (filename.Contains('/') || filename.Contains('\\'))
            throw new ArgumentException("Filename cannot contain path separators!", nameof(filename));
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
