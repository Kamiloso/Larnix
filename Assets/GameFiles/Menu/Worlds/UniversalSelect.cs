using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.UI;
using System;
using System.Linq;

namespace Larnix.Menu.Worlds
{
    public abstract class UniversalSelect : MonoBehaviour
    {
        [SerializeField] protected GameObject WorldSegmentPrefab;

        [SerializeField] protected ScrollView ScrollView;
        [SerializeField] protected Image TitleImage;

        [SerializeField] protected TextMeshProUGUI NameText;
        [SerializeField] protected TextMeshProUGUI DescriptionText;

        public string SelectedWorld { get; private set; } = null;

        protected abstract void OnWorldSelect(string worldName);
        protected abstract string GetSavesPath();
        protected abstract string SortFileName();

        public void ReloadWorldList()
        {
            SelectWorld(null);
            ScrollView.ClearAll();

            // World Segments
            List<string> availableWorldPaths = GetSortedWorldPaths(GetSavesPath(), SortFileName());
            foreach (string worldPath in availableWorldPaths)
            {
                RectTransform rt = Instantiate(WorldSegmentPrefab).transform as RectTransform;
                if (rt == null)
                    throw new System.InvalidOperationException("Prefab should be of type RectTransform!");

                string worldName = WorldPathToName(worldPath);

                rt.name = $"WorldSegment: \"{worldName}\"";
                rt.GetComponent<WorldSegment>().Init(worldName, this);
                ScrollView.PushElement(rt);
            }
        }

        public void SelectWorld(string worldName)
        {
            List<RectTransform> list = ScrollView.GetAllTransforms();
            foreach (RectTransform rt in list)
            {
                WorldSegment sgm = rt.GetComponent<WorldSegment>();
                if (sgm != null) sgm.SetSelection(worldName);
            }

            SelectedWorld = worldName ?? null;
            NameText.text = worldName ?? "";

            OnWorldSelect(worldName);
        }

        public static void LoadImageOrClear(string path, Image targetImage)
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

        protected static List<string> GetSortedWorldPaths(string parentFolderPath, string sortByFile)
        {
            if (!Directory.Exists(parentFolderPath))
                Directory.CreateDirectory(parentFolderPath);

            string[] folders = Directory.GetDirectories(parentFolderPath);

            var foldersWithDate = folders
                .Select(folderPath =>
                {
                    string sortFile = Path.Combine(folderPath, sortByFile);
                    DateTime? modificationDate = null;

                    if (File.Exists(sortFile))
                    {
                        modificationDate = File.GetLastWriteTime(sortFile);
                    }

                    return new { folderPath, modificationDate };
                });

            var sortedFolders = foldersWithDate
                .OrderByDescending(x => x.modificationDate ?? DateTime.MinValue)
                .Select(x => x.folderPath)
                .ToList();

            return sortedFolders;
        }

        protected static string WorldPathToName(string worldPath)
        {
            return Path.GetFileName(worldPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
    }
}
