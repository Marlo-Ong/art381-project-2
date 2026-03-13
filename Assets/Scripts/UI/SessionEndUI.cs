using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionEndUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text playerNameTmpText;
    [SerializeField] private Text playerNameText;
    [SerializeField] private TMP_Text tokenCountTmpText;
    [SerializeField] private Text tokenCountText;
    [SerializeField] private TMP_InputField roomNameTmpInput;
    [SerializeField] private InputField roomNameInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button browseRoomsButton;
    [SerializeField] private TMP_Text statusTmpText;
    [SerializeField] private Text statusText;

    private GameManager gameManager;
    private PlayerSession playerSession;

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

        if (string.IsNullOrWhiteSpace(UiInputAdapter.GetText(roomNameInput, roomNameTmpInput)) && playerSession != null)
            UiInputAdapter.SetText(roomNameInput, roomNameTmpInput, playerSession.PlayerName);

        SetBusy(false);
        ClearStatus();
    }

    public void SetBusy(bool isBusy)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = !isBusy;

        if (browseRoomsButton != null)
            browseRoomsButton.interactable = !isBusy;

        UiInputAdapter.SetInteractable(roomNameInput, roomNameTmpInput, !isBusy);
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

    public void OnCreateRoomPressed()
    {
        if (gameManager == null)
            return;

        gameManager.RequestCreateRoom(UiInputAdapter.GetText(roomNameInput, roomNameTmpInput));
    }

    public void OnBrowseRoomsPressed()
    {
        if (gameManager != null)
            gameManager.OpenRoomBrowser();
    }

    private void RefreshSummary()
    {
        if (playerSession == null)
            return;

        UiTextAdapter.SetText(playerNameText, playerNameTmpText, playerSession.PlayerName);
        UiTextAdapter.SetText(tokenCountText, tokenCountTmpText, playerSession.CurrentRunTokens.ToString());
    }
}
