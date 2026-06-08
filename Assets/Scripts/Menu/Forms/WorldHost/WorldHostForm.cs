using Larnix.Core;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu.Worlds;
using Larnix.Model;
using Larnix.Server.Run.Records;
using Larnix.Server.Run;

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

        private string VisibleAddress => _serverSpy.RunAnswer?.Address ?? "...";
        private string VisibleAuthcode => _serverSpy.RunAnswer?.Authcode ?? "...";
        private ushort VisiblePort => _serverSpy.RunAnswer?.Port ?? 0;

        private IServerSpy _serverSpy;
        private bool _usesRelay;
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

            _serverSpy = null;
            _relayEstablished = false;

            OF_ServerAddress.text = VisibleAddress;
            OF_Authcode.text = VisibleAuthcode;
            OF_ServerAddress.interactable = false;
            OF_Authcode.interactable = false;

            IF_RelayAddress.interactable = true;
            ButtonTitle.text = "START SERVER";

            BT_RefreshRelay.interactable = true;

            _state = 0;

            Menu.SetScreen("HostWorld");
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

                _usesRelay = IF_RelayAddress.text != "";
                string relayAddress = IF_RelayAddress.text;

                RunInfo runInfo = new(
                    Mode: RunMode.Host,
                    SavesPath: GamePath.SavesPath,
                    WorldName: IF_WorldName.text
                    )
                {
                    RelayAddress = _usesRelay ? relayAddress : null
                };

                _serverSpy = ServerRunner.Start("host", runInfo);

                OF_ServerAddress.text = VisibleAddress;
                OF_Authcode.text = VisibleAuthcode;
                OF_ServerAddress.interactable = true;
                OF_Authcode.interactable = true;

                TX_ErrorText.text = $"Server is running on localhost:{VisiblePort}\n " +
                    (_usesRelay ? $"Connecting to relay..." : "Relay disabled.");

                IF_RelayAddress.interactable = false;
                ButtonTitle.text = "JOIN AS HOST";

                SaveRelayString();

                BT_RefreshRelay.interactable = false;
                BT_Submit.interactable = false;
            }

            if (_state == 1)
            {
                // TODO: Rethink it fully
                return;

                BT_Submit.interactable = true;
            }

            if (_state == 2)
            {
                string worldName = IF_WorldName.text;
                WorldSelect.HostAndPlayWorldByName(worldName, _serverSpy.RunAnswer);
            }

            _state++;
        }

        private void Update()
        {
            if (_usesRelay && !_relayEstablished && _serverSpy.RunAnswer != null)
            {

            }



            if (!_relayEstablished && RelayEstablishment?.IsCompleted == true)
            {
                string connectAddress = RelayEstablishment.Result;
                if (connectAddress == null)
                {
                    TX_ErrorText.text = $"Server is running on localhost:{VisiblePort}\n " +
                        $"Relay connection failed :(";
                }
                else
                {
                    TX_ErrorText.text = $"Server is running on localhost:{VisiblePort}\n " +
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
