using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using Larnix.Menu.Worlds;

namespace Larnix.Menu.Forms
{
    public class WorldDeleteForm : BaseForm
    {
        private Menu Menu => Ref.Menu;
        private WorldSelect WorldSelect => Ref.WorldSelect;
        [SerializeField] TMP_InputField IF_DeleteName;

        public override void EnterForm(params string[] args)
        {
            IF_DeleteName.text = args[0];

            TX_ErrorText.text = "";

            Menu.SetScreen("DeleteWorld");
        }

        protected override ErrorCode GetErrorCode()
        {
            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            string delName = IF_DeleteName.text;
            string delDir = Path.Combine(WorldSelect.SavesPath, delName);

            if (Directory.Exists(delDir))
            {
                Directory.Delete(delDir, true);
                WorldSelect.ReloadWorldList();
            }
            
            Menu.SetScreen("Singleplayer");
        }

        public void Cancel()
        {
            Menu.GoBack();
        }
    }
}
