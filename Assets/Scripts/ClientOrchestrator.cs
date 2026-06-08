using Larnix.Patches;
using System;
using UnityEngine.SceneManagement;

public record JoinCredentials(
    string Address,
    string Authcode,
    string Nickname,
    string Password
    );

public class ClientOrchestrator : IGlobalUnitySingleton
{
    public bool IsRunning { get; private set; }
    public JoinCredentials JoinCredentials { get; private set; }
    public string ScreenToShow { get; private set; }

    public void JoinWorld(JoinCredentials joinCredentials)
    {
        if (IsRunning)
            throw new InvalidOperationException("Only one world at a time is allowed.");

        IsRunning = true;
        JoinCredentials = joinCredentials;
        ScreenToShow = "Default";

        SceneManager.LoadScene("Client");
    }

    public void CloseWorld(string screenToShow)
    {
        if (!IsRunning)
            throw new InvalidOperationException("No world is currently running.");

        IsRunning = false;
        JoinCredentials = null;
        ScreenToShow = screenToShow;

        SceneManager.LoadScene("Menu");
    }
}
