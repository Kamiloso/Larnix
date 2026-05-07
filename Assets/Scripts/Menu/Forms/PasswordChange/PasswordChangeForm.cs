using System.Collections;
using TMPro;
using UnityEngine;
using Larnix.Menu.Worlds;
using Larnix.Forms;
using System.Threading.Tasks;
using Larnix.Model.Utils;
using Larnix.Core;
using Larnix.Socket.Client;
using Larnix.Socket.Client.Records;
using Larnix.Core.Serialization;

namespace Larnix.Menu.Forms
{
    public class PasswordChangeForm : BaseForm
    {
        private Menu Menu => GlobRef.Get<Menu>();

        [SerializeField] TMP_InputField IF_Address;
        [SerializeField] TMP_InputField IF_Nickname;
        [SerializeField] TMP_InputField IF_Password;
        [SerializeField] TMP_InputField IF_Confirm;

        private ServerThinker _thinker = null;
        private InputSwapper _swapper = null;

        private string _actionState = null;
        private bool? _result = null;

        private void Awake()
        {
            _swapper = GetComponent<InputSwapper>();
        }

        public void ProvideServerThinker(ServerThinker thinker)
        {
            _thinker = thinker;
        }

        public override void EnterForm(params string[] args)
        {
            IF_Address.text = args[0];
            IF_Nickname.text = args[1];
            IF_Password.text = "";
            IF_Confirm.text = "";

            ChangeState("TYPING_1");

            TX_ErrorText.text = "Your login data will be visible to the server owner.";

            Menu.SetScreen("ChangePassword");
        }

        protected override ErrorCode GetErrorCode()
        {
            string nickname = IF_Nickname.text;
            string oldPassword = _thinker.serverData.Password;
            string newPassword = IF_Password.text;
            string confirm = IF_Confirm.text;

            if (_actionState == "RESULT")
                return ErrorCode.SUCCESS;

            if (!Validation.IsGoodNickname(nickname))
                return ErrorCode.NICKNAME_FORMAT;

            if (!Validation.IsGoodPassword(newPassword))
                return ErrorCode.PASSWORD_FORMAT;

            if (_actionState == "TYPING_2")
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
            string oldPassword = _thinker.serverData.Password;
            string newPassword = IF_Password.text;

            if (_actionState == "TYPING_1") // before submit 1
            {
                ChangeState("TYPING_2");
            }
            else if (_actionState == "TYPING_2") // before submit 2
            {
                TX_ErrorText.text = "Changing password...";
                ChangeState("WAITING");

                StartCoroutine(
                    ChangePassword(address, nickname, oldPassword, newPassword)
                    );
            }
            else if (_actionState == "RESULT")
            {
                if (_result == false)
                {
                    EnterForm(IF_Address.text, IF_Nickname.text);
                }
                else
                {
                    Menu.GoBack();
                }
            }
        }

        private IEnumerator ChangePassword(string address, string nickname, string oldPassword, string newPassword)
        {
            string authcode = _thinker.serverData.AuthCodeRSA;

            PasswordChangeData loginData = new(
                Address: address,
                Authcode: authcode,
                Nickname: new FixedString32(nickname),
                Password: new FixedString64(oldPassword),
                NewPassword: new FixedString64(newPassword)
                );

            var passchange = Task.Run(() => Resolver.TryChangePasswordAsync(loginData));

            while (!passchange.IsCompleted)
            {
                yield return null;
            }

            ResolveAnswer<bool> resolved = passchange.Result;
            _result = resolved.Error == ResolveError.None && resolved.Result;

            TX_ErrorText.text = _result switch
            {
                true => "Password changed.",
                false => "Password change failed.",
                null => "Connection error. Cannot check if password change was successful.",
            };

            if (_result == true)
            {
                _thinker.SubmitUserOnlyData(nickname, newPassword);
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
                    _swapper.SetState(0);
                    break;

                case "TYPING_2":
                    _swapper.SetState(1);
                    break;

                case "WAITING":
                    Menu.LockScreen();
                    IF_Confirm.interactable = false;
                    BT_Submit.interactable = false;
                    break;

                case "RESULT":
                    Menu.UnlockScreen();
                    BT_Submit.interactable = true;
                    break;
            }
            _actionState = state;
        }
    }
}
