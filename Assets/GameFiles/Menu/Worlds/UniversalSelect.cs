using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.UI;
using System;
using System.Linq;

namespace Larnix.Menu.Worlds
{
    public abstract class UniversalSelect : MonoBehaviour
    {
        [SerializeField] protected GameObject WorldSegmentPrefab;

        [SerializeField] protected ScrollView ScrollView;
        [SerializeField] protected TextMeshProUGUI NameText;

        public string SelectedWorld { get; private set; } = null;

        public abstract void ReloadWorldList();
        protected abstract void OnWorldSelect(string worldName);

        public void SelectWorld(string worldName)
        {
            List<RectTransform> list = ScrollView.GetAllTransforms();
            foreach (RectTransform rt in list)
            {
                WorldSegment sgm = rt.GetComponent<WorldSegment>();
                if (sgm != null) sgm.SetSelection(worldName);
            }

            SelectedWorld = worldName ?? null;

            OnWorldSelect(worldName);
        }
    }
}
