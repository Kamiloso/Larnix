using Larnix.Socket;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Socket.Commands;
using System.Threading;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

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
        private A_ServerInfo serverInfo = null;

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

        private enum State : byte
        {
            None,
            Waiting,
            Ready,
            Failed,
            WrongPublicKey
        }

        public void SubmitServerData()
        {
            string address = InputAddress.text;
            string authCodeRSA = InputAuthCodeRSA.text;

            Data.Address = address;
            Data.AuthCodeRSA = authCodeRSA;

            References.Menu.SetScreen("Multiplayer");
        }

        public void SubmitPlayerData()
        {
            string nickname = InputNickname.text;
            string password = InputPassword.text;

            Data.Nickname = nickname;
            Data.Password = password;

            References.Menu.SetScreen("Multiplayer");
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
            // download server info

            string address = Data.Address;
            string nickname = Data.Nickname;

            Task<A_ServerInfo> downloading = new Task<A_ServerInfo>(() =>
            {
                return Resolver.downloadServerInfo(address, nickname == "" ? "Player" : nickname);
            });
            downloading.Start();
            while (!downloading.IsCompleted)
                yield return null;

            // ? fail

            if (downloading.Result == null)
            {
                state = State.Failed;
                serverInfo = null;
                yield break;
            }

            // check key using authcode

            byte[] key_bytes = downloading.Result.PublicKeyModulus.Concat(downloading.Result.PublicKeyExponent).ToArray();
            string authcode = Data.AuthCodeRSA;

            Task<bool> checkingKey = new Task<bool>(() =>
            {
                return Server.Data.KeyObtainer.VerifyPublicKey(key_bytes, authcode);
            });
            checkingKey.Start();
            while (!checkingKey.IsCompleted)
                yield return null;

            // ? success / ? wrong public key

            if(checkingKey.Result)
            {
                state = State.Ready;
                serverInfo = downloading.Result;
                yield break;
            }
            else
            {
                state = State.WrongPublicKey;
                serverInfo = null;
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

            if (state == State.None)
            {
                FieldDescription.text = "EMPTY: No server here.";
                FieldPlayerNumber.text = "?? / ??";
                ButtonJoin.interactable = false;
            }
            else if (state == State.Waiting)
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
            else if (state == State.Failed)
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
