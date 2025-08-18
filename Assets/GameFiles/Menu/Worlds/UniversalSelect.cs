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
        [SerializeField] protected TextMeshProUGUI NameText;

        public string SelectedWorld { get; private set; } = null;

        public abstract void ReloadWorldList();
        protected abstract void OnWorldSelect(string worldName);

        public void SelectWorld(string worldName)
        {
            List<RectTransform> list = ScrollView.GetAllTransforms();
            foreach (RectTransform rt in list)
            {
                WorldSegment sgm = rt.GetComponent<WorldSegment>();
                if (sgm != null) sgm.SetSelection(worldName);
            }

            SelectedWorld = worldName ?? null;

            OnWorldSelect(worldName);
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
