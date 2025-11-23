using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Menu.Worlds;
using Larnix.Forms;
using Socket;

namespace Larnix.Menu.Forms
{
    public class ServerRegisterForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_Address;
        [SerializeField] TMP_InputField IF_Nickname;
        [SerializeField] TMP_InputField IF_Password;
        [SerializeField] TMP_InputField IF_Confirm;

        private ServerThinker thinker = null;
        private InputSwapper swapper = null;

        private void Awake()
        {
            swapper = GetComponent<InputSwapper>();
        }

        public void ProvideServerThinker(ServerThinker thinker)
        {
            this.thinker = thinker;
        }

        public override void EnterForm(params string[] args)
        {
            IF_Address.text = args[0];
            IF_Nickname.text = "";
            IF_Password.text = "";
            IF_Confirm.text = "";

            swapper.SetState(0);
            IF_Nickname.interactable = true;

            TX_ErrorText.text = "Your login data will be visible to the server owner.";

            References.Menu.SetScreen("Register");
        }

        protected override ErrorCode GetErrorCode()
        {
            string address = IF_Address.text;
            string nickname = IF_Nickname.text;
            string password = IF_Password.text;
            string confirm = IF_Confirm.text;

            if (!Validation.IsGoodNickname(nickname))
                return ErrorCode.NICKNAME_FORMAT;

            if (nickname == "Player")
                return ErrorCode.NICKNAME_IS_PLAYER;

            if (!Validation.IsGoodPassword(password))
                return ErrorCode.PASSWORD_FORMAT;

            if (swapper.State == 1)
            {
                if (password != confirm)
                    return ErrorCode.PASSWORDS_NOT_MATCH;
            }

            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            if (swapper.State == 0) // before submit 1
            {
                swapper.SetState(1);
                IF_Nickname.interactable = false;
            }
            else if (swapper.State == 1) // before submit 2
            {
                string nickname = IF_Nickname.text;
                string password = IF_Password.text;

                thinker.SubmitUser(nickname, password, true);
                References.Menu.GoBack();
            }
        }
    }
}
