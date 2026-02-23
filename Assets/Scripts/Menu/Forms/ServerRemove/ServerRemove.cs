using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Menu.Worlds;
using Larnix.Core;

namespace Larnix.Menu.Forms
{
    public class ServerRemoveForm : BaseForm
    {
        private Menu Menu => GlobRef.Get<Menu>();
        private WorldSelect WorldSelect => GlobRef.Get<WorldSelect>();
        [SerializeField] TMP_InputField IF_RemoveAddress;

        private ServerSelect ServerSelect => GlobRef.Get<ServerSelect>();

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
            ServerSelect.TrueRemoveServer();
            Menu.SetScreen("Multiplayer");
        }

        public void Cancel()
        {
            Menu.GoBack();
        }
    }
}
