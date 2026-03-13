using System;
using StarterAssets;
using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    [SerializeField] private string playerName = "Player";
    [SerializeField, Min(0)] private int currentRunTokens;

    public event Action<string> PlayerNameChanged;
    public event Action<int> TokenCountChanged;

    public string PlayerName => string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
    public int CurrentRunTokens => Mathf.Max(0, currentRunTokens);
    public bool HasTokens => CurrentRunTokens > 0;

    public void SetInputActive(bool enabled)
    {
        var inputs = FindFirstObjectByType<StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.cursorLocked = enabled;
            inputs.cursorInputForLook = enabled;
        }
        else
            Debug.LogWarning("Could not find player armature in scene.");
    }

    public void SetPlayerName(string newPlayerName)
    {
        var sanitizedName = string.IsNullOrWhiteSpace(newPlayerName) ? "Player" : newPlayerName.Trim();
        if (playerName == sanitizedName)
            return;

        playerName = sanitizedName;
        PlayerNameChanged?.Invoke(playerName);
    }

    public void AddTokens(int amount)
    {
        if (amount <= 0)
            return;

        currentRunTokens = Mathf.Max(0, currentRunTokens) + amount;
        TokenCountChanged?.Invoke(currentRunTokens);
    }

    public void ResetRunTokens()
    {
        SetRunTokens(0);
    }

    public void SetRunTokens(int amount)
    {
        currentRunTokens = Mathf.Max(0, amount);
        TokenCountChanged?.Invoke(currentRunTokens);
    }
}
