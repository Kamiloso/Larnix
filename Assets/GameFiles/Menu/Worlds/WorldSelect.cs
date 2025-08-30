using Larnix.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using Larnix.Menu.Forms;
using QuickNet;

namespace Larnix.Menu.Worlds
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
    }

    public class WorldSelect : UniversalSelect
    {
        public static string SavesPath { get => Path.Combine(Application.persistentDataPath, "Saves"); }

        [SerializeField] Image TitleImage;
        [SerializeField] TextMeshProUGUI DescriptionText;

        [SerializeField] Button BT_Play;
        [SerializeField] Button BT_Host;
        [SerializeField] Button BT_Rename;
        [SerializeField] Button BT_Delete;

        private readonly Dictionary<string, MetadataSGP> MetadatasSGP = new();

        private void Awake()
        {
            References.WorldSelect = this;
            ReloadWorldList();
        }

        public void CreateWorld()
        {
            BaseForm.GetInstance<WorldCreateForm>().EnterForm();
        }

        public void PlayWorld()
        {
            PlayWorldByName(SelectedWorld);
        }

        public static void PlayWorldByName(string name)
        {
            MetadataSGP mdata = ReadMetadataSGP(name);
            WorldLoad.StartLocal(name, mdata.nickname);

            if(mdata.nickname != "Player")
                Settings.Settings.Instance.SetValue("$last-nickname-SGP", mdata.nickname, true);
        }

        public void HostWorld()
        {
            MetadataSGP mdata = ReadMetadataSGP(SelectedWorld);
            BaseForm.GetInstance<WorldHostForm>().EnterForm(SelectedWorld, mdata.nickname);
        }

        public void RenameWorld()
        {
            BaseForm.GetInstance<WorldRenameForm>().EnterForm(SelectedWorld);
        }

        public void DeleteWorld()
        {
            BaseForm.GetInstance<WorldDeleteForm>().EnterForm(SelectedWorld);
        }

        public override void ReloadWorldList()
        {
            SelectWorld(null);
            ScrollView.ClearAll();
            MetadatasSGP.Clear();

            // World Segments
            List<string> availableWorldPaths = GetSortedWorldPaths(SavesPath, "database.sqlite");
            foreach (string worldPath in availableWorldPaths)
            {
                RectTransform rt = Instantiate(WorldSegmentPrefab).transform as RectTransform;
                if (rt == null)
                    throw new System.InvalidOperationException("Prefab should be of type RectTransform!");

                string worldName = WorldPathToName(worldPath);

                rt.name = $"WorldSegment: \"{worldName}\"";
                rt.GetComponent<WorldSegment>().Init(worldName, this);
                ScrollView.BottomAddElement(rt);

                MetadatasSGP[worldName] = ReadMetadataSGP(worldPath);
            }
        }

        protected override void OnWorldSelect(string worldName)
        {
            NameText.text = worldName ?? "";

            bool enable = worldName != null;
            bool compatible = enable ? MetadatasSGP[worldName].version <= Version.Current : false;

            BT_Play.interactable = enable && compatible;
            BT_Host.interactable = enable;
            BT_Rename.interactable = enable;
            BT_Delete.interactable = enable;

            if (enable)
            {
                string versionDisplay = MetadatasSGP[worldName].version.ToString();
                string playerDisplay = MetadatasSGP[worldName].nickname;

                LoadImageOrClear(Path.Combine(SavesPath, worldName, "last_image.png"), TitleImage);
                string description = $"Version: {versionDisplay}[REPLACE]\n" +
                                     (playerDisplay != "Player" ? $"Player: {playerDisplay}" : "Detached World");

                DescriptionText.text = description.Replace("[REPLACE]", compatible ? "" : " - Incompatible");
            }
            else
            {
                LoadImageOrClear(null, TitleImage);
                DescriptionText.text = "";
            }
        }

        private static void LoadImageOrClear(string path, Image targetImage)
        {
            bool success = false;

            if (File.Exists(path))
            {
                byte[] imageData = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);

                if (tex.LoadImage(imageData))
                {
                    Rect rect = new Rect(0, 0, tex.width, tex.height);
                    Vector2 pivot = new Vector2(0.5f, 0.5f);
                    Sprite sprite = Sprite.Create(tex, rect, pivot);
                    targetImage.sprite = sprite;
                    success = true;
                }
            }

            if (!success)
            {
                Texture2D blackTex = new Texture2D(1, 1);
                blackTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
                blackTex.Apply();

                Rect rect = new Rect(0, 0, 1, 1);
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                Sprite blackSprite = Sprite.Create(blackTex, rect, pivot);
                targetImage.sprite = blackSprite;
            }
        }

        public static void SaveMetadataSGP(string worldName, MetadataSGP metadataSGP, bool fullPath = false)
        {
            string path = fullPath ? worldName : Path.Combine(SavesPath, worldName);
            FileManager.Write(path, "metadata.txt", metadataSGP.GetString());
        }

        public static MetadataSGP ReadMetadataSGP(string worldName, bool fullPath = false)
        {
            string path = fullPath ? worldName : Path.Combine(SavesPath, worldName);
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
