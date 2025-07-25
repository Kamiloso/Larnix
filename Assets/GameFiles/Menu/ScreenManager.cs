using Larnix;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    [SerializeField]
    private List<RectTransform> Screens;

    [SerializeField]
    private List<string> EscapeList;

    private string currentScreen = null;

    public void SetScreen(string parentName)
    {
        currentScreen = parentName;
        foreach (var screen in Screens)
        {
            CanvasGroup cg = screen.gameObject.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = screen.gameObject.AddComponent<CanvasGroup>();

            bool isActiveScreen = screen.name == parentName;
            cg.alpha = isActiveScreen ? 1f : 0f;
            cg.interactable = isActiveScreen;
            cg.blocksRaycasts = isActiveScreen;
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

    private void Start()
    {
        foreach(var screen in Screens)
        {
            screen.gameObject.SetActive(true);
        }

        SetScreen(WorldLoad.ScreenLoad);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }
    }
}
