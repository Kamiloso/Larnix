using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu.Worlds;

namespace Larnix.Menu.Forms
{
    public abstract class BaseForm : MonoBehaviour
    {
        [SerializeField] protected Button BT_Submit;
        [SerializeField] protected TextMeshProUGUI TX_ErrorText;

        protected enum ErrorCode
        {
            SUCCESS = 0,
            WORLD_NAME_FORMAT = 1,
            WORLD_EXISTS = 2,
            NICKNAME_FORMAT = 3,
            NICKNAME_IS_PLAYER = 4,
            PASSWORD_FORMAT = 5,
            ADDRESS_EXISTS = 6,
            AUTHCODE_FORMAT = 7,
            ADDRESS_EMPTY = 8,
            PASSWORDS_NOT_MATCH = 9,
            PASSWORDS_MATCH = 10,
            AUTHCODE_EXISTS = 11,
        }

        protected static string GetErrorInfo(ErrorCode code) => code switch
        {
            ErrorCode.SUCCESS => null,
            ErrorCode.WORLD_NAME_FORMAT => "World name should be 1–32 characters, be already trimmed and only use: letters, digits, space, _ or -.",
            ErrorCode.WORLD_EXISTS => "World with such name already exists.",
            ErrorCode.NICKNAME_FORMAT => "Nickname should be 3-16 characters and only use: letters, digits, _ or -.",
            ErrorCode.NICKNAME_IS_PLAYER => "Nickname \"Player\" is reserved.",
            ErrorCode.PASSWORD_FORMAT => "Password should be 7-32 characters and not end with NULL (0x00).",
            ErrorCode.ADDRESS_EXISTS => "This address already exists in the server list.",
            ErrorCode.AUTHCODE_FORMAT => "It is not a correct authcode.",
            ErrorCode.ADDRESS_EMPTY => "Server address cannot be empty.",
            ErrorCode.PASSWORDS_NOT_MATCH => "Passwords must match.",
            ErrorCode.PASSWORDS_MATCH => "New password cannot be the same as the old password.",
            ErrorCode.AUTHCODE_EXISTS => "This authcode already exists in the server list.",
            _ => "Unknown error.",
        };

        public void Submit()
        {
            ErrorCode code = GetErrorCode();
            string errorInfo = GetErrorInfo(code);
            if (errorInfo != null)
                TX_ErrorText.text = errorInfo;

            if (code == 0) // everything ok
            {
                RealSubmit();
            }
        }

        public static T GetInstance<T>() where T : BaseForm
        {
            T found = FindAnyObjectByType<T>();
            if (found == null)
                throw new System.NotImplementedException("Cannot find object of type " + typeof(T).ToString());
            return found;
        }

        public abstract void EnterForm(params string[] args);
        protected abstract ErrorCode GetErrorCode();
        protected abstract void RealSubmit();
    }
}
