using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Larnix.Core
{
    public static class DirectoryUtils
    {
        public static bool AreSameDirectory(string dir1, string dir2)
        {
            if (string.IsNullOrWhiteSpace(dir1) || string.IsNullOrWhiteSpace(dir2))
                return false;

            string full1 = Path.GetFullPath(dir1);
            string full2 = Path.GetFullPath(dir2);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return string.Equals(full1, full2, StringComparison.OrdinalIgnoreCase);
            else
                return string.Equals(full1, full2, StringComparison.Ordinal);
        }
    }
}
