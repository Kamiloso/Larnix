using Larnix.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using TMPro;
using Larnix.Menu.Forms;
using Larnix.Core;
using Version = Larnix.Core.Version;
using Org.BouncyCastle.Asn1.X509;

namespace Larnix.Menu.Worlds
{
    public class WorldSelect : UniversalSelect
    {
        public static string SavesPath => GamePath.SavesPath;

        [SerializeField] Image TitleImage;
        [SerializeField] TextMeshProUGUI DescriptionText;

        [SerializeField] Button BT_Play;
        [SerializeField] Button BT_Host;
        [SerializeField] Button BT_Rename;
        [SerializeField] Button BT_Delete;

        private readonly Dictionary<string, WorldMeta> MetadatasSGP = new();

        private void Awake()
        {
            Ref.WorldSelect = this;
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

        public static void PlayWorldByName(string name, long? seedSuggestion = null)
        {
            WorldMeta mdata = WorldMeta.ReadFromWorldFolder(name);

            if(mdata.Nickname != "Player")
                Settings.Settings.Instance.SetValue("$last-nickname-SGP", mdata.Nickname, true);

            WorldLoad.StartLocal(name, mdata.Nickname, seedSuggestion);
        }

        public static void HostAndPlayWorldByName(string name, (string address, string authcode) serverTuple)
        {
            WorldMeta mdata = WorldMeta.ReadFromWorldFolder(name);

            if (mdata.Nickname != "Player")
                Settings.Settings.Instance.SetValue("$last-nickname-SGP", mdata.Nickname, true);

            WorldLoad.StartHost(name, mdata.Nickname, serverTuple);
        }

        public void HostWorld()
        {
            WorldMeta mdata = WorldMeta.ReadFromWorldFolder(SelectedWorld);
            BaseForm.GetInstance<WorldHostForm>().EnterForm(SelectedWorld, mdata.Nickname);
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
                    throw new InvalidOperationException("Prefab should be of type RectTransform!");

                string worldName = WorldPathToName(worldPath);

                rt.name = $"WorldSegment: \"{worldName}\"";
                rt.GetComponent<WorldSegment>().Init(worldName, this);
                ScrollView.BottomAddElement(rt);

                MetadatasSGP[worldName] = WorldMeta.ReadFromWorldFolder(worldPath);
            }
        }

        protected override void OnWorldSelect(string worldName)
        {
            NameText.text = worldName ?? "";

            bool enable = worldName != null;
            bool compatible = enable ? MetadatasSGP[worldName].Version <= Version.Current : false;

            BT_Play.interactable = enable && compatible;
            BT_Host.interactable = enable && compatible;
            BT_Rename.interactable = enable;
            BT_Delete.interactable = enable;

            if (enable)
            {
                string versionDisplay = MetadatasSGP[worldName].Version.ToString();
                string playerDisplay = MetadatasSGP[worldName].Nickname;

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
                    CleanSprite(targetImage.sprite);
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
                CleanSprite(targetImage.sprite);
                targetImage.sprite = blackSprite;
            }
        }

        private static void CleanSprite(Sprite sprite)
        {
            if (sprite != null)
            {
                Texture2D tex = sprite.texture;
                UnityEngine.Object.Destroy(sprite);
                UnityEngine.Object.Destroy(tex);
            }
        }

        private void OnDestroy()
        {
            CleanSprite(TitleImage.sprite);
        }
    }
}
