using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu.Worlds;
using Larnix.ServerRun;
using System.Threading.Tasks;
using Larnix.Core;

namespace Larnix.Menu.Forms
{
    public class WorldHostForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_WorldName;
        [SerializeField] TMP_InputField IF_RelayAddress;
        [SerializeField] TMP_InputField OF_ServerAddress;
        [SerializeField] TMP_InputField OF_Authcode;

        [SerializeField] Button BT_RefreshRelay;
        [SerializeField] TMP_Text ButtonTitle;

        private string nickname = null;
        private (string address, string authcode) serverTuple = default;

        private int state = 0;
        private Task<string> RelayEstablishment = null;

        private void Awake()
        {
            IF_RelayAddress.onValueChanged.AddListener(_ => SaveRelayString());
        }

        public override void EnterForm(params string[] args)
        {
            TX_ErrorText.text = "";

            IF_WorldName.text = args[0];
            nickname = args[1];
            IF_RelayAddress.text = Settings.Settings.Instance.GetValue("$relay-server");

            serverTuple = ("...", "...");
            OF_ServerAddress.text = serverTuple.address;
            OF_Authcode.text = serverTuple.authcode;
            OF_ServerAddress.interactable = false;
            OF_Authcode.interactable = false;

            IF_RelayAddress.interactable = true;
            ButtonTitle.text = "START SERVER";
            RelayEstablishment = null;

            BT_RefreshRelay.interactable = true;

            state = 0;

            References.Menu.SetScreen("HostWorld");
        }

        private ushort PortFromAddress(string address)
        {
            try
            {
                string[] args = address.Split(':');
                return ushort.Parse(args[args.Length - 1]);
            }
            catch
            {
                return 0;
            }
        }

        protected override ErrorCode GetErrorCode()
        {
            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            if (state == 0)
            {
                string path = Path.Combine(GamePath.SavesPath, IF_WorldName.text);
                serverTuple = ServerRunner.Instance.StartServer(Server.ServerType.Host, path, null);
                OF_ServerAddress.text = serverTuple.address;
                OF_Authcode.text = serverTuple.authcode;
                OF_ServerAddress.interactable = true;
                OF_Authcode.interactable = true;

                TX_ErrorText.text = $"Server is running on localhost:{PortFromAddress(serverTuple.address)}\n " +
                    (IF_RelayAddress.text != "" ? $"Connecting to relay..." : "Relay disabled.");

                IF_RelayAddress.interactable = false;
                ButtonTitle.text = "JOIN AS HOST";

                if (IF_RelayAddress.text != "")
                {
                    RelayEstablishment = Task.Run(() => ServerRunner.Instance.ConnectToRelay(IF_RelayAddress.text));
                }
                SaveRelayString();

                BT_RefreshRelay.interactable = false;
            }

            if (state == 1)
            {
                string worldName = IF_WorldName.text;
                WorldSelect.HostAndPlayWorldByName(worldName, serverTuple);
            }

            state++;
        }

        private void Update()
        {
            if (RelayEstablishment?.IsCompleted == true)
            {
                string connectAddress = RelayEstablishment.Result;
                if (connectAddress == null)
                {
                    TX_ErrorText.text = $"Server is running on localhost:{PortFromAddress(serverTuple.address)}\n " +
                        $"Relay connection failed :(";
                }
                else
                {
                    TX_ErrorText.text = $"Server is running on localhost:{PortFromAddress(serverTuple.address)}\n " +
                        $"Players can join!";
                    OF_ServerAddress.text = connectAddress;
                }
                RelayEstablishment = null;
            }
        }

        public void RefreshRelay()
        {
            IF_RelayAddress.text = Settings.Settings.Instance.GetDefaultValue("$relay-server");
            SaveRelayString();
        }

        public void SaveRelayString()
        {
            Settings.Settings.Instance.SetValue("$relay-server", IF_RelayAddress.text, true);
        }
    }
}
