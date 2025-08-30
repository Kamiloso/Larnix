using System.Collections;
using TMPro;
using UnityEngine;
using Larnix.Menu.Worlds;
using Larnix.Forms;
using System.Threading.Tasks;
using QuickNet.Frontend;
using QuickNet.Data;

namespace Larnix.Menu.Forms
{
    public class PasswordChangeForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_Address;
        [SerializeField] TMP_InputField IF_Nickname;
        [SerializeField] TMP_InputField IF_Password;
        [SerializeField] TMP_InputField IF_Confirm;

        private ServerThinker thinker = null;
        private InputSwapper swapper = null;

        private string ActionState = null;
        private bool? Result = null;

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
            IF_Nickname.text = args[1];
            IF_Password.text = "";
            IF_Confirm.text = "";

            ChangeState("TYPING_1");

            TX_ErrorText.text = "Your login data will be visible to the server owner.";

            References.Menu.SetScreen("ChangePassword");
        }

        protected override ErrorCode GetErrorCode()
        {
            if (ActionState == "RESULT")
                return ErrorCode.SUCCESS;

            string address = IF_Address.text;
            string nickname = IF_Nickname.text;
            string oldPassword = thinker.serverData.Password;
            string newPassword = IF_Password.text;
            string confirm = IF_Confirm.text;

            if (!Validation.IsGoodNickname(nickname))
                return ErrorCode.NICKNAME_FORMAT;

            if (!Validation.IsGoodPassword(newPassword))
                return ErrorCode.PASSWORD_FORMAT;

            if (oldPassword == newPassword)
                return ErrorCode.PASSWORDS_MATCH;

            if (ActionState == "TYPING_2")
            {
                if (newPassword != confirm)
                    return ErrorCode.PASSWORDS_NOT_MATCH;
            }

            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            string address = IF_Address.text;
            string nickname = IF_Nickname.text;
            string oldPassword = thinker.serverData.Password;
            string newPassword = IF_Password.text;

            if (ActionState == "TYPING_1") // before submit 1
            {
                ChangeState("TYPING_2");
            }
            else if (ActionState == "TYPING_2") // before submit 2
            {
                TX_ErrorText.text = "Changing password...";
                ChangeState("WAITING");
                
                StartCoroutine(ChangePassword(address, nickname, oldPassword, newPassword));
            }
            else if (ActionState == "RESULT")
            {
                if (Result == false)
                    EnterForm(IF_Address.text, IF_Nickname.text);
                else
                    References.Menu.GoBack();
            }
        }

        private IEnumerator ChangePassword(string address, string nickname, string oldPassword, string newPassword)
        {
            string authcode = thinker.serverData.AuthCodeRSA;

            Task<bool?> changeTask = Resolver.TryChangePasswordAsync(address, authcode, nickname, oldPassword, newPassword);
            yield return new WaitUntil(() => changeTask.IsCompleted);

            Result = changeTask.Result;

            if (Result == true)
            {
                TX_ErrorText.text = "Password changed.";
                thinker.SubmitUserOnlyData(nickname, newPassword);
            }
            else if (Result == false)
            {
                TX_ErrorText.text = "Password change failed.";
            }
            else
            {
                TX_ErrorText.text = "Connection error. Cannot check if password change was successful.";
            }

            ChangeState("RESULT");
        }

        private void ChangeState(string state)
        {
            switch (state)
            {
                case "TYPING_1":
                    IF_Confirm.interactable = true;
                    BT_Submit.interactable = true;
                    swapper.SetState(0);
                    break;

                case "TYPING_2":
                    swapper.SetState(1);
                    break;

                case "WAITING":
                    References.Menu.LockScreen();
                    IF_Confirm.interactable = false;
                    BT_Submit.interactable = false;
                    break;

                case "RESULT":
                    References.Menu.UnlockScreen();
                    BT_Submit.interactable = true;
                    break;
            }
            ActionState = state;
        }
    }
}
