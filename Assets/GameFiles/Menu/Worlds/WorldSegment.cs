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

        private UniversalSelect MySelect;

        private string Name;

        public void Init(string name, UniversalSelect mySelect)
        {
            Name = name;
            MySelect = mySelect;
        }

        private void Update()
        {
            NameText.text = Name;
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
