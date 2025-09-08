using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Menu;

namespace Larnix.Menu.Settings
{
    public class ResetOptionButton : MonoBehaviour
    {
        [SerializeField] bool AutoKeyFromParent = true;
        [SerializeField] string Key;

        public void ResetOption()
        {
            SettingsInput.Instance.ResetByKey(AutoKeyFromParent ? transform.parent.name : Key);
        }
    }
}
