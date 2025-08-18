using Larnix.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace Larnix.Menu.Worlds
{
    public class WorldSelect : UniversalSelect
    {
        public static string SavesPath { get => Path.Combine(Application.persistentDataPath, "Saves"); }

        [SerializeField] Image TitleImage;
        [SerializeField] TextMeshProUGUI DescriptionText;

        [SerializeField] Button BT_Play;
        [SerializeField] Button BT_Host;
        [SerializeField] Button BT_Rename;
        [SerializeField] Button BT_Delete;

        private void Awake()
        {
            References.WorldSelect = this;
        }

        private void Start()
        {
            ReloadWorldList();
        }

        public void CreateWorld()
        {
            string newWorldName = "Random World " + Common.Rand().Next();
            WorldLoad.StartLocal(newWorldName);
        }

        public void PlayWorld()
        {
            WorldLoad.StartLocal(SelectedWorld);
        }

        public void HostWorld()
        {
            UnityEngine.Debug.LogWarning("Hosting worlds not implemented yet.");
        }

        public void RenameWorld()
        {
            UnityEngine.Debug.LogWarning("Renaming worlds not implemented yet.");

            ReloadWorldList();
        }

        public void DeleteWorld()
        {
            string delPath = Path.Combine(SavesPath, SelectedWorld);
            if (Directory.Exists(delPath))
                Directory.Delete(delPath, true);

            ReloadWorldList();
        }

        public override void ReloadWorldList()
        {
            SelectWorld(null);
            ScrollView.ClearAll();

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
                ScrollView.EntryPushElement(rt);
            }
        }

        protected override void OnWorldSelect(string worldName)
        {
            NameText.text = worldName ?? "";

            bool enable = worldName != null;
            BT_Play.interactable = enable;
            BT_Host.interactable = enable;
            BT_Rename.interactable = enable;
            BT_Delete.interactable = enable;

            if (enable)
            {
                LoadImageOrClear(Path.Combine(SavesPath, worldName, "last_image.png"), TitleImage);
                DescriptionText.text = $"Version: {"0.0.0"}\n" +
                                       $"Difficulty: {"UNKNOWN"}";
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
    }
}
