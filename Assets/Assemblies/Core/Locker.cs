using System;
using System.Collections;
using System.IO;
using Socket;

namespace Larnix
{
    public class Locker : IDisposable
    {
        public readonly string file;
        private FileStream fileStream = null;
        
        private Locker(string file)
        {
            this.file = file;
        }

        public static Locker TryLock(string path, string filename)
        {
            Locker locker = new Locker(Path.Combine(path, filename));

            FileManager.EnsureDirectory(path);
            try
            {
                locker.fileStream = new FileStream(
                    locker.file,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch { return null; }

            return locker;
        }

        public void Dispose()
        {
            fileStream?.Dispose();
        }
    }
}
