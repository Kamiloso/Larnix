#nullable enable
using System;
using System.IO;

namespace Larnix.Core.Files;

internal static class FileHelpers
{
    public static void CheckFilePathValidity(string path, string filename)
    {
        try
        {
            string file = Path.Combine(path, filename);
            _ = Path.GetFullPath(file);
        }
        catch (Exception)
        {
            throw new ArgumentException("Invalid file path!");
        }
    }

    public static void FilenameCheck(string filename, bool isLock)
    {
        if (string.IsNullOrEmpty(filename))
            throw new ArgumentNullException(nameof(filename));

        if (filename.StartsWith('_'))
            throw new ArgumentException("Filename cannot start with '_'!", nameof(filename));

        if (!isLock && filename.StartsWith('~'))
            throw new ArgumentException("Normal filename cannot start with '~'!", nameof(filename));

        if (isLock && !filename.StartsWith('~'))
            throw new ArgumentException("Lock filename must start with '~'!", nameof(filename));

        if (filename.Contains('/') || filename.Contains('\\'))
            throw new ArgumentException("Filename cannot contain path separators!", nameof(filename));
    }

    public static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
