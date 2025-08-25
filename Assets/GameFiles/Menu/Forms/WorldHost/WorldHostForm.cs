using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu.Worlds;

namespace Larnix.Menu.Forms
{
    public class WorldHostForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_WorldName;
        [SerializeField] TMP_InputField OF_ServerAddress;
        [SerializeField] TMP_InputField OF_Authcode;
        [SerializeField] TMP_InputField OF_RelayAddress;

        private string nickname = null;

        public override void EnterForm(params string[] args)
        {
            IF_WorldName.text = args[0];
            nickname = args[1];
            OF_ServerAddress.text = "???";
            OF_Authcode.text = "???";
            OF_RelayAddress.text = "@germany";
            BT_Submit.interactable = false;

            TX_ErrorText.text = "Starting the server...";

            References.Menu.SetScreen("HostWorld");
        }

        protected override ErrorCode GetErrorCode()
        {
            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            string worldName = IF_WorldName.text;
            WorldLoad.StartLocal(worldName, nickname);
        }
    }
}
