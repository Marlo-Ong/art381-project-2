using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomBrowserUI : MonoBehaviour
{
    private enum BrowseMode
    {
        Recent,
        Leaderboard
    }

    [SerializeField] private GameObject root;
    [SerializeField] private MockApiRoomService roomService;
    [SerializeField] private Transform listContainer;
    [SerializeField] private RoomListItemUI listItemPrefab;
    [SerializeField] private RoomDetailsUI roomDetailsUI;
    [SerializeField] private Button recentButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button depositButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private TMP_Text statusTmpText;
    [SerializeField] private Text statusText;

    private readonly List<RoomListItemUI> spawnedItems = new List<RoomListItemUI>();
    private GameManager gameManager;
    private PlayerSession playerSession;
    private RoomDto selectedRoom;
    private bool isLoading;
    private bool isSubmitting;

    public void Initialize(GameManager manager, MockApiRoomService service, PlayerSession session)
    {
        gameManager = manager;
        roomService = service != null ? service : roomService;
        playerSession = session;

        if (playerSession != null)
            playerSession.TokenCountChanged += HandleTokenCountChanged;

        ClearSelection();
        ClearStatus();
        RefreshButtons();
    }

    private void OnDestroy()
    {
        if (playerSession != null)
            playerSession.TokenCountChanged -= HandleTokenCountChanged;
    }

    public void SetVisible(bool isVisible)
    {
        var target = root != null ? root : gameObject;
        target.SetActive(isVisible);
    }

    public void Open()
    {
        ClearStatus();
        ClearSelection();
        LoadRecentRooms();
    }

    public void LoadRecentRooms()
    {
        if (isLoading || isSubmitting)
            return;

        StartCoroutine(LoadRoomsRoutine(BrowseMode.Recent));
    }

    public void LoadLeaderboardRooms()
    {
        if (isLoading || isSubmitting)
            return;

        StartCoroutine(LoadRoomsRoutine(BrowseMode.Leaderboard));
    }

    public void OnDepositPressed()
    {
        if (gameManager == null)
            return;

        if (selectedRoom == null)
        {
            SetStatus("Select a room before depositing.", true);
            return;
        }

        gameManager.DepositToSelectedRoom(selectedRoom);
    }

    public void OnBackPressed()
    {
        if (gameManager != null)
            gameManager.CloseRoomBrowser();
    }

    public void SetSubmitting(bool submitting, string statusMessage)
    {
        isSubmitting = submitting;
        if (!string.IsNullOrWhiteSpace(statusMessage))
            SetStatus(statusMessage, false);

        UpdateLoadingIndicator();
        RefreshButtons();
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

    public void ClearSelection()
    {
        selectedRoom = null;
        for (var i = 0; i < spawnedItems.Count; i++)
            if (spawnedItems[i] != null)
                spawnedItems[i].SetSelected(false);

        if (roomDetailsUI != null)
            roomDetailsUI.Clear();

        RefreshButtons();
    }

    private IEnumerator LoadRoomsRoutine(BrowseMode mode)
    {
        if (roomService == null)
        {
            SetStatus("Room service reference is missing.", true);
            yield break;
        }

        if (listContainer == null || listItemPrefab == null)
        {
            SetStatus("Assign a list container and list item prefab.", true);
            yield break;
        }

        isLoading = true;
        UpdateLoadingIndicator();
        RefreshButtons();
        ClearStatus();
        ClearSelection();
        ClearList();

        var loadingMessage = mode == BrowseMode.Recent ? "Loading recent rooms..." : "Loading leaderboard...";
        SetStatus(loadingMessage, false);

        List<RoomDto> rooms = null;
        string requestError = null;
        if (mode == BrowseMode.Recent)
            yield return roomService.GetRecentRooms(result => rooms = result, error => requestError = error);
        else
            yield return roomService.GetLeaderboardRooms(result => rooms = result, error => requestError = error);

        isLoading = false;
        UpdateLoadingIndicator();
        RefreshButtons();

        if (!string.IsNullOrWhiteSpace(requestError))
        {
            SetStatus(requestError, true);
            yield break;
        }

        if (rooms == null || rooms.Count == 0)
        {
            SetStatus("No rooms were returned by the API.", false);
            yield break;
        }

        for (var i = 0; i < rooms.Count; i++)
        {
            var item = Instantiate(listItemPrefab, listContainer);
            item.Bind(rooms[i], HandleRoomSelected);
            spawnedItems.Add(item);
        }

        SetStatus(mode == BrowseMode.Recent ? "Showing recent rooms." : "Showing leaderboard.", false);
    }

    private void HandleRoomSelected(RoomDto room, RoomListItemUI item)
    {
        selectedRoom = room;
        for (var i = 0; i < spawnedItems.Count; i++)
            if (spawnedItems[i] != null)
                spawnedItems[i].SetSelected(spawnedItems[i] == item);

        if (roomDetailsUI != null)
            roomDetailsUI.ShowRoom(room);

        RefreshButtons();
    }

    private void ClearList()
    {
        for (var i = 0; i < spawnedItems.Count; i++)
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);

        spawnedItems.Clear();
    }

    private void RefreshButtons()
    {
        if (recentButton != null)
            recentButton.interactable = !isLoading && !isSubmitting;

        if (leaderboardButton != null)
            leaderboardButton.interactable = !isLoading && !isSubmitting;

        if (depositButton != null)
            depositButton.interactable = !isLoading &&
                                         !isSubmitting &&
                                         selectedRoom != null &&
                                         playerSession != null &&
                                         playerSession.HasTokens;

        if (backButton != null)
            backButton.interactable = !isSubmitting;
    }

    private void UpdateLoadingIndicator()
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(isLoading || isSubmitting);
    }

    private void HandleTokenCountChanged(int _)
    {
        RefreshButtons();
    }
}
