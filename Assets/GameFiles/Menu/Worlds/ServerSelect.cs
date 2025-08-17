using Larnix.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using Larnix.Socket.Commands;
using System.Threading.Tasks;
using Larnix.Socket;
using Unity.VisualScripting;

namespace Larnix.Menu.Worlds
{
    public class ServerSelect : UniversalSelect
    {
        public static string MultiplayerFile { get => Path.Combine(Application.persistentDataPath, "servers.txt"); }

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
        }

        private void Start()
        {
            ReloadWorldList();
        }

        public void AddServer()
        {
            UnityEngine.Debug.Log("ADD SERVER");
        }

        public void JoinServer()
        {
            UnityEngine.Debug.Log("JOIN");
        }

        public void EditServer()
        {
            UnityEngine.Debug.Log("EDIT");
        }

        public void RefreshServer()
        {
            if (serverThinker != null)
                serverThinker.SafeRefresh();
        }

        public void RemoveServer()
        {
            UnityEngine.Debug.Log("REMOVE: " + (SelectedWorld ?? "null"));

            ReloadWorldList();
        }

        public void Login()
        {
            UnityEngine.Debug.Log("LOGIN");

            serverThinker.SubmitUser("Kamiloso", "haslo123");
        }

        public void Register()
        {
            UnityEngine.Debug.Log("REGISTER");
        }

        public void Logout()
        {
            serverThinker.Logout();
        }

        public void PasswordChange()
        {
            UnityEngine.Debug.Log("PASSWORD CHANGE");
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
            List<string> availableWorldPaths = new() { "::1", "127.0.0.1", "192.168.0.1", "Test string", "@Kamiloso.240.89.72" };
            foreach (string serverName in availableWorldPaths)
            {
                RectTransform rt = Instantiate(WorldSegmentPrefab).transform as RectTransform;
                if (rt == null)
                    throw new System.InvalidOperationException("Prefab should be of type RectTransform!");

                rt.name = $"Server: \"{serverName}\"";
                rt.GetComponent<WorldSegment>().Init(serverName, this);
                ScrollView.PushElement(rt);

                ServerThinker thinker = rt.AddComponent<ServerThinker>();
                thinker.SubmitServer(serverName, "TTHX-RFF6-7Q8Q");
                thinker.SafeRefresh();
                ServerThinkers[serverName] = thinker;
            }
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

                BT_Edit.interactable = true;
                BT_Refresh.interactable = false;
                BT_Remove.interactable = true;
            }
            else if (state == ThinkerState.Ready)
            {
                NameText.text = SelectedWorld ?? "";
                TX_Description.text = "Version: 0.0.0\nDifficulty: UNKNOWN";
                TX_Motd.text = "This is a test motd.";
                TX_PlayerAmount.text = "ACTIVE\n0 / 0";

                switch (logState)
                {
                    case LoginState.None: TX_LoginInfo.text = ""; break;
                    case LoginState.Ready: TX_LoginInfo.text = "Login or register to continue..."; break;
                    case LoginState.Waiting: TX_LoginInfo.text = "Logging in..."; break;
                    case LoginState.Good: TX_LoginInfo.text = $"Playing as {serverThinker.serverData.Nickname}"; break;
                    case LoginState.Bad: TX_LoginInfo.text = "Login failed!"; break;
                }

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
    }
}
