using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionEndUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text playerNameTmpText;
    [SerializeField] private TMP_Text tokenCountTmpText;
    [SerializeField] private TMP_InputField roomNameTmpInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button browseRoomsButton;
    [SerializeField] private TMP_Text statusTmpText;

    private GameManager gameManager;
    private PlayerSession playerSession;

    private void Awake()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomPressed);

        if (browseRoomsButton != null)
            browseRoomsButton.onClick.AddListener(OnBrowseRoomsPressed);
    }

    private void OnDestroy()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.RemoveListener(OnCreateRoomPressed);

        if (browseRoomsButton != null)
            browseRoomsButton.onClick.RemoveListener(OnBrowseRoomsPressed);
    }

    public void Initialize(GameManager manager, PlayerSession session)
    {
        gameManager = manager;
        playerSession = session;
        RefreshSummary();
        ClearStatus();
    }

    public void SetVisible(bool isVisible)
    {
        var target = root != null ? root : gameObject;
        target.SetActive(isVisible);

        if (isVisible)
            RefreshSummary();
    }

    public void PrepareForCurrentSession()
    {
        RefreshSummary();

        if (string.IsNullOrWhiteSpace(UiInputAdapter.GetText(roomNameTmpInput)) && playerSession != null)
            UiInputAdapter.SetText(roomNameTmpInput, playerSession.PlayerName);

        SetBusy(false);
        ClearStatus();
    }

    public void SetBusy(bool isBusy)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = !isBusy;

        if (browseRoomsButton != null)
            browseRoomsButton.interactable = !isBusy;

        UiInputAdapter.SetInteractable(roomNameTmpInput, !isBusy);
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

    public void OnCreateRoomPressed()
    {
        if (gameManager == null)
        {
            LogFailedRequest(nameof(OnCreateRoomPressed), "GameManager reference is missing.");
            return;
        }

        gameManager.RequestCreateRoom(UiInputAdapter.GetText(roomNameTmpInput));
    }

    public void OnBrowseRoomsPressed()
    {
        if (gameManager == null)
        {
            LogFailedRequest(nameof(OnBrowseRoomsPressed), "GameManager reference is missing.");
            return;
        }

        gameManager.OpenRoomBrowser();
    }

    private void RefreshSummary()
    {
        if (playerSession == null)
            return;

        UiTextAdapter.SetText(playerNameTmpText, playerSession.PlayerName);
        UiTextAdapter.SetText(tokenCountTmpText, playerSession.CurrentRunTokens.ToString());
    }

    private void LogFailedRequest(string requestName, string reason)
    {
        UiRequestLogger.LogFailedRequest(this, nameof(SessionEndUI), requestName, reason);
    }
}
