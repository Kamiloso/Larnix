using Larnix.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Core;
using Version = Larnix.Core.Version;

namespace Larnix.Menu.Worlds
{
    public class WorldSegment : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI NameText;
        [SerializeField] Button SelectButton;
        [SerializeField] Button PlayButton;

        private ServerSelect ServerSelect => GlobRef.Get<ServerSelect>();

        private UniversalSelect _mySelect;
        private bool _versionChecked = false;

        public string Name { get; private set; }

        public void Init(string name, UniversalSelect mySelect)
        {
            Name = name;
            _mySelect = mySelect;

            using (new ButtonFadeDisabler(SelectButton))
            {
                UpdateView();
            }
        }

        public void ReInit(string name)
        {
            Init(name, _mySelect);
        }

        private void Update()
        {
            UpdateView();
        }

        private void UpdateView()
        {
            NameText.text = Name;

            if (_mySelect is WorldSelect)
            {
                if (!_versionChecked)
                {
                    WorldMeta mdata = WorldMeta.ReadFromWorldFolder(Name);
                    PlayButton.interactable = mdata.Version <= Version.Current;
                    _versionChecked = true;
                }
            }

            if (_mySelect is ServerSelect)
            {
                ServerThinker thinker = GetComponent<ServerThinker>();
                PlayButton.interactable = thinker != null ? thinker.GetLoginState() == LoginState.Good : false;
            }
        }

        public void SetSelection(string worldName, bool instant)
        {
            ButtonFadeDisabler bfd = instant ? new ButtonFadeDisabler(SelectButton) : null;
            SelectButton.interactable = worldName != Name;
            bfd?.Dispose();
        }

        public void SelectWorld()
        {
            _mySelect.SelectWorld(Name);
        }

        public void PlayWorld()
        {
            if (_mySelect is WorldSelect)
            {
                WorldSelect.PlayWorldByName(Name);
            }
            
            else if (_mySelect is ServerSelect)
            {
                ServerSelect.JoinByName(Name);
            }
        }
    }
}
