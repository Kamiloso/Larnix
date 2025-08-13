using Larnix.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using Larnix.Modules.Blocks;

namespace Larnix.Menu.Worlds
{
    public class ServerSelect : UniversalSelect
    {
        public static string MultiplayerPath { get => Path.Combine(Application.persistentDataPath, "Multiplayer"); }
        protected override string GetSavesPath() => MultiplayerPath;
        protected override string SortFileName() => "info.txt";

        [SerializeField] Button BT_Join;
        [SerializeField] Button BT_Edit;
        [SerializeField] Button BT_Refresh;
        [SerializeField] Button BT_Remove;

        private void Awake()
        {
            References.ServerSelect = this;
        }

        private void Start()
        {
            ReloadWorldList();
        }

        public void AddServer()
        {
            string addServerName = "Random Server " + Common.Rand().Next();
            string dirCreate = Path.Combine(MultiplayerPath, addServerName);
            if (!Directory.Exists(dirCreate))
                Directory.CreateDirectory(dirCreate);
        }

        public void JoinServer()
        {
            UnityEngine.Debug.LogWarning("Joining servers not implemented yet.");
        }

        public void EditServer()
        {
            UnityEngine.Debug.LogWarning("Editing servers not implemented yet.");

            ReloadWorldList();
        }

        public void RefreshServer()
        {
            UnityEngine.Debug.LogWarning("Refreshing servers not implemented yet.");
        }

        public void RemoveServer()
        {
            string delPath = Path.Combine(MultiplayerPath, SelectedWorld);
            if (Directory.Exists(delPath))
                Directory.Delete(delPath, true);

            ReloadWorldList();
        }

        protected override void OnWorldSelect(string worldName)
        {
            bool enable = worldName != null;

            BT_Join.interactable = enable;
            BT_Refresh.interactable = enable;
            BT_Edit.interactable = enable;
            BT_Remove.interactable = enable;

            if (enable)
            {
                //LoadImageOrClear(Path.Combine(SavesPath, worldName, "last_image.png"), TitleImage);
                //DescriptionText.text = $"Version: {"0.0.0"}\n" +
                //                       $"Difficulty: {"UNKNOWN"}";
            }
            else
            {
                //LoadImageOrClear(null, TitleImage);
                //DescriptionText.text = "";
            }
        }
    }
}
