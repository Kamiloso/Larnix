using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Menu.Worlds;
using QuickNet.Data;

namespace Larnix.Menu.Forms
{
    public class ServerLoginForm : BaseForm
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
            IF_Nickname.text = args[1];
            IF_Password.text = args[2];

            TX_ErrorText.text = "Your login data will be visible to the server owner.";

            References.Menu.SetScreen("Login");
        }

        protected override ErrorCode GetErrorCode()
        {
            string address = IF_Address.text;
            string nickname = IF_Nickname.text;
            string password = IF_Password.text;

            if (!Validation.IsGoodNickname(nickname))
                return ErrorCode.NICKNAME_FORMAT;

            if (!Validation.IsGoodPassword(password))
                return ErrorCode.PASSWORD_FORMAT;

            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            string nickname = IF_Nickname.text;
            string password = IF_Password.text;

            thinker.SubmitUser(nickname, password, false);
            References.Menu.GoBack();
        }
    }
}
