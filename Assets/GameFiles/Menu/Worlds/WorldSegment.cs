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

            var cb = PlayButton.colors;
            float oldDuration = cb.fadeDuration;
            cb.fadeDuration = 0f;
            PlayButton.colors = cb;

            UpdateView();

            cb.fadeDuration = oldDuration;
            PlayButton.colors = cb;
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

        public void SetSelection(string worldName)
        {
            SelectButton.interactable = worldName != Name;
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
