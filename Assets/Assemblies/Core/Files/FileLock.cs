#nullable enable
using Larnix.Core.Files;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

public class FileLock : IDisposable
{
    private readonly FileStream _fileStream;
    private bool _disposed;

    protected FileLock(string path, string lockname)
    {
        FileHelpers.CheckFilePathValidity(path, lockname);
        FileHelpers.FilenameCheck(lockname, true);

        FileHelpers.EnsureDirectory(path);

        try
        {
            string file = Path.Combine(path, lockname);

            _fileStream = new FileStream(
                path: file,
                mode: FileMode.OpenOrCreate,
                access: FileAccess.ReadWrite,
                share: FileShare.None,
                bufferSize: 4096,
                options: FileOptions.DeleteOnClose
                );
        }
        catch (IOException ex)
        {
            Dispose();
            throw new TimeoutException($"Failed to acquire lock on path '{path}', lockname '{lockname}'.", ex);
        }
    }

    public static FileLock Acquire(string path, string lockname, long timeout)
    {
        var sw = Stopwatch.StartNew();

        while (true)
        {
            try
            {
                return new FileLock(path, lockname);
            }
            catch (TimeoutException)
            {
                if (timeout == 0) throw;
                if (sw.ElapsedMilliseconds >= timeout) throw;

                Thread.Sleep(10);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _fileStream?.Dispose();
    }
}
