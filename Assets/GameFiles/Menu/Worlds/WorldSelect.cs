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
        protected override string GetSavesPath() => SavesPath;
        protected override string SortFileName() => "database.sqlite";

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

        protected override void OnWorldSelect(string worldName)
        {
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
    }
}
