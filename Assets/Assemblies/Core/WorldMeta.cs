using System.Collections;
using System.Collections.Generic;
using System.IO;
using Larnix.Core.Utils;
using Larnix.Core.Files;

namespace Larnix.Core
{
    public readonly struct WorldMeta
    {
        // historically metadata was "version<newline>nickname".  New files now
        // use "version:nickname".  Because nicknames cannot contain colons, the new
        // format is unambiguous and parsing is straightforward.  Legacy files are
        // still accepted during read.
        
        private const char LEGACY_SEP = '\n';
        private const char NEW_SEP = ':';

        public Version Version { get; init; }
        public string Nickname { get; init; }

        public static WorldMeta Default => new WorldMeta(
            Version.Current, Common.ReservedNickname
            );

        public WorldMeta(Version version, string nickname)
        {
            Version = version;
            Nickname = nickname;
        }

        private static WorldMeta FromText(string text)
        {
            WorldMeta ParseFormat(string text, char sep)
            {
                string[] parts = text.Split(sep);
                string versionPart = parts.Length > 0 ? parts[0].Trim() : string.Empty;
                string nickname = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                return new WorldMeta(
                    version: new Version(uint.Parse(versionPart)),
                    nickname: nickname
                );
            }

            return ParseFormat(text, text.Contains(NEW_SEP) ?
                NEW_SEP : LEGACY_SEP);
        }

        public static void SaveToWorldFolder(string worldName, WorldMeta mdata)
        {
            string path = Path.Combine(GamePath.SavesPath, worldName);
            SaveToFolder(path, mdata);
        }

        public static WorldMeta ReadFromWorldFolder(string worldName)
        {
            string path = Path.Combine(GamePath.SavesPath, worldName);
            return ReadFromFolder(path);
        }

        public static void SaveToFolder(string path, WorldMeta mdata)
        {
            string data = $"{mdata.Version.ID}{NEW_SEP}{mdata.Nickname}";
            FileManager.Write(path, "metadata.txt", data);
        }

        public static WorldMeta ReadFromFolder(string path)
        {
            string contents = FileManager.Read(path, "metadata.txt");
            if (contents == null)
            {
                return Default;
            }
            
            try
            {
                return FromText(contents);
            }
            catch
            {
                WorldMeta mdata = Default;
                SaveToFolder(path, mdata);
                return mdata;
            }
        }
    }
}
