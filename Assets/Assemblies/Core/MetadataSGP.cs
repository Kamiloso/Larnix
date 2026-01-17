using System.Collections;
using System.Collections.Generic;
using System.IO;
using Larnix.Core.Utils;
using Larnix.Core.Files;

namespace Larnix.Core
{
    public struct WorldMeta
    {
        public Version version;
        public string nickname;

        public WorldMeta(Version version, string nickname)
        {
            this.version = version;
            this.nickname = nickname;
        }

        public WorldMeta(string text)
        {
            string[] arg = text.Split('\n');
            version = new Version(uint.Parse(arg[0]));
            nickname = arg[1];
        }

        public string GetString()
        {
            return version.ID + "\n" + nickname;
        }

        public static void SaveData(string worldName, WorldMeta mdata, bool fullPath = false)
        {
            string path = fullPath ? worldName : Path.Combine(Common.SavesPath, worldName);
            FileManager.Write(path, "metadata.txt", mdata.GetString());
        }

        public static WorldMeta ReadData(string worldName, bool fullPath = false)
        {
            string path = fullPath ? worldName : Path.Combine(Common.SavesPath, worldName);
            string contents = FileManager.Read(path, "metadata.txt");

            try
            {
                return new WorldMeta(contents);
            }
            catch
            {
                WorldMeta mdata = new WorldMeta(Version.Current, "Player");
                SaveData(path, mdata);
                return mdata;
            }
        }
    }
}
