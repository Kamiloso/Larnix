#nullable enable
using System;
using System.IO;

namespace Larnix.Core.Files
{
    public class Locker : IDisposable
    {
        private readonly FileStream _fileStream;
        
        private Locker(FileStream fileStream)
        {
            _fileStream = fileStream;
        }

        public static Locker LockOrException(string path, string filename, Func<Exception>? makeException = null)
        {
            FileManager.EnsureDirectory(path);
            try
            {
                FileStream fileStream = new(
                    Path.Combine(path, filename),
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);

                return new Locker(fileStream);
            }
            catch
            {
                throw makeException?.Invoke() ??
                    new IOException($"Cannot acquire lock on file: \"{filename}\"");
            }
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
        }
    }
}
