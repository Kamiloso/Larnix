using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Socket.Frontend;
using Socket.Channel.Cmds;
using Version = Larnix.Core.Version;

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
        public bool WasRegistration { get; private set; } = false;

        private string oldNickname = null;
        private string oldPassword = null;

        public void SetServerData(ServerData serverData)
        {
            this.serverData = serverData;
            SafeRefresh();
        }

        public void SubmitServer(string address, string authCodeRSA)
        {
            if (serverData.AuthCodeRSA != authCodeRSA) // reset user data when changing authcode (for security)
            {
                serverData.Nickname = "";
                serverData.Password = "";
            }

            serverData.Address = address;
            serverData.AuthCodeRSA = authCodeRSA;

            // to file
            ServerSelect.SaveServerData(serverData);

            SafeRefresh();
        }

        public void SubmitUser(string nickname, string password, bool isRegistration)
        {
            oldNickname = serverData.Nickname;
            oldPassword = serverData.Password;

            serverData.Nickname = nickname;
            serverData.Password = password;

            Logout();
            Login(isRegistration);

            if (!isRegistration)
            {
                // to file
                ServerSelect.SaveServerData(serverData);
            }
        }

        internal void SubmitUserOnlyData(string nickname, string password)
        {
            serverData.Nickname = nickname;
            serverData.Password = password;

            ServerSelect.SaveServerData(serverData);
        }

        private void Login(bool isRegistration)
        {
            if (State == ThinkerState.Ready)
                loginCoroutine = StartCoroutine(LoginCoroutine(isRegistration));
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

            // download server info

            string address = serverData.Address;
            string authcode = serverData.AuthCodeRSA;
            string nickname = serverData.Nickname;
            string password = serverData.Password;

            bool knowsUserData = nickname != "" && password != "";

            var downloading = Task.Run(() =>
                Resolver.DownloadServerInfoAsync(address, authcode, knowsUserData ? nickname : "Player", true));

            while (!downloading.IsCompleted)
                yield return null;

            if (downloading.Result.info == null)
            {
                if (downloading.Result.error == ResolverError.PublicKeyInvalid) // public key problems fail
                {
                    serverInfo = null;
                    State = ThinkerState.WrongPublicKey;
                    yield break;
                }
                else // classic fail
                {
                    serverInfo = null;
                    State = ThinkerState.Failed;
                    yield break;
                }
            }
            else // success
            {
                serverInfo = downloading.Result.info;
                State = Version.Current.CompatibleWith(new Version(serverInfo.GameVersion)) ? ThinkerState.Ready : ThinkerState.Incompatible;

                if (knowsUserData && State != ThinkerState.Incompatible)
                {
                    Login(false);
                }
                yield break;
            }
        }

        private IEnumerator LoginCoroutine(bool isRegistration)
        {
            string address = serverData.Address;
            string authcode = serverData.AuthCodeRSA;
            string nickname = serverData.Nickname;
            string password = serverData.Password;

            WasRegistration = isRegistration;

            var login = isRegistration ?
                Task.Run(() => Resolver.TryRegisterAsync(address, authcode, nickname, password)) :
                Task.Run(() => Resolver.TryLoginAsync(address, authcode, nickname, password));

            while (!login.IsCompleted)
                yield return null;

            if (login.Result.success == true)
            {
                if (isRegistration) // apply data
                {
                    ServerSelect.SaveServerData(serverData);
                }

                LoginSuccess = true;
                loginCoroutine = null;
                yield break;
            }
            else
            {
                if (isRegistration) // revert data
                {
                    serverData.Nickname = oldNickname;
                    serverData.Password = oldPassword;
                }

                LoginSuccess = false;
                loginCoroutine = null;
                yield break;
            }
        }
    }
}
