using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_InputField playerNameTmpInput;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text statusTmpText;

    private GameManager gameManager;
    private PlayerSession playerSession;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartPressed);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartPressed);
    }

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

        UiInputAdapter.SetInteractable(playerNameTmpInput, !isBusy);
    }

    public void RefreshFromSession()
    {
        if (playerSession == null)
            return;

        UiInputAdapter.SetText(playerNameTmpInput, playerSession.PlayerName);
    }

    public void SetStatus(string message, bool isError)
    {
        var finalMessage = string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : (isError ? "Error: " + message : message);
        UiTextAdapter.SetText(statusTmpText, finalMessage);
    }

    public void ClearStatus()
    {
        SetStatus(string.Empty, false);
    }

    public void OnStartPressed()
    {
        if (gameManager == null)
        {
            LogFailedRequest(nameof(OnStartPressed), "GameManager reference is missing.");
            return;
        }

        gameManager.BeginRun(UiInputAdapter.GetText(playerNameTmpInput));
    }

    private void LogFailedRequest(string requestName, string reason)
    {
        UiRequestLogger.LogFailedRequest(this, nameof(StartMenuUI), requestName, reason);
    }
}

internal static class UiTextAdapter
{
    public static void SetText(TMP_Text tmpText, string value)
    {
        if (tmpText != null)
            tmpText.text = value;
    }
}

internal static class UiInputAdapter
{
    public static string GetText(TMP_InputField tmpInput)
    {
        if (tmpInput != null)
            return tmpInput.text;

        return string.Empty;
    }

    public static void SetText(TMP_InputField tmpInput, string value)
    {
        if (tmpInput != null)
            tmpInput.text = value;
    }

    public static void SetInteractable(TMP_InputField tmpInput, bool isInteractable)
    {
        if (tmpInput != null)
            tmpInput.interactable = isInteractable;
    }
}
