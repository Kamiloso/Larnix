using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu.Worlds;
using Larnix.Server.Data;
using System;
using Larnix.Socket.Commands;

namespace Larnix.Menu.Forms
{
    public class ServerRegisterForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_Address;
        [SerializeField] TMP_InputField IF_Nickname;
        [SerializeField] TMP_InputField IF_Password;

        private ServerThinker thinker = null;

        public void ProvideServerThinker(ServerThinker thinker)
        {
            this.thinker = thinker;
        }

        public override void EnterForm(params string[] args)
        {
            IF_Address.text = args[0];
            IF_Nickname.text = "";
            IF_Password.text = "";

            TX_ErrorText.text = "Your login data will be visible to the server owner.";

            References.Menu.SetScreen("Register");
        }

        protected override ErrorCode GetErrorCode()
        {
            string address = IF_Address.text;
            string nickname = IF_Nickname.text;
            string password = IF_Password.text;

            if (!Common.IsGoodNickname(nickname))
                return ErrorCode.NICKNAME_FORMAT;

            if (!Common.IsGoodPassword(password))
                return ErrorCode.PASSWORD_FORMAT;

            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            string nickname = IF_Nickname.text;
            string password = IF_Password.text;

            thinker.SubmitUser(nickname, password, true);
            References.Menu.GoBack();
        }
    }
}
