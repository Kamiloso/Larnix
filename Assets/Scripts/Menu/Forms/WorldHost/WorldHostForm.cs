using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu.Worlds;
using Larnix.ServerRun;

namespace Larnix.Menu.Forms
{
    public class WorldHostForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_WorldName;
        [SerializeField] TMP_InputField OF_ServerAddress;
        [SerializeField] TMP_InputField OF_Authcode;
        [SerializeField] TMP_InputField OF_RelayAddress;

        private string nickname = null;
        private (string address, string authcode) serverTuple = default;

        public override void EnterForm(params string[] args)
        {
            string path = Path.Combine(Core.Common.SavesPath, args[0]);
            serverTuple = ServerRunner.Instance.StartServer(Server.ServerType.Host, path, null);
            TX_ErrorText.text = $"Server is running on port {PortFromAddress(serverTuple.address)}.\n Players can join now.";

            IF_WorldName.text = args[0];
            nickname = args[1];
            OF_ServerAddress.text = serverTuple.address;
            OF_Authcode.text = serverTuple.authcode;
            OF_RelayAddress.text = Settings.Settings.Instance.GetValue("P2P_Server");

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
            string worldName = IF_WorldName.text;
            WorldSelect.HostAndPlayWorldByName(worldName, serverTuple);
        }
    }
}
