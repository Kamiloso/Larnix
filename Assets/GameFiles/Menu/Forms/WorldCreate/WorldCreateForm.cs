using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu.Worlds;
using QuickNet;

namespace Larnix.Menu.Forms
{
    public class WorldCreateForm : BaseForm
    {
        [SerializeField] TMP_InputField IF_Nickname;
        [SerializeField] TMP_InputField IF_WorldName;
        [SerializeField] TMP_InputField IF_Seed;

        public override void EnterForm(params string[] args)
        {
            IF_Nickname.text = Settings.Settings.Instance.GetValue("$last-nickname-SGP");
            IF_WorldName.text = "";
            IF_Seed.text = Common.GetSecureLong().ToString();

            TX_ErrorText.text = "";

            References.Menu.SetScreen("CreateWorld");
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

            // seed calculate
            if (long.TryParse(seedStr, out long seed))
            {
                WorldLoad.SeedSuggestion = seed;
            }
            else if (seedStr.Length > 0)
            {
                WorldLoad.SeedSuggestion = Common.GetSeedFromString(seedStr);
            }
            else
            {
                WorldLoad.SeedSuggestion = null;
            }

            WorldSelect.SaveMetadataSGP(worldName, new MetadataSGP(Version.Current, nickname));
            WorldSelect.PlayWorldByName(worldName);
        }
    }
}
