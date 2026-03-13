using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_InputField playerNameTmpInput;
    [SerializeField] private InputField playerNameInput;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text statusTmpText;
    [SerializeField] private Text statusText;

    private GameManager gameManager;
    private PlayerSession playerSession;

    public void Initialize(GameManager manager, PlayerSession session)
    {
        gameManager = manager;
        playerSession = session;
        RefreshFromSession();
        ClearStatus();
    }

    public void SetVisible(bool isVisible)
    {
        var target = root != null ? root : gameObject;
        target.SetActive(isVisible);

        if (isVisible)
            RefreshFromSession();
    }

    public void SetBusy(bool isBusy)
    {
        if (startButton != null)
            startButton.interactable = !isBusy;

        UiInputAdapter.SetInteractable(playerNameInput, playerNameTmpInput, !isBusy);
    }

    public void RefreshFromSession()
    {
        if (playerSession == null)
            return;

        UiInputAdapter.SetText(playerNameInput, playerNameTmpInput, playerSession.PlayerName);
    }

    public void SetStatus(string message, bool isError)
    {
        var finalMessage = string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : (isError ? "Error: " + message : message);
        UiTextAdapter.SetText(statusText, statusTmpText, finalMessage);
    }

    public void ClearStatus()
    {
        SetStatus(string.Empty, false);
    }

    public void OnStartPressed()
    {
        if (gameManager == null)
            return;

        gameManager.BeginRun(UiInputAdapter.GetText(playerNameInput, playerNameTmpInput));
    }
}

internal static class UiTextAdapter
{
    public static void SetText(Text legacyText, TMP_Text tmpText, string value)
    {
        if (legacyText != null)
            legacyText.text = value;

        if (tmpText != null)
            tmpText.text = value;
    }
}

internal static class UiInputAdapter
{
    public static string GetText(InputField legacyInput, TMP_InputField tmpInput)
    {
        if (tmpInput != null)
            return tmpInput.text;

        if (legacyInput != null)
            return legacyInput.text;

        return string.Empty;
    }

    public static void SetText(InputField legacyInput, TMP_InputField tmpInput, string value)
    {
        if (legacyInput != null)
            legacyInput.text = value;

        if (tmpInput != null)
            tmpInput.text = value;
    }

    public static void SetInteractable(InputField legacyInput, TMP_InputField tmpInput, bool isInteractable)
    {
        if (legacyInput != null)
            legacyInput.interactable = isInteractable;

        if (tmpInput != null)
            tmpInput.interactable = isInteractable;
    }
}
