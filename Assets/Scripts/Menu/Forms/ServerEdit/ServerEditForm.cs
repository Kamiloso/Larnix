using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Menu.Worlds;
using System;
using Larnix.Socket.Security;
using Larnix.Core;

namespace Larnix.Menu.Forms
{
    public class ServerEditForm : BaseForm
    {
        private Menu Menu => GlobRef.Get<Menu>();
        private ServerSelect ServerSelect => GlobRef.Get<ServerSelect>();

        [SerializeField] TextMeshProUGUI Title;
        [SerializeField] TMP_InputField IF_Address;
        [SerializeField] TMP_InputField IF_Authcode;

        private string[] args = null;

        public override void EnterForm(params string[] args)
        {
            if (args[0] == "ADD")
            {
                Title.text = "ADD SERVER";
                IF_Address.text = "";
                IF_Authcode.text = "";
            }
            else if (args[0] == "EDIT")
            {
                Title.text = "EDIT SERVER";
                IF_Address.text = args[1];
                IF_Authcode.text = args[2];
            }
            else throw new NotImplementedException("Unknown arg[0]: \"" + args[0] + "\" (can be ADD or EDIT)");

            this.args = args;
            TX_ErrorText.text = "";

            Menu.SetScreen("ServerEdit");
        }

        protected override ErrorCode GetErrorCode()
        {
            string address = IF_Address.text;
            string authcode = IF_Authcode.text;

            if (address == "")
                return ErrorCode.ADDRESS_EMPTY;

            if ((args[0] == "ADD" || (args[0] == "EDIT" && args[1] != address)) && ServerSelect.ContainsAddress(address))
                return ErrorCode.ADDRESS_EXISTS;

            if (!Authcode.IsGoodAuthcode(authcode))
                return ErrorCode.AUTHCODE_FORMAT;

            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            string address = IF_Address.text;
            string authcode = IF_Authcode.text;

            if (args[0] == "ADD")
            {
                ServerData serverData = new ServerData
                {
                    FolderName = Guid.NewGuid().ToString("N"),
                    Address = address,
                    AuthCodeRSA = authcode,
                    Nickname = "",
                    Password = "",
                };

                ServerSelect.AddServerSegment(address, serverData, true);
                ServerSelect.SelectWorld(address, true);
                ServerSelect.SaveServerData(serverData);
            }

            if (args[0] == "EDIT")
            {
                ServerSelect.EditSegment(args[1], address, authcode);
            }

            Menu.GoBack();
        }
    }
}
