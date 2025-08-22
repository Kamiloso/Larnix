using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu.Worlds;
using Unity.VisualScripting;

namespace Larnix.Menu.Forms
{
    public class WorldRenameForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_OldName;
        [SerializeField] TMP_InputField IF_NewName;

        public override void EnterForm(params string[] args)
        {
            IF_OldName.text = args[0];
            IF_NewName.text = "";

            TX_ErrorText.text = "";

            References.Menu.SetScreen("RenameWorld");
        }

        protected override ErrorCode GetErrorCode()
        {
            if (!Common.IsValidWorldName(IF_NewName.text))
                return ErrorCode.WORLD_NAME_FORMAT;

            string oldDir = Path.Combine(WorldSelect.SavesPath, IF_OldName.text);
            string newDir = Path.Combine(WorldSelect.SavesPath, IF_NewName.text);

            if (DirectoryUtils.AreSameDirectory(oldDir, newDir) || !Directory.Exists(newDir))
                return ErrorCode.SUCCESS;
            else
                return ErrorCode.WORLD_EXISTS;
        }

        protected override void RealSubmit()
        {
            string oldName = IF_OldName.text;
            string newName = IF_NewName.text;
            string tempName = ".temp_world";

            string oldDir = Path.Combine(WorldSelect.SavesPath, oldName);
            string newDir = Path.Combine(WorldSelect.SavesPath, newName);
            string tempDir = Path.Combine(WorldSelect.SavesPath, tempName);

            if (Directory.Exists(oldDir))
            {
                if (!DirectoryUtils.AreSameDirectory(oldDir, newDir)) // casual name change
                {
                    Directory.Move(oldDir, newDir);
                }
                else // something like "Name" -> "name" on Windows
                {
                    Directory.Move(oldDir, tempDir);
                    Directory.Move(tempDir, newDir);
                }

                References.WorldSelect.ReloadWorldList();
                References.WorldSelect.SelectWorld(newName);
            }

            References.Menu.SetScreen("Singleplayer");
        }
    }
}
