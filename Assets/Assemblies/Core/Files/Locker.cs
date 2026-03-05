using System;
using System.Collections;
using System.IO;

namespace Larnix.Core.Files
{
    public class Locker : IDisposable
    {
        public readonly string _file;
        private FileStream _fileStream = null;
        
        private Locker(string file)
        {
            _file = file;
        }

        public static Locker LockOrException(string path, string filename, Func<Exception> makeException = null)
        {
            Locker locker = new Locker(Path.Combine(path, filename));

            FileManager.EnsureDirectory(path);
            try
            {
                locker._fileStream = new FileStream(
                    locker._file,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch
            {
                throw makeException?.Invoke() ??
                    new IOException($"Cannot acquire lock on file: {locker._file}");
            }

            return locker;
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
        }
    }
}
