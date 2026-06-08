#nullable enable
using System.IO;

namespace Larnix.Core.Files;

public static class FileManager
{
    private const int TIMEOUT = 2000;

    public static void Write(string path, string filename, string text)
    {
        FileHelpers.CheckFilePathValidity(path, filename);
        FileHelpers.FilenameCheck(filename, false);

        using var _ = FileLock.Acquire(path, $"~{filename}", TIMEOUT);

        _Cleanup(path, filename);

        string file = Path.Combine(path, filename);
        string _file = Path.Combine(path, $"_{filename}");
        string __file = Path.Combine(path, $"__{filename}");

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

    public static string? Read(string path, string filename)
    {
        FileHelpers.CheckFilePathValidity(path, filename);
        FileHelpers.FilenameCheck(filename, false);

        using var _ = FileLock.Acquire(path, $"~{filename}", TIMEOUT);

        _Cleanup(path, filename);

        string file = Path.Combine(path, filename);
        string _file = Path.Combine(path, $"_{filename}");
        string __file = Path.Combine(path, $"__{filename}");

        if (File.Exists(file))
        {
            return File.ReadAllText(file);
        }

        return null;
    }

    public static void Delete(string path, string filename)
    {
        FileHelpers.CheckFilePathValidity(path, filename);
        FileHelpers.FilenameCheck(filename, false);

        using var _ = FileLock.Acquire(path, $"~{filename}", TIMEOUT);

        _Cleanup(path, filename);

        string file = Path.Combine(path, filename);

        if (File.Exists(file))
            File.Delete(file);
    }

    private static void _Cleanup(string path, string filename)
    {
        FileHelpers.EnsureDirectory(path);

        string file = Path.Combine(path, filename);
        string _file = Path.Combine(path, $"_{filename}");
        string __file = Path.Combine(path, $"__{filename}");

        if (File.Exists(__file))
            File.Delete(__file);

        if (File.Exists(_file) && !File.Exists(file))
            File.Move(_file, file);

        if (File.Exists(_file))
            File.Delete(_file);
    }
}
