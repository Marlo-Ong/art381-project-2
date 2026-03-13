using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private enum GameState
    {
        StartMenu,
        Playing,
        SessionEnd,
        RoomBrowser
    }

    [SerializeField] private PlayerSession playerSession;
    [SerializeField] private MockApiRoomService roomService;
    [SerializeField] private StartMenuUI startMenuUI;
    [SerializeField] private HUDUI hudUI;
    [SerializeField] private SessionEndUI sessionEndUI;
    [SerializeField] private RoomBrowserUI roomBrowserUI;
    [SerializeField] private GameObject[] gameplayObjectsToToggle;
    [SerializeField] private TokenPickup[] tokenPickupsToReset;

    private GameState currentState;
    private bool isSubmissionInFlight;

    public bool IsSessionRunning => currentState == GameState.Playing;

    private void Awake()
    {
        if (playerSession == null)
            playerSession = FindFirstObjectByType<PlayerSession>(FindObjectsInactive.Include);

        if (roomService == null)
            roomService = FindFirstObjectByType<MockApiRoomService>(FindObjectsInactive.Include);

        if (startMenuUI == null)
            startMenuUI = FindFirstObjectByType<StartMenuUI>(FindObjectsInactive.Include);

        if (hudUI == null)
            hudUI = FindFirstObjectByType<HUDUI>(FindObjectsInactive.Include);

        if (sessionEndUI == null)
            sessionEndUI = FindFirstObjectByType<SessionEndUI>(FindObjectsInactive.Include);

        if (roomBrowserUI == null)
            roomBrowserUI = FindFirstObjectByType<RoomBrowserUI>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        if (startMenuUI != null)
            startMenuUI.Initialize(this, playerSession);

        if (hudUI != null)
            hudUI.Initialize(this, playerSession);

        if (sessionEndUI != null)
            sessionEndUI.Initialize(this, playerSession);

        if (roomBrowserUI != null)
            roomBrowserUI.Initialize(this, roomService, playerSession);

        playerSession.SetInputActive(false);
        ShowStartMenu(string.Empty);
    }

    public void BeginRun(string requestedPlayerName)
    {
        if (isSubmissionInFlight)
        {
            LogFailedRequest(nameof(BeginRun), "Another submission is already in flight.");
            return;
        }

        if (playerSession == null)
        {
            LogFailedRequest(nameof(BeginRun), "PlayerSession reference is missing.");
            return;
        }

        playerSession.SetInputActive(true);
        playerSession.SetPlayerName(requestedPlayerName);
        playerSession.ResetRunTokens();
        ResetSessionPickups();
        ClearAllStatuses();
        SetState(GameState.Playing);
    }

    public void EndRun()
    {
        if (currentState != GameState.Playing)
        {
            LogFailedRequest(nameof(EndRun), $"The current state is {currentState}, not {GameState.Playing}.");
            return;
        }

        playerSession.SetInputActive(false);
        SetState(GameState.SessionEnd);
        if (sessionEndUI != null)
            sessionEndUI.PrepareForCurrentSession();
    }

    public void RequestCreateRoom(string requestedRoomName)
    {
        if (currentState != GameState.SessionEnd)
        {
            LogFailedRequest(nameof(RequestCreateRoom), $"The current state is {currentState}, not {GameState.SessionEnd}.");
            return;
        }

        if (isSubmissionInFlight)
        {
            LogFailedRequest(nameof(RequestCreateRoom), "Another submission is already in flight.");
            return;
        }

        if (playerSession == null)
        {
            LogFailedRequest(nameof(RequestCreateRoom), "PlayerSession reference is missing.");
            return;
        }

        if (roomService == null)
        {
            LogFailedRequest(nameof(RequestCreateRoom), "Room service reference is missing.");
            if (sessionEndUI != null)
                sessionEndUI.SetStatus("Room service reference is missing.", true);

            return;
        }

        if (!playerSession.HasTokens)
        {
            LogFailedRequest(nameof(RequestCreateRoom), "The player has no tokens to submit.");
            if (sessionEndUI != null)
                sessionEndUI.SetStatus("Collect tokens before creating a room.", true);

            return;
        }

        StartCoroutine(CreateRoomRoutine(requestedRoomName));
    }

    public void OpenRoomBrowser()
    {
        if (currentState != GameState.SessionEnd)
        {
            LogFailedRequest(nameof(OpenRoomBrowser), $"The current state is {currentState}, not {GameState.SessionEnd}.");
            return;
        }

        if (isSubmissionInFlight)
        {
            LogFailedRequest(nameof(OpenRoomBrowser), "Another submission is already in flight.");
            return;
        }

        if (playerSession == null)
        {
            LogFailedRequest(nameof(OpenRoomBrowser), "PlayerSession reference is missing.");
            return;
        }

        if (roomService == null)
        {
            LogFailedRequest(nameof(OpenRoomBrowser), "Room service reference is missing.");
            if (sessionEndUI != null)
                sessionEndUI.SetStatus("Room service reference is missing.", true);

            return;
        }

        if (!playerSession.HasTokens)
        {
            LogFailedRequest(nameof(OpenRoomBrowser), "The player has no tokens to deposit.");
            if (sessionEndUI != null)
                sessionEndUI.SetStatus("Collect tokens before depositing to a room.", true);

            return;
        }

        SetState(GameState.RoomBrowser);
        if (roomBrowserUI != null)
            roomBrowserUI.Open();
    }

    public void CloseRoomBrowser()
    {
        if (currentState != GameState.RoomBrowser)
        {
            LogFailedRequest(nameof(CloseRoomBrowser), $"The current state is {currentState}, not {GameState.RoomBrowser}.");
            return;
        }

        if (isSubmissionInFlight)
        {
            LogFailedRequest(nameof(CloseRoomBrowser), "A submission is still in flight.");
            return;
        }

        SetState(GameState.SessionEnd);
        if (sessionEndUI != null)
            sessionEndUI.PrepareForCurrentSession();
    }

    public void DepositToSelectedRoom(RoomDto selectedRoom)
    {
        if (currentState != GameState.RoomBrowser)
        {
            LogFailedRequest(nameof(DepositToSelectedRoom), $"The current state is {currentState}, not {GameState.RoomBrowser}.");
            return;
        }

        if (isSubmissionInFlight)
        {
            LogFailedRequest(nameof(DepositToSelectedRoom), "Another submission is already in flight.");
            return;
        }

        if (playerSession == null)
        {
            LogFailedRequest(nameof(DepositToSelectedRoom), "PlayerSession reference is missing.");
            return;
        }

        if (roomService == null)
        {
            LogFailedRequest(nameof(DepositToSelectedRoom), "Room service reference is missing.");
            if (roomBrowserUI != null)
                roomBrowserUI.SetStatus("Room service reference is missing.", true);

            return;
        }

        if (selectedRoom == null)
        {
            LogFailedRequest(nameof(DepositToSelectedRoom), "No room was selected.");
            if (roomBrowserUI != null)
                roomBrowserUI.SetStatus("Select a room before depositing.", true);

            return;
        }

        if (!playerSession.HasTokens)
        {
            LogFailedRequest(nameof(DepositToSelectedRoom), "The player has no tokens to deposit.");
            if (roomBrowserUI != null)
                roomBrowserUI.SetStatus("No tokens are available to deposit.", true);

            return;
        }

        StartCoroutine(DepositRoutine(selectedRoom));
    }

    private IEnumerator CreateRoomRoutine(string requestedRoomName)
    {
        isSubmissionInFlight = true;
        if (sessionEndUI != null)
        {
            sessionEndUI.SetBusy(true);
            sessionEndUI.SetStatus("Creating room...", false);
        }

        var timestamp = ApiDateUtils.GetCurrentUtcIsoString();
        var createRequest = new CreateRoomRequest
        {
            ownerName = playerSession.PlayerName,
            roomName = ResolveRoomName(requestedRoomName),
            totalTokens = playerSession.CurrentRunTokens,
            createdAt = timestamp,
            updatedAt = timestamp
        };

        RoomDto createdRoom = null;
        string requestError = null;
        yield return roomService.CreateRoom(createRequest, room => createdRoom = room, error => requestError = error);

        isSubmissionInFlight = false;
        if (sessionEndUI != null)
            sessionEndUI.SetBusy(false);

        if (!string.IsNullOrWhiteSpace(requestError) || createdRoom == null)
        {
            var failureReason = string.IsNullOrWhiteSpace(requestError) ? "Room creation failed." : requestError;
            LogFailedRequest(nameof(RequestCreateRoom), failureReason);
            if (sessionEndUI != null)
                sessionEndUI.SetStatus(failureReason, true);

            yield break;
        }

        playerSession.ResetRunTokens();
        ShowStartMenu("Room created successfully.");
    }

    private IEnumerator DepositRoutine(RoomDto selectedRoom)
    {
        isSubmissionInFlight = true;
        if (roomBrowserUI != null)
            roomBrowserUI.SetSubmitting(true, "Depositing tokens...");

        RoomDto updatedRoom = null;
        string requestError = null;
        yield return roomService.DepositToRoom(
            selectedRoom.id,
            playerSession.CurrentRunTokens,
            room => updatedRoom = room,
            error => requestError = error);

        isSubmissionInFlight = false;
        if (roomBrowserUI != null)
            roomBrowserUI.SetSubmitting(false, string.Empty);

        if (!string.IsNullOrWhiteSpace(requestError) || updatedRoom == null)
        {
            var failureReason = string.IsNullOrWhiteSpace(requestError) ? "Deposit failed." : requestError;
            LogFailedRequest(nameof(DepositToSelectedRoom), failureReason);
            if (roomBrowserUI != null)
                roomBrowserUI.SetStatus(failureReason, true);

            yield break;
        }

        playerSession.ResetRunTokens();
        ShowStartMenu("Deposit complete.");
    }

    private void ShowStartMenu(string statusMessage)
    {
        ClearAllStatuses();
        SetState(GameState.StartMenu);
        if (startMenuUI != null)
        {
            startMenuUI.SetBusy(false);
            startMenuUI.RefreshFromSession();
            if (!string.IsNullOrWhiteSpace(statusMessage))
                startMenuUI.SetStatus(statusMessage, false);
        }
    }

    private void SetState(GameState nextState)
    {
        currentState = nextState;

        if (startMenuUI != null)
            startMenuUI.SetVisible(currentState == GameState.StartMenu);

        if (hudUI != null)
            hudUI.SetVisible(currentState == GameState.Playing);

        if (sessionEndUI != null)
            sessionEndUI.SetVisible(currentState == GameState.SessionEnd);

        if (roomBrowserUI != null)
            roomBrowserUI.SetVisible(currentState == GameState.RoomBrowser);

        var gameplayShouldBeActive = currentState == GameState.Playing;
        for (var i = 0; i < gameplayObjectsToToggle.Length; i++)
            if (gameplayObjectsToToggle[i] != null)
                gameplayObjectsToToggle[i].SetActive(gameplayShouldBeActive);
    }

    private void ResetSessionPickups()
    {
        for (var i = 0; i < tokenPickupsToReset.Length; i++)
            if (tokenPickupsToReset[i] != null)
                tokenPickupsToReset[i].ResetPickup();
    }

    private void ClearAllStatuses()
    {
        if (startMenuUI != null)
            startMenuUI.ClearStatus();

        if (sessionEndUI != null)
        {
            sessionEndUI.ClearStatus();
            sessionEndUI.SetBusy(false);
        }

        if (roomBrowserUI != null)
        {
            roomBrowserUI.ClearStatus();
            roomBrowserUI.SetSubmitting(false, string.Empty);
            roomBrowserUI.ClearSelection();
        }
    }

    private string ResolveRoomName(string requestedRoomName)
    {
        return string.IsNullOrWhiteSpace(requestedRoomName)
            ? playerSession.PlayerName
            : requestedRoomName.Trim();
    }

    private void LogFailedRequest(string requestName, string reason)
    {
        UiRequestLogger.LogFailedRequest(this, nameof(GameManager), requestName, reason);
    }
}
