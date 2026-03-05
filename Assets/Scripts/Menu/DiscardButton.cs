using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Core;

namespace Larnix.Menu
{
    public class DiscardButton : MonoBehaviour
    {
        private Menu Menu => GlobRef.Get<Menu>();
        private Button _button;

        private void Start()
        {
            _button = GetComponent<Button>();
        }

        private void LateUpdate()
        {
            _button.interactable = Menu.ScreenLock == 0;
        }

        public void GoBack()
        {
            Menu.GoBack();
        }
    }
}
