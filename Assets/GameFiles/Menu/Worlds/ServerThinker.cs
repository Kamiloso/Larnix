using Larnix.Socket.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Larnix.Socket;
using System.Linq;

namespace Larnix.Menu.Worlds
{
    public class ServerData
    {
        public string FolderName = "";
        public string Address = "";
        public string AuthCodeRSA = "";
        public string Nickname = "";
        public string Password = "";
    }

    public enum ThinkerState
    {
        None,
        Waiting,
        Ready,
        Failed,
        WrongPublicKey,
        Incompatible
    }

    public enum LoginState
    {
        None,
        Ready,
        Waiting,
        Good,
        Bad
    }

    public class ServerThinker : MonoBehaviour
    {
        public ThinkerState State { get; private set; } = ThinkerState.None;
        public ServerData serverData = new(); // input
        public A_ServerInfo serverInfo = null; // output

        Coroutine loginCoroutine = null;
        public bool? LoginSuccess { get; private set; } = null;

        private long PasswordIndex = 0;

        public void SetServerData(ServerData serverData)
        {
            this.serverData = serverData;
        }

        public void SubmitServer(string address, string authCodeRSA)
        {
            serverData.Address = address;
            serverData.AuthCodeRSA = authCodeRSA;
            serverData.Nickname = null;
            serverData.Password = null;

            // to file
            ServerSelect.SaveServerData(serverData);
        }

        public void SubmitUser(string nickname, string password)
        {
            serverData.Nickname = nickname;
            serverData.Password = password;

            Logout();
            Login();

            // to file
            ServerSelect.SaveServerData(serverData);
        }

        private void Login()
        {
            if (State == ThinkerState.Ready)
                loginCoroutine = StartCoroutine(LoginCoroutine());
        }

        public void Logout()
        {
            if (loginCoroutine != null)
            {
                StopCoroutine(loginCoroutine);
                loginCoroutine = null;
            }

            LoginSuccess = null;
        }

        public void SafeRefresh()
        {
            if (State != ThinkerState.Waiting)
                StartCoroutine(RefreshCoroutine());
        }

        public LoginState GetLoginState()
        {
            if (State != ThinkerState.Ready)
                return LoginState.None;

            if (LoginSuccess != null)
                return (bool)LoginSuccess ? LoginState.Good : LoginState.Bad;

            return loginCoroutine == null ? LoginState.Ready : LoginState.Waiting;
        }

        private IEnumerator RefreshCoroutine()
        {
            // set waiting state

            Logout();
            State = ThinkerState.Waiting;
            serverInfo = null;

            // download server info

            string address = serverData.Address;
            string nickname = serverData.Nickname;
            string password = serverData.Password;

            bool knowsUserData = nickname != "" && password != "";

            Task<A_ServerInfo> downloading = new Task<A_ServerInfo>(() =>
            {
                return Resolver.downloadServerInfo(address, knowsUserData ? nickname : "Player");
            });
            downloading.Start();
            while (!downloading.IsCompleted)
                yield return null;

            // ? fail

            if (downloading.Result == null)
            {
                State = ThinkerState.Failed;
                serverInfo = null;
                yield break;
            }

            // check key using authcode

            byte[] key_bytes = downloading.Result.PublicKeyModulus.Concat(downloading.Result.PublicKeyExponent).ToArray();
            string authcode = serverData.AuthCodeRSA;

            Task<bool> checkingKey = new Task<bool>(() =>
            {
                return Server.Data.KeyObtainer.VerifyPublicKey(key_bytes, authcode);
            });
            checkingKey.Start();
            while (!checkingKey.IsCompleted)
                yield return null;

            // ? success / ? wrong public key

            if (checkingKey.Result)
            {
                serverInfo = downloading.Result;
                State = Version.Current.CompatibleWith(new Version(serverInfo.GameVersion)) ? ThinkerState.Ready : ThinkerState.Incompatible;
                
                if (knowsUserData && State != ThinkerState.Incompatible)
                {
                    PasswordIndex = serverInfo.PasswordIndex;
                    Login();
                }
                
                yield break;
            }
            else
            {
                State = ThinkerState.WrongPublicKey;
                serverInfo = null;
                yield break;
            }
        }

        private IEnumerator LoginCoroutine()
        {
            yield return new WaitForSecondsRealtime(1f);

            LoginSuccess = false;
            loginCoroutine = null;
            yield break;
        }
    }
}
