using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu.Worlds;
using Larnix.Server;
using System.Threading.Tasks;
using Larnix.Core;
using ServerAnswer = Larnix.Server.ServerRunner.ServerAnswer;
using RunSuggestions = Larnix.Server.ServerRunner.RunSuggestions;

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

        private Menu Menu => GlobRef.Get<Menu>();

        private string Address => _serverAnswer?.Address;
        private string Authcode => _serverAnswer?.Authcode;
        private ushort Port => PortFromAddress(Address);
        private Task<string> RelayEstablishment => _serverAnswer?.RelayEstablishment;

        private ServerAnswer _serverAnswer;
        private bool _relayEstablished;

        private int _state = 0;

        private void Awake()
        {
            IF_RelayAddress.onValueChanged.AddListener(_ => SaveRelayString());
        }

        public override void EnterForm(params string[] args)
        {
            TX_ErrorText.text = "";

            IF_WorldName.text = args[0];
            IF_RelayAddress.text = Settings.Settings.Instance.GetValue("$relay-server");

            _serverAnswer = new ServerAnswer("...", "...");
            _relayEstablished = false;

            OF_ServerAddress.text = _serverAnswer.Address;
            OF_Authcode.text = _serverAnswer.Authcode;
            OF_ServerAddress.interactable = false;
            OF_Authcode.interactable = false;

            IF_RelayAddress.interactable = true;
            ButtonTitle.text = "START SERVER";

            BT_RefreshRelay.interactable = true;

            _state = 0;

            Menu.SetScreen("HostWorld");
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
            if (_state == 0)
            {
                string path = Path.Combine(GamePath.SavesPath, IF_WorldName.text);

                bool usesRelay = IF_RelayAddress.text != "";
                string relayAddress = IF_RelayAddress.text;

                RunSuggestions suggestions = new(
                    Seed: null,
                    RelayAddress: usesRelay ? relayAddress : null
                );

                _serverAnswer = ServerRunner.Instance.Start(
                    Server.ServerType.Host, path, suggestions);

                OF_ServerAddress.text = _serverAnswer.Address;
                OF_Authcode.text = _serverAnswer.Authcode;
                OF_ServerAddress.interactable = true;
                OF_Authcode.interactable = true;

                TX_ErrorText.text = $"Server is running on localhost:{Port}\n " +
                    (usesRelay ? $"Connecting to relay..." : "Relay disabled.");

                IF_RelayAddress.interactable = false;
                ButtonTitle.text = "JOIN AS HOST";

                SaveRelayString();

                BT_RefreshRelay.interactable = false;
            }

            if (_state == 1)
            {
                string worldName = IF_WorldName.text;
                WorldSelect.HostAndPlayWorldByName(worldName, _serverAnswer);
            }

            _state++;
        }

        private void Update()
        {
            if (!_relayEstablished && RelayEstablishment?.IsCompleted == true)
            {
                string connectAddress = RelayEstablishment.Result;
                if (connectAddress == null)
                {
                    TX_ErrorText.text = $"Server is running on localhost:{Port}\n " +
                        $"Relay connection failed :(";
                }
                else
                {
                    TX_ErrorText.text = $"Server is running on localhost:{Port}\n " +
                        $"Players can join!";
                    OF_ServerAddress.text = connectAddress;
                }
                _relayEstablished = true;
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
