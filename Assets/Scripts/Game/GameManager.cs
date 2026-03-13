using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private enum PrototypeState
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

    private PrototypeState currentState;
    private bool isSubmissionInFlight;

    public bool IsSessionRunning => currentState == PrototypeState.Playing;

    private void Awake()
    {
        if (playerSession == null)
            playerSession = FindFirstObjectByType<PlayerSession>();

        if (roomService == null)
            roomService = FindFirstObjectByType<MockApiRoomService>();
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

        ShowStartMenu(string.Empty);
    }

    public void BeginRun(string requestedPlayerName)
    {
        if (isSubmissionInFlight || playerSession == null)
            return;

        playerSession.SetPlayerName(requestedPlayerName);
        playerSession.ResetRunTokens();
        ResetSessionPickups();
        ClearAllStatuses();
        SetState(PrototypeState.Playing);
    }

    public void EndRun()
    {
        if (currentState != PrototypeState.Playing)
            return;

        SetState(PrototypeState.SessionEnd);
        if (sessionEndUI != null)
            sessionEndUI.PrepareForCurrentSession();
    }

    public void RequestCreateRoom(string requestedRoomName)
    {
        if (currentState != PrototypeState.SessionEnd || isSubmissionInFlight || playerSession == null)
            return;

        if (roomService == null)
        {
            if (sessionEndUI != null)
                sessionEndUI.SetStatus("Room service reference is missing.", true);

            return;
        }

        if (!playerSession.HasTokens)
        {
            if (sessionEndUI != null)
                sessionEndUI.SetStatus("Collect tokens before creating a room.", true);

            return;
        }

        StartCoroutine(CreateRoomRoutine(requestedRoomName));
    }

    public void OpenRoomBrowser()
    {
        if (currentState != PrototypeState.SessionEnd || isSubmissionInFlight || playerSession == null)
            return;

        if (roomService == null)
        {
            if (sessionEndUI != null)
                sessionEndUI.SetStatus("Room service reference is missing.", true);

            return;
        }

        if (!playerSession.HasTokens)
        {
            if (sessionEndUI != null)
                sessionEndUI.SetStatus("Collect tokens before depositing to a room.", true);

            return;
        }

        SetState(PrototypeState.RoomBrowser);
        if (roomBrowserUI != null)
            roomBrowserUI.Open();
    }

    public void CloseRoomBrowser()
    {
        if (currentState != PrototypeState.RoomBrowser || isSubmissionInFlight)
            return;

        SetState(PrototypeState.SessionEnd);
        if (sessionEndUI != null)
            sessionEndUI.PrepareForCurrentSession();
    }

    public void DepositToSelectedRoom(RoomDto selectedRoom)
    {
        if (currentState != PrototypeState.RoomBrowser || isSubmissionInFlight || playerSession == null)
            return;

        if (roomService == null)
        {
            if (roomBrowserUI != null)
                roomBrowserUI.SetStatus("Room service reference is missing.", true);

            return;
        }

        if (selectedRoom == null)
        {
            if (roomBrowserUI != null)
                roomBrowserUI.SetStatus("Select a room before depositing.", true);

            return;
        }

        if (!playerSession.HasTokens)
        {
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
            if (sessionEndUI != null)
                sessionEndUI.SetStatus(string.IsNullOrWhiteSpace(requestError) ? "Room creation failed." : requestError, true);

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
            if (roomBrowserUI != null)
                roomBrowserUI.SetStatus(string.IsNullOrWhiteSpace(requestError) ? "Deposit failed." : requestError, true);

            yield break;
        }

        playerSession.ResetRunTokens();
        ShowStartMenu("Deposit complete.");
    }

    private void ShowStartMenu(string statusMessage)
    {
        ClearAllStatuses();
        SetState(PrototypeState.StartMenu);
        if (startMenuUI != null)
        {
            startMenuUI.SetBusy(false);
            startMenuUI.RefreshFromSession();
            if (!string.IsNullOrWhiteSpace(statusMessage))
                startMenuUI.SetStatus(statusMessage, false);
        }
    }

    private void SetState(PrototypeState nextState)
    {
        currentState = nextState;

        if (startMenuUI != null)
            startMenuUI.SetVisible(currentState == PrototypeState.StartMenu);

        if (hudUI != null)
            hudUI.SetVisible(currentState == PrototypeState.Playing);

        if (sessionEndUI != null)
            sessionEndUI.SetVisible(currentState == PrototypeState.SessionEnd);

        if (roomBrowserUI != null)
            roomBrowserUI.SetVisible(currentState == PrototypeState.RoomBrowser);

        var gameplayShouldBeActive = currentState == PrototypeState.Playing;
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
}
