using System.Collections;
using System.Collections.Generic;
using System.IO;
using Larnix.Core.Utils;
using Larnix.Core.Files;

namespace Larnix.Core
{
    public struct WorldMeta
    {
        public Version Version { get; init; }
        public string Nickname { get; init; }

        public static WorldMeta Default => new WorldMeta(
            Version.Current, Common.LOOPBACK_ONLY_NICKNAME);

        public WorldMeta(Version version, string nickname)
        {
            Version = version;
            Nickname = nickname;
        }

        private static WorldMeta FromText(string text)
        {
            string[] args = text.Split('\n');
            
            return new WorldMeta(
                version: new Version(uint.Parse(args[0])),
                nickname: args[1]
                );
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
            string data = mdata.Version.ID + "\n" + mdata.Nickname;
            FileManager.Write(path, "metadata.txt", data);
        }

        public static WorldMeta ReadFromFolder(string path)
        {
            string contents = FileManager.Read(path, "metadata.txt");
            
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
