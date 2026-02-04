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
    public class WorldDeleteForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_DeleteName;

        public override void EnterForm(params string[] args)
        {
            IF_DeleteName.text = args[0];

            TX_ErrorText.text = "";

            Ref.Menu.SetScreen("DeleteWorld");
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
                Ref.WorldSelect.ReloadWorldList();
            }
            
            Ref.Menu.SetScreen("Singleplayer");
        }

        public void Cancel()
        {
            Ref.Menu.GoBack();
        }
    }
}
