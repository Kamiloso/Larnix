using Larnix.Socket;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Socket.Commands;
using System.Threading;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace Larnix.Menu
{
    public class ServerContainer : MonoBehaviour
    {
        // Don't change this field while State.Waiting
        private readonly ServerInfo Data = new ServerInfo();
        
        [System.Serializable]
        public class ServerInfo
        {
            public string Address = "";
            public string AuthCodeRSA = "";
            public string Nickname = "";
            public string Password = "";
            public byte PasswordIndex = 0;
        }

        // Threaded data
        private State state = State.None;
        private volatile A_ServerInfo serverInfo = null;

        // UI components
        [SerializeField] TextMeshProUGUI FieldAddress;
        [SerializeField] TextMeshProUGUI FieldDescription;
        [SerializeField] TextMeshProUGUI FieldPlayerNumber;
        [SerializeField] Button ButtonJoin;

        [SerializeField] TMP_InputField InputAddress;
        [SerializeField] TMP_InputField InputAuthCodeRSA;
        [SerializeField] Button ButtonSubmitServer;

        [SerializeField] TMP_InputField InputNickname;
        [SerializeField] TMP_InputField InputPassword;
        [SerializeField] TMP_InputField InputConfirmPassword;
        [SerializeField] Button ButtonLogin;
        [SerializeField] Button ButtonRegister;

        //[SerializeField] InputField InputOldPassword;
        //[SerializeField] InputField InputNewPassword;
        //[SerializeField] InputField InputConfirmNewPassword;
        //[SerializeField] Button ButtonChangePassword;

        // References
        [SerializeField] ScreenManager ScreenManager;

        private enum State : byte
        {
            None,
            Waiting,
            Ready,
            Failied,
            WrongPublicKey
        }

        public void SubmitServerData()
        {
            string address = InputAddress.text;
            string authCodeRSA = InputAuthCodeRSA.text;

            Data.Address = address;
            Data.AuthCodeRSA = authCodeRSA;

            ScreenManager.SetScreen("Multiplayer");
        }

        public void SubmitPlayerData()
        {
            string nickname = InputNickname.text;
            string password = InputPassword.text;

            Data.Nickname = nickname;
            Data.Password = password;

            ScreenManager.SetScreen("Multiplayer");
        }

        public void RefreshInfo()
        {
            if(state != State.Waiting)
            {
                state = State.Waiting;
                serverInfo = null;

                StartCoroutine(RefreshInfoCoroutine());
            }
        }

        public IEnumerator RefreshInfoCoroutine()
        {
            Thread thread = new Thread(() =>
            {
                serverInfo = Resolver.downloadServerInfo(Data.Address, Data.Nickname == "" ? "Player" : Data.Nickname);
            });
            thread.Start();

            while (thread.IsAlive)
                yield return null;

            if (serverInfo == null)
            {
                state = State.Failied;
                yield break;
            }

            byte[] key_bytes = serverInfo.PublicKeyModulus.Concat(serverInfo.PublicKeyExponent).ToArray();
            if (Server.Data.KeyObtainer.VerifyPublicKey(key_bytes, Data.AuthCodeRSA))
            {
                state = State.Ready;
                yield break;
            }
            else
            {
                state = State.WrongPublicKey;
                yield break;
            }
        }

        public void Join()
        {
            if (state != State.Ready)
                return;

            byte[] modulus = serverInfo.PublicKeyModulus;
            byte[] exponent = serverInfo.PublicKeyExponent;
            byte[] public_key = modulus.Concat(exponent).ToArray();

            WorldLoad.StartRemote(Data.Address, Data.Nickname, Data.Password, public_key);
        }

        private void Update()
        {
            // Update UI components
            FieldAddress.text = Data.Address;

            if (state == State.None || state == State.Waiting)
            {
                FieldDescription.text = "Downloading info...";
                FieldPlayerNumber.text = "?? / ??";
                ButtonJoin.interactable = false;
            }
            else if (state == State.Ready)
            {
                FieldDescription.text = serverInfo.Motd;
                FieldPlayerNumber.text = serverInfo.CurrentPlayers + " / " + serverInfo.MaxPlayers;
                ButtonJoin.interactable = true;
            }
            else if (state == State.Failied)
            {
                FieldDescription.text = "ERROR: Server not found.";
                FieldPlayerNumber.text = "ERROR";
                ButtonJoin.interactable = false;
            }
            else if (state == State.WrongPublicKey)
            {
                FieldDescription.text = "ERROR: Public key hashes don't match.";
                FieldPlayerNumber.text = "ERROR";
                ButtonJoin.interactable = false;
            }
        }
    }
}
