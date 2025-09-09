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

        private UniversalSelect MySelect;
        private bool versionChecked = false;

        public string Name { get; private set; }

        public void Init(string name, UniversalSelect mySelect)
        {
            Name = name;
            MySelect = mySelect;

            using (new ButtonFadeDisabler(SelectButton))
            {
                UpdateView();
            }
        }

        public void ReInit(string name)
        {
            Init(name, MySelect);
        }

        private void Update()
        {
            UpdateView();
        }

        private void UpdateView()
        {
            NameText.text = Name;

            if (MySelect is WorldSelect)
            {
                if (!versionChecked)
                {
                    MetadataSGP mdata = MetadataSGP.ReadMetadataSGP(Name);
                    PlayButton.interactable = mdata.version <= Version.Current;
                    versionChecked = true;
                }
            }

            if (MySelect is ServerSelect)
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
            MySelect.SelectWorld(Name);
        }

        public void PlayWorld()
        {
            if (MySelect is WorldSelect)
            {
                WorldSelect.PlayWorldByName(Name);
            }
            
            else if (MySelect is ServerSelect)
            {
                References.ServerSelect.JoinByName(Name);
            }
        }
    }
}
