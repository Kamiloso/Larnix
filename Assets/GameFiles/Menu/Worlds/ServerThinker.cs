using Larnix.Socket.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Larnix.Socket;
using System.Linq;
using Larnix.Server.Data;
using System.Security.Cryptography.X509Certificates;

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

    public class LoginInfo
    {
        public string Nickname = "";
        public long ChallengeID = 0;
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
        public LoginInfo loginInfo = null; // how you should login?

        Coroutine loginCoroutine = null;
        public bool? LoginSuccess { get; private set; } = null;

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
            serverInfo = null;
            loginInfo = null;

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
                    loginInfo = new LoginInfo
                    {
                        Nickname = nickname,
                        ChallengeID = serverInfo.PasswordIndex
                    };
                    Login(false);
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

        private IEnumerator LoginCoroutine(bool isRegistration)
        {
            string address = serverData.Address;
            string nickname = serverData.Nickname;
            string password = serverData.Password;

            long serverSecret = KeyObtainer.GetSecretFromAuthCode(serverData.AuthCodeRSA);

            // challengeID obtain

            if (loginInfo == null || loginInfo.Nickname != nickname)
            {
                Task<A_ServerInfo> downloading = new Task<A_ServerInfo>(() =>
                {
                    return Resolver.downloadServerInfo(address, nickname);
                });
                downloading.Start();
                while (!downloading.IsCompleted)
                    yield return null;

                A_ServerInfo dinfo = downloading.Result;
                if (dinfo == null)
                    goto login_failed;

                loginInfo = new LoginInfo
                {
                    Nickname = nickname,
                    ChallengeID = dinfo.PasswordIndex,
                };
            }

            long challengeID = loginInfo.ChallengeID;

            if (!isRegistration)
            {
                // login

                byte[] public_key = serverInfo.PublicKeyModulus.Concat(serverInfo.PublicKeyExponent).ToArray();
                Task<A_LoginTry> login = new Task<A_LoginTry>(() =>
                {
                    return Resolver.tryLogin(address, public_key, nickname, password, serverSecret, challengeID);
                });
                login.Start();
                while (!login.IsCompleted)
                    yield return null;

                A_LoginTry linfo = login.Result;
                if (linfo == null)
                    goto login_failed;

                if (linfo.Code == 1) goto login_success;
                else goto login_failed;
            }
            else
            {
                // register (fake... but will be true in a moment)

                if (challengeID == 0) goto login_success;
                else goto login_failed;
            }

            // goto statements

            login_failed:
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

            login_success:
            {
                if (isRegistration) // apply data
                {
                    ServerSelect.SaveServerData(serverData);
                }

                if (!isRegistration) // logged in, increment
                {
                    loginInfo.ChallengeID++;
                }

                LoginSuccess = true;
                loginCoroutine = null;
                yield break;
            }
        }
    }
}
