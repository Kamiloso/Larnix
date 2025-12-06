using Larnix.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using Larnix.Core.Files;
using Larnix.Menu.Forms;
using Version = Larnix.Core.Version;

namespace Larnix.Menu.Worlds
{
    public class ServerSelect : UniversalSelect
    {
        public static string MultiplayerPath { get => Path.Combine(Application.persistentDataPath, "Multiplayer"); }

        private readonly Dictionary<string, ServerThinker> ServerThinkers = new();
        private ServerThinker serverThinker = null;

        [SerializeField] TextMeshProUGUI DescriptionText;

        [SerializeField] TextMeshProUGUI TX_Description;
        [SerializeField] TextMeshProUGUI TX_PlayerAmount;
        [SerializeField] TextMeshProUGUI TX_Motd;
        [SerializeField] TextMeshProUGUI TX_LoginInfo;

        [SerializeField] Button BT_Join;
        [SerializeField] Button BT_Edit;
        [SerializeField] Button BT_Refresh;
        [SerializeField] Button BT_Remove;

        [SerializeField] RectTransform LoginParent;
        [SerializeField] Button BT_Login;
        [SerializeField] Button BT_Register;

        [SerializeField] RectTransform LoggedParent;
        [SerializeField] Button BT_Logout;
        [SerializeField] Button BT_ChangePassword;

        private void Awake()
        {
            References.ServerSelect = this;
            ReloadWorldList();
        }

        public void AddServer()
        {
            BaseForm.GetInstance<ServerEditForm>().EnterForm("ADD");
        }

        public void JoinServer()
        {
            JoinByName(null);
        }

        public void JoinByName(string name = null)
        {
            ServerThinker thinker = ServerThinkers[name ?? SelectedWorld];

            // Save only to change last edit date
            SaveServerData(thinker.serverData);

            WorldLoad.StartRemote(
                address: thinker.serverData.Address,
                authcode: thinker.serverData.AuthCodeRSA,
                nickname: thinker.serverData.Nickname,
                password: thinker.serverData.Password
                );
        }

        public void EditServer()
        {
            ServerThinker thinker = ServerThinkers[SelectedWorld];
            BaseForm.GetInstance<ServerEditForm>().EnterForm("EDIT", SelectedWorld, thinker.serverData.AuthCodeRSA);
        }

        public void RefreshServer()
        {
            if (serverThinker != null)
                serverThinker.SafeRefresh();
        }

        public void RemoveServer()
        {
            if(SelectedWorld != null)
            {
                string folderName = ServerThinkers[SelectedWorld].serverData.FolderName;
                Directory.Delete(Path.Combine(MultiplayerPath, folderName), true);
                ScrollView.RemoveWhere(rt => ReferenceEquals(rt, ServerThinkers[SelectedWorld].transform as RectTransform));
                ServerThinkers.Remove(SelectedWorld);
                SelectWorld(null);
            }
        }

        public void Login()
        {
            ServerThinker thinker = ServerThinkers[SelectedWorld];
            ServerLoginForm form = BaseForm.GetInstance<ServerLoginForm>();

            form.ProvideServerThinker(thinker);
            form.EnterForm(
                SelectedWorld,
                thinker.serverData.Nickname,
                thinker.serverData.Password
                );
        }

        public void Register()
        {
            ServerThinker thinker = ServerThinkers[SelectedWorld];
            ServerRegisterForm form = BaseForm.GetInstance<ServerRegisterForm>();

            form.ProvideServerThinker(thinker);
            form.EnterForm(SelectedWorld);
        }

        public void Logout()
        {
            serverThinker.Logout();
        }

        public void PasswordChange()
        {
            ServerThinker thinker = ServerThinkers[SelectedWorld];
            PasswordChangeForm form = BaseForm.GetInstance<PasswordChangeForm>();

            form.ProvideServerThinker(thinker);
            form.EnterForm(SelectedWorld, thinker.serverData.Nickname);
        }

        protected override void OnWorldSelect(string worldName)
        {
            serverThinker = worldName != null ? ServerThinkers[worldName] : null;
            UpdateUI();
        }

        private void Update()
        {
            UpdateUI();
        }

        public override void ReloadWorldList()
        {
            SelectWorld(null);
            ScrollView.ClearAll();

            foreach (ServerThinker thinker in ServerThinkers.Values)
            {
                Destroy(thinker.gameObject);
            }
            ServerThinkers.Clear();

            // Server Segments
            Dictionary<string, ServerData> servers = ReadServerDataDictionary();
            foreach (string name in servers.Keys)
            {
                AddServerSegment(name, servers[name], false);
            }
        }

        public void AddServerSegment(string name, ServerData serverData, bool asFirst)
        {
            if (ServerThinkers.ContainsKey(name))
                throw new System.InvalidOperationException("Trying to add server segment with a duplicate name: " + name);

            RectTransform rt = Instantiate(WorldSegmentPrefab).transform as RectTransform;
            if (rt == null)
                throw new System.InvalidOperationException("Prefab should be of type RectTransform!");

            rt.name = $"Server: \"{name}\"";
            rt.GetComponent<WorldSegment>().Init(name, this);

            if (asFirst) ScrollView.TopAddElement(rt);
            else ScrollView.BottomAddElement(rt);

            ServerThinker thinker = rt.AddComponent<ServerThinker>();
            thinker.SetServerData(serverData);

            ServerThinkers[name] = thinker;
        }

        private void UpdateUI()
        {
            ThinkerState state = ThinkerState.None;
            LoginState logState = LoginState.None;

            if (serverThinker != null)
            {
                state = serverThinker.State;
                logState = serverThinker.GetLoginState();
            }

            if (state == ThinkerState.None)
            {
                NameText.text = SelectedWorld ?? "";
                TX_Description.text = "";
                TX_Motd.text = "...";
                TX_PlayerAmount.text = "";
                TX_LoginInfo.text = "";

                BT_Edit.interactable = false;
                BT_Refresh.interactable = false;
                BT_Remove.interactable = false;
            }
            else if (state == ThinkerState.Waiting)
            {
                NameText.text = SelectedWorld ?? "";
                TX_Description.text = "Scanning...";
                TX_Motd.text = "...";
                TX_PlayerAmount.text = "LOADING\n?? / ??";
                TX_LoginInfo.text = "";

                BT_Edit.interactable = false;
                BT_Refresh.interactable = false;
                BT_Remove.interactable = true;
            }
            else if (state == ThinkerState.Ready || state == ThinkerState.Incompatible)
            {
                string versionDisplay = serverThinker.serverInfo.GameVersion.ToString();
                string nicknameText = serverThinker.serverInfo.HostUser;
                string hostDisplay = nicknameText != "Player" ? $"Host: {nicknameText}" : "Detached Server";

                NameText.text = SelectedWorld ?? "";
                TX_Description.text = $"Version: {versionDisplay}\n{hostDisplay}";
                TX_Motd.text = serverThinker.serverInfo.Motd;
                TX_PlayerAmount.text = $"ACTIVE\n{serverThinker.serverInfo.CurrentPlayers} / {serverThinker.serverInfo.MaxPlayers}";

                bool regist = serverThinker.WasRegistration;
                switch (logState)
                {
                    case LoginState.None: TX_LoginInfo.text = ""; break;
                    case LoginState.Ready: TX_LoginInfo.text = "Login or register to continue..."; break;
                    case LoginState.Waiting: TX_LoginInfo.text = regist ? "Signing up..." : "Logging in..."; break;
                    case LoginState.Good: TX_LoginInfo.text = $"Playing as {serverThinker.serverData.Nickname}"; break;
                    case LoginState.Bad: TX_LoginInfo.text = regist ? "Register failed!" : "Login failed!"; break;
                }

                if (state == ThinkerState.Incompatible)
                    TX_LoginInfo.text = "Incompatible version!";

                BT_Edit.interactable = true;
                BT_Refresh.interactable = true;
                BT_Remove.interactable = true;
            }
            else if (state == ThinkerState.Failed)
            {
                NameText.text = SelectedWorld ?? "";
                TX_Description.text = "Server not found.";
                TX_Motd.text = "...";
                TX_PlayerAmount.text = "ERROR\n?? / ??";
                TX_LoginInfo.text = "";

                BT_Edit.interactable = true;
                BT_Refresh.interactable = true;
                BT_Remove.interactable = true;
            }
            else if (state == ThinkerState.WrongPublicKey)
            {
                NameText.text = SelectedWorld ?? "";
                TX_Description.text = "Cannot verify server.";
                TX_Motd.text = "...";
                TX_PlayerAmount.text = "ERROR\n?? / ??";
                TX_LoginInfo.text = "";

                BT_Edit.interactable = true;
                BT_Refresh.interactable = true;
                BT_Remove.interactable = true;
            }

            // Account buttons

            bool showLogin = logState != LoginState.Good;
            bool activeLogin = logState == LoginState.Ready || logState == LoginState.Bad;
            bool showLogged = !showLogin;
            bool activeLogged = showLogged;

            LoginParent.gameObject.SetActive(showLogin);
            LoggedParent.gameObject.SetActive(showLogged);

            BT_Login.interactable = activeLogin;
            BT_Register.interactable = activeLogin;
            BT_Logout.interactable = activeLogged;
            BT_ChangePassword.interactable = activeLogged;

            BT_Join.interactable = logState == LoginState.Good;
        }

        public static Dictionary<string, ServerData> ReadServerDataDictionary()
        {
            Dictionary<string, ServerData> returns = new();
            List<string> sortedPaths = GetSortedWorldPaths(MultiplayerPath, "info.txt");

            foreach (string path in sortedPaths)
            {
                string data = FileManager.Read(path, "info.txt");
                if (data == null) continue;

                string[] arg = data.Split('\n').Select(s => Decode(s)).ToArray();
                if (arg.Length < 4) continue;

                if (!returns.ContainsKey(arg[0]))
                {
                    returns.Add(arg[0], new ServerData
                    {
                        FolderName = WorldPathToName(path),
                        Address = arg[0],
                        AuthCodeRSA = arg[1],
                        Nickname = arg[2],
                        Password = arg[3],
                    });
                }
                else
                {
                    Directory.Delete(path, true);
                    Larnix.Debug.LogWarning("Detected and removed saved server name conflict. Address: " + arg[0]);
                }
            }

            return returns;
        }

        public static void SaveServerData(ServerData serverData)
        {
            string data = string.Join("\n", new string[]
            {
                Encode(serverData.Address),
                Encode(serverData.AuthCodeRSA),
                Encode(serverData.Nickname),
                Encode(serverData.Password),
            });
            FileManager.Write(Path.Combine(MultiplayerPath, serverData.FolderName), "info.txt", data);
        }

        private static string Encode(string s) =>
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s));

        private static string Decode(string s) =>
            System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(s));

        public bool ContainsAddress(string address)
        {
            return ServerThinkers.ContainsKey(address);
        }

        public bool ContainsAuthcode(string authcode)
        {
            return ServerThinkers.Values.Any(st => st?.serverData?.AuthCodeRSA == authcode);
        }

        public void EditSegment(string address, string newAddress, string newAuthcode)
        {
            ServerThinker thinker = ServerThinkers[address];
            if (address != newAddress)
            {
                ServerThinkers.Remove(address);
                ServerThinkers[newAddress] = thinker;
            }
            thinker.SubmitServer(newAddress, newAuthcode);

            RectTransform rt = thinker.transform as RectTransform;
            rt.GetComponent<WorldSegment>().ReInit(newAddress);
            ScrollView.BubbleUp(rt);

            if (SelectedWorld == address)
            {
                SelectWorld(newAddress);
            }
        }
    }
}
