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
            screen.gameObject.SetActive(screen.transform.name == parentName);
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
