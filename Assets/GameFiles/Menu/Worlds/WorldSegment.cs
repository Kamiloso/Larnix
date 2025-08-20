using Larnix.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Larnix.Menu.Worlds
{
    public class WorldSegment : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI NameText;
        [SerializeField] Button SelectButton;
        [SerializeField] Button PlayButton;

        private UniversalSelect MySelect;

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

        private void Update()
        {
            UpdateView();
        }

        private void UpdateView()
        {
            NameText.text = Name;

            if (MySelect is WorldSelect)
            {
                PlayButton.interactable = true;
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
                WorldLoad.StartLocal(Name);
            }
            
            else if (MySelect is ServerSelect)
            {
                References.Menu.GoBack();
            }
        }
    }
}
