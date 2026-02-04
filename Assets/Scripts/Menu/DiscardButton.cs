using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu;

namespace Larnix.Menu
{
    public class DiscardButton : MonoBehaviour
    {
        private Button button;

        private void Start()
        {
            button = GetComponent<Button>();
        }

        private void LateUpdate()
        {
            button.interactable = Ref.Menu.ScreenLock == 0;
        }

        public void GoBack()
        {
            Ref.Menu.GoBack();
        }
    }
}
