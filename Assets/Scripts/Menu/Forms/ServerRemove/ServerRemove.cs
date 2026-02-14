using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using Larnix.Menu.Worlds;

namespace Larnix.Menu.Forms
{
    public class ServerRemoveForm : BaseForm
    {
        private Menu Menu => Ref.Menu;
        private WorldSelect WorldSelect => Ref.WorldSelect;
        [SerializeField] TMP_InputField IF_RemoveAddress;

        public override void EnterForm(params string[] args)
        {
            IF_RemoveAddress.text = args[0];

            TX_ErrorText.text = "";

            Menu.SetScreen("RemoveServer");
        }

        protected override ErrorCode GetErrorCode()
        {
            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            Ref.ServerSelect.TrueRemoveServer();
            Menu.SetScreen("Multiplayer");
        }

        public void Cancel()
        {
            Menu.GoBack();
        }
    }
}
