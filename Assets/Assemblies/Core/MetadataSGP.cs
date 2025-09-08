using QuickNet;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Larnix.Core
{
    public struct MetadataSGP
    {
        public Version version;
        public string nickname;

        public MetadataSGP(Version version, string nickname)
        {
            this.version = version;
            this.nickname = nickname;
        }

        public MetadataSGP(string text)
        {
            string[] arg = text.Split('\n');
            version = new Version(uint.Parse(arg[0]));
            nickname = arg[1];
        }

        public string GetString()
        {
            return version.ID + "\n" + nickname;
        }

        public static void SaveMetadataSGP(string worldName, MetadataSGP metadataSGP, bool fullPath = false)
        {
            string path = fullPath ? worldName : Path.Combine(Common.SavesPath, worldName);
            FileManager.Write(path, "metadata.txt", metadataSGP.GetString());
        }

        public static MetadataSGP ReadMetadataSGP(string worldName, bool fullPath = false)
        {
            string path = fullPath ? worldName : Path.Combine(Common.SavesPath, worldName);
            string contents = FileManager.Read(path, "metadata.txt");

            try
            {
                return new MetadataSGP(contents);
            }
            catch
            {
                MetadataSGP mdata = new MetadataSGP(Version.Current, "Player");
                SaveMetadataSGP(path, mdata);
                return mdata;
            }
        }
    }
}
