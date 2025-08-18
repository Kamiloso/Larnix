using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.UI
{
    public class DiscardButton : MonoBehaviour
    {
        public void GoBack()
        {
            Menu.References.Menu.GoBack();
        }
    }
}
