using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using Larnix.Menu.Worlds;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Socket;

namespace Larnix.Menu.Forms
{
    public class WorldCreateForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_Nickname;
        [SerializeField] TMP_InputField IF_WorldName;
        [SerializeField] TMP_InputField IF_Seed;

        private Menu Menu => Ref.Menu;

        public override void EnterForm(params string[] args)
        {
            IF_Nickname.text = Settings.Settings.Instance.GetValue("$last-nickname-SGP");
            IF_WorldName.text = "";
            IF_Seed.text = Common.GetSecureLong().ToString();

            TX_ErrorText.text = "";

            Menu.SetScreen("CreateWorld");
        }

        protected override ErrorCode GetErrorCode()
        {
            if (IF_Nickname.text == "Player")
                return ErrorCode.NICKNAME_IS_PLAYER;

            if (!Validation.IsGoodNickname(IF_Nickname.text))
                return ErrorCode.NICKNAME_FORMAT;

            if (!Common.IsValidWorldName(IF_WorldName.text))
                return ErrorCode.WORLD_NAME_FORMAT;

            string path = Path.Combine(WorldSelect.SavesPath, IF_WorldName.text);

            if (Directory.Exists(path))
                return ErrorCode.WORLD_EXISTS;

            return ErrorCode.SUCCESS;
        }

        protected override void RealSubmit()
        {
            string nickname = IF_Nickname.text;
            string worldName = IF_WorldName.text;
            string seedStr = IF_Seed.text;

            long? seedSuggestion = null;

            // seed calculate
            if (long.TryParse(seedStr, out long seed))
            {
                seedSuggestion = seed;
            }
            else if (seedStr.Length > 0)
            {
                seedSuggestion = Common.GetSeedFromString(seedStr);
            }

            WorldMeta.SaveToWorldFolder(worldName, new WorldMeta(Version.Current, nickname));
            WorldSelect.PlayWorldByName(worldName, seedSuggestion);
        }
    }
}
