using Larnix.Socket;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading;
using UnityEditor;
using Larnix.Socket.Commands;
using System.Linq;
using System;
using Larnix.Menu.Worlds;
using TMPro;

namespace Larnix.Menu
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI VersionText;
        [SerializeField] List<RectTransform> Screens;
        [SerializeField] List<string> UniversalSelectScreens;
        [SerializeField] List<string> EscapeList;

        private string currentScreen = null;

        public void Awake()
        {
            Application.runInBackground = true;
            References.Menu = this;

            UnityEngine.Debug.Log("Menu loaded");
        }

        private void Start()
        {
            VersionText.text = "Version " + Version.Current;

            foreach (var screen in Screens)
            {
                screen.gameObject.SetActive(true);
            }

            SetScreen(WorldLoad.ScreenLoad);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GoBack();
            }
        }

        public void SetScreen(string newScreen)
        {
            string oldScreen = currentScreen;
            currentScreen = newScreen;

            foreach (var screen in Screens)
            {
                CanvasGroup cg = screen.gameObject.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = screen.gameObject.AddComponent<CanvasGroup>();

                bool isActiveScreen = screen.name == newScreen;
                cg.alpha = isActiveScreen ? 1f : 0f;
                cg.interactable = isActiveScreen;
                cg.blocksRaycasts = isActiveScreen;
            }

            if (UniversalSelectScreens.Contains(newScreen))
            {
                if (!EscapeList.Contains(oldScreen + "~" + newScreen)) // if not going back
                SelectTopElements();
            }
        }

        public void GoBack()
        {
            foreach (string str in EscapeList)
            {
                string[] rule = str.Split('~');
                if (rule.Length >= 2)
                {
                    if (currentScreen == rule[0])
                    {
                        SetScreen(rule[1]);
                        break;
                    }
                }
            }
        }

        public void Quit()
        {
            Application.Quit();
        }

        private void SelectTopElements()
        {
            UniversalSelect[] universalSelects = FindObjectsByType<UniversalSelect>(FindObjectsSortMode.None);
            foreach (var usl in universalSelects)
            {
                usl.TrySelectTopElement(true);
            }
        }
    }
}
