using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RootRoomFlowController : MonoBehaviour
{
    private enum BrowseMode
    {
        Recent,
        Leaderboard
    }

    private const string DefaultBaseUrl = "https://69b358d8e224ec066bdbf5ae.mockapi.io/";

    [Header("Scenes")]
    [SerializeField] private string collectSceneName = "CollectScene";
    [SerializeField] private string viewSceneName = "ViewScene";

    [Header("API")]
    [SerializeField] private string baseUrl = DefaultBaseUrl;
    [SerializeField, Min(1)] private int recentPageSize = 20;
    [SerializeField, Min(1)] private int leaderboardPageSize = 20;
    [SerializeField, Min(1)] private int requestTimeoutSeconds = 15;
    [SerializeField] private bool usePatchForUpdates = true;

    [Header("Browser Shortcut")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key openBrowserKey = Key.F8;
#endif

    [Header("View Scale")]
    [SerializeField, Min(0.1f)] private float minScaleMultiplier = 0.75f;
    [SerializeField, Min(0.1f)] private float maxScaleMultiplier = 4f;
    [SerializeField, Min(0.01f)] private float scalePerLogStep = 0.75f;

    [Header("Browser")]
    [SerializeField] private bool autoRefreshBrowserOnOpen = true;

    private static RootRoomFlowController instance;

    private readonly List<RoomDto> browserRooms = new List<RoomDto>();

    private RootSceneLoader sceneLoader;
    private MockApiRoomService runtimeRoomService;
    private PlayerSession activePlayerSession;
    private RootSoundEffectPlayer soundEffectPlayer;
    private Transform viewGummyWorm;
    private Vector3 viewGummyInitialScale = Vector3.one;
    private Vector2 browserScrollPosition;

    private RoomDto selectedRoom;
    private string playerName = "MyQuarry";
    private string pendingRoomName = "MyQuarry";
    private string statusMessage = string.Empty;
    private string lastSuggestedRoomName = "NewQuarry";
    private int currentRunTokens;
    private bool hasViewGummyInitialScale;
    private bool hasCreatedRoomThisPlaythrough;
    private bool isCreatingRoom;
    private bool isLoadingRooms;
    private bool isDepositingTokens;
    private bool browserOpen;
    private bool uiOwnsCursorState;
    private bool restoreCursorLocked = true;
    private bool restoreCursorVisible;
    private BrowseMode currentBrowseMode = BrowseMode.Recent;

    public bool IsBrowserOpen => browserOpen;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        sceneLoader = GetComponent<RootSceneLoader>();
        soundEffectPlayer = GetComponent<RootSoundEffectPlayer>();
        EnsureRuntimeRoomService();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void Start()
    {
        RefreshSceneBindings();
        UpdateCursorOwnership();
    }

    private void Update()
    {
        HandleCollectSceneShortcut();
        UpdateCursorOwnership();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        DetachPlayerSession();

        if (uiOwnsCursorState)
            ApplyCursorState(restoreCursorLocked, restoreCursorVisible);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (scene.name != collectSceneName && scene.name != viewSceneName)
            return;

        RefreshSceneBindings();

        if (scene.name == viewSceneName)
            ApplySelectedRoomToViewScene();

        UpdateCursorOwnership();
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        if (scene.name == viewSceneName)
        {
            viewGummyWorm = null;
            hasViewGummyInitialScale = false;
        }
    }

    private void OpenBrowser()
    {
        browserOpen = true;
        SyncPendingRoomName(playerName);
        SetStatus(
            hasCreatedRoomThisPlaythrough
                ? "Select a quarry to open it."
                : "Select a quarry to open it, or create a new one.",
            false);

        if (autoRefreshBrowserOnOpen || browserRooms.Count == 0)
            LoadRooms(currentBrowseMode);

        UpdateCursorOwnership();
    }

    private void CloseBrowser()
    {
        browserOpen = false;
        UpdateCursorOwnership();
    }

    private void LoadRooms(BrowseMode mode)
    {
        if (isLoadingRooms || isDepositingTokens || isCreatingRoom)
            return;

        StartCoroutine(LoadRoomsRoutine(mode));
    }

    private IEnumerator LoadRoomsRoutine(BrowseMode mode)
    {
        EnsureRuntimeRoomService();
        if (runtimeRoomService == null)
        {
            SetStatus("Quarry service is not configured.", true);
            yield break;
        }

        isLoadingRooms = true;
        currentBrowseMode = mode;
        var selectedRoomId = selectedRoom != null ? selectedRoom.id : string.Empty;
        browserRooms.Clear();
        SetStatus(mode == BrowseMode.Recent ? "Loading recent quarries..." : "Loading popular quarries...", false);

        List<RoomDto> loadedRooms = null;
        string requestError = null;
        if (mode == BrowseMode.Recent)
            yield return runtimeRoomService.GetRecentRooms(result => loadedRooms = result, error => requestError = error);
        else
            yield return runtimeRoomService.GetLeaderboardRooms(result => loadedRooms = result, error => requestError = error);

        isLoadingRooms = false;

        if (!string.IsNullOrWhiteSpace(requestError))
        {
            SetStatus(requestError, true);
            yield break;
        }

        if (loadedRooms == null || loadedRooms.Count == 0)
        {
            selectedRoom = null;
            SetStatus("No quarries were returned by the API.", false);
            yield break;
        }

        browserRooms.AddRange(loadedRooms);
        if (!string.IsNullOrWhiteSpace(selectedRoomId))
        {
            for (var i = 0; i < browserRooms.Count; i++)
            {
                if (browserRooms[i] == null || browserRooms[i].id != selectedRoomId)
                    continue;

                selectedRoom = browserRooms[i];
                break;
            }
        }

        SetStatus(mode == BrowseMode.Recent ? "Showing recent quarries." : "Showing popular quarries.", false);
    }

    private void ViewRoom(RoomDto room)
    {
        if (room == null)
            return;

        selectedRoom = room;
        CloseBrowser();

        if (GetActiveSceneName() != viewSceneName)
        {
            if (sceneLoader != null)
                sceneLoader.LoadViewScene();
            else
                SceneManager.LoadScene(viewSceneName);

            return;
        }

        ApplySelectedRoomToViewScene();
    }

    private void DepositSelectedRoomTokens()
    {
        if (isLoadingRooms || isDepositingTokens || isCreatingRoom || selectedRoom == null || currentRunTokens <= 0)
            return;

        StartCoroutine(DepositTokensRoutine());
    }

    private void CreateRoomFromCurrentTokens()
    {
        if (isLoadingRooms || isDepositingTokens || isCreatingRoom)
            return;

        if (hasCreatedRoomThisPlaythrough)
        {
            SetStatus("You can only create one quarry per playthrough.", true);
            return;
        }

        if (currentRunTokens <= 0)
        {
            SetStatus("Collect artifacts before creating a quarry.", true);
            return;
        }

        StartCoroutine(CreateRoomRoutine());
    }

    private IEnumerator CreateRoomRoutine()
    {
        EnsureRuntimeRoomService();
        if (runtimeRoomService == null)
        {
            SetStatus("Quarry service is not configured.", true);
            yield break;
        }

        isCreatingRoom = true;
        SetStatus("Creating quarry...", false);

        var timestamp = ApiDateUtils.GetCurrentUtcIsoString();
        var createRequest = new CreateRoomRequest
        {
            roomName = ResolvePendingRoomName(),
            totalTokens = Mathf.Max(0, currentRunTokens),
            createdAt = timestamp,
            updatedAt = timestamp
        };

        RoomDto createdRoom = null;
        string requestError = null;
        yield return runtimeRoomService.CreateRoom(
            createRequest,
            room => createdRoom = room,
            error => requestError = error);

        isCreatingRoom = false;

        if (!string.IsNullOrWhiteSpace(requestError) || createdRoom == null)
        {
            SetStatus(string.IsNullOrWhiteSpace(requestError) ? "Quarry creation failed." : requestError, true);
            yield break;
        }

        hasCreatedRoomThisPlaythrough = true;
        UpsertBrowserRoom(createdRoom);
        SetCurrentRunTokens(0);
        SetStatus("Quarry created.", false);
        ViewRoom(createdRoom);
    }

    private IEnumerator DepositTokensRoutine()
    {
        EnsureRuntimeRoomService();
        if (runtimeRoomService == null)
        {
            SetStatus("Quarry service is not configured.", true);
            yield break;
        }

        isDepositingTokens = true;
        SetStatus("Depositing artifacts...", false);

        var tokensToDeposit = Mathf.Max(0, currentRunTokens);
        RoomDto updatedRoom = null;
        string requestError = null;
        yield return runtimeRoomService.DepositToRoom(
            selectedRoom.id,
            tokensToDeposit,
            room => updatedRoom = room,
            error => requestError = error);

        isDepositingTokens = false;

        if (!string.IsNullOrWhiteSpace(requestError) || updatedRoom == null)
        {
            SetStatus(string.IsNullOrWhiteSpace(requestError) ? "Deposit failed." : requestError, true);
            yield break;
        }

        selectedRoom = updatedRoom;
        UpsertBrowserRoom(updatedRoom);

        SetCurrentRunTokens(0);
        PlayTokensDepositedSound();
        ApplySelectedRoomToViewScene();
        SetStatus("Deposit complete.", false);
    }

    private void BackToCollectScene()
    {
        browserOpen = false;
        UpdateCursorOwnership();

        if (sceneLoader != null)
            sceneLoader.LoadCollectScene();
        else
            SceneManager.LoadScene(collectSceneName);
    }

    private void RefreshSceneBindings()
    {
        AttachPlayerSession(ResolvePlayerSession());
    }

    private void AttachPlayerSession(PlayerSession session)
    {
        if (activePlayerSession == session)
            return;

        DetachPlayerSession();
        activePlayerSession = session;
        if (activePlayerSession == null)
            return;

        activePlayerSession.PlayerNameChanged += HandlePlayerNameChanged;
        activePlayerSession.TokenCountChanged += HandleTokenCountChanged;
        playerName = activePlayerSession.PlayerName;
        currentRunTokens = activePlayerSession.CurrentRunTokens;
        SyncPendingRoomName(playerName);
    }

    private void DetachPlayerSession()
    {
        if (activePlayerSession == null)
            return;

        activePlayerSession.PlayerNameChanged -= HandlePlayerNameChanged;
        activePlayerSession.TokenCountChanged -= HandleTokenCountChanged;
        activePlayerSession = null;
    }

    private PlayerSession ResolvePlayerSession()
    {
        var playerSession = GetComponent<PlayerSession>();
        if (playerSession != null)
            return playerSession;

        return FindFirstObjectByType<PlayerSession>(FindObjectsInactive.Include);
    }

    private void HandlePlayerNameChanged(string newPlayerName)
    {
        playerName = string.IsNullOrWhiteSpace(newPlayerName) ? "Player" : newPlayerName.Trim();
        SyncPendingRoomName(playerName);
    }

    private void HandleTokenCountChanged(int newTokenCount)
    {
        currentRunTokens = Mathf.Max(0, newTokenCount);
    }

    private void SetCurrentRunTokens(int newTokenCount)
    {
        currentRunTokens = Mathf.Max(0, newTokenCount);

        if (activePlayerSession != null)
            activePlayerSession.SetRunTokens(currentRunTokens);
    }

    private void ApplySelectedRoomToViewScene()
    {
        if (selectedRoom == null || !EnsureViewGummyWorm())
            return;

        viewGummyWorm.localScale = viewGummyInitialScale * selectedRoom.totalTokens;
    }

    private bool EnsureViewGummyWorm()
    {
        var viewScene = SceneManager.GetSceneByName(viewSceneName);
        if (!viewScene.IsValid() || !viewScene.isLoaded)
            return false;

        if (viewGummyWorm != null)
            return true;

        viewGummyWorm = FindTransformInScene(viewScene, "GummyWorm");
        if (viewGummyWorm == null)
            return false;

        viewGummyInitialScale = viewGummyWorm.localScale;
        hasViewGummyInitialScale = true;
        return hasViewGummyInitialScale;
    }

    private void EnsureRuntimeRoomService()
    {
        if (runtimeRoomService != null)
            return;

        var runtimeRoot = new GameObject("RoomViewRuntime");
        runtimeRoot.transform.SetParent(transform, false);

        var config = runtimeRoot.AddComponent<ApiConfig>();
        config.Configure(baseUrl, recentPageSize, leaderboardPageSize);

        var client = runtimeRoot.AddComponent<ApiClient>();
        client.Configure(requestTimeoutSeconds);

        runtimeRoomService = runtimeRoot.AddComponent<MockApiRoomService>();
        runtimeRoomService.Configure(config, client, usePatchForUpdates);
    }

    private void PlayTokensDepositedSound()
    {
        if (soundEffectPlayer == null)
            soundEffectPlayer = GetComponent<RootSoundEffectPlayer>();

        if (soundEffectPlayer != null)
            soundEffectPlayer.PlaySoundEffect(RootSoundEffectPlayer.TokensDepositedSoundEffectIndex);
    }

    private void UpdateCursorOwnership()
    {
        var shouldUnlock = browserOpen || ShouldShowViewOverlay();
        if (shouldUnlock)
        {
            if (!uiOwnsCursorState)
            {
                restoreCursorLocked = Cursor.lockState == CursorLockMode.Locked;
                restoreCursorVisible = Cursor.visible;
                uiOwnsCursorState = true;
            }

            ApplyCursorState(false, true);
            return;
        }

        if (!uiOwnsCursorState)
            return;

        ApplyCursorState(restoreCursorLocked, restoreCursorVisible);
        uiOwnsCursorState = false;
    }

    private static void ApplyCursorState(bool cursorLocked, bool cursorVisible)
    {
        Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = cursorVisible;
    }

    private void HandleCollectSceneShortcut()
    {
#if ENABLE_INPUT_SYSTEM
        if (!CanOpenBrowserFromCollectScene())
            return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[openBrowserKey].wasPressedThisFrame)
            OpenBrowser();
#endif
    }

    private bool CanOpenBrowserFromCollectScene()
    {
        return !browserOpen &&
               !isLoadingRooms &&
               !isDepositingTokens &&
               !isCreatingRoom &&
               GetActiveSceneName() == collectSceneName;
    }

    private bool ShouldShowCollectScenePrompt()
    {
        return GetActiveSceneName() == collectSceneName;
    }

    private bool ShouldShowViewOverlay()
    {
        return GetActiveSceneName() == viewSceneName && selectedRoom != null;
    }

    private string GetActiveSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    private void SetStatus(string message, bool isError)
    {
        statusMessage = string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : (isError ? "Error: " + message : message);
    }

    private void OnGUI()
    {
        DrawCollectSceneArtifactCount();

        if (ShouldShowCollectScenePrompt())
            DrawCollectScenePrompt();

        if (browserOpen)
            DrawBrowserOverlay();

        if (ShouldShowViewOverlay())
            DrawViewOverlay();
    }

    private void DrawCollectScenePrompt()
    {
#if ENABLE_INPUT_SYSTEM
        var label = $"Press {openBrowserKey} to browse quarries";
#else
        var label = "Browse quarries";
#endif
        var size = GUI.skin.box.CalcSize(new GUIContent(label));
        var rect = new Rect(16f, Screen.height - size.y - 24f, size.x + 20f, size.y + 10f);
        GUI.Box(rect, label);
    }

    private void DrawCollectSceneArtifactCount()
    {
        var label = "Collected artifacts: " + Mathf.Max(0, currentRunTokens);
        var size = GUI.skin.box.CalcSize(new GUIContent(label));
        var width = size.x + 20f;
        var height = size.y + 10f;
        var rect = new Rect(Screen.width - width - 16f, Screen.height - height - 24f, width, height);
        GUI.Box(rect, label);
    }

    private void DrawBrowserOverlay()
    {
        var areaHeight = Mathf.Min(Screen.height - 32f, 520f);
        var scrollHeight = Mathf.Max(120f, areaHeight - 250f);

        GUILayout.BeginArea(new Rect(16f, 16f, 420f, areaHeight), GUI.skin.window);

        if (!hasCreatedRoomThisPlaythrough)
        {
            GUILayout.Label("New Quarry Name");

            GUI.enabled = !isLoadingRooms && !isDepositingTokens && !isCreatingRoom && !hasCreatedRoomThisPlaythrough;
            pendingRoomName = GUILayout.TextField(pendingRoomName ?? string.Empty);

            GUI.enabled = !isLoadingRooms &&
                          !isDepositingTokens &&
                          !isCreatingRoom &&
                          currentRunTokens > 0;
            if (GUILayout.Button("Create New Quarry"))
                CreateRoomFromCurrentTokens();

            GUI.enabled = true;
            if (currentRunTokens <= 0)
                GUILayout.Label("Collect at least one artifact to create a quarry.");

            GUILayout.Space(8f);
        }

        GUILayout.Label("Quarries");

        GUILayout.BeginHorizontal();
        GUI.enabled = !isLoadingRooms && !isDepositingTokens && !isCreatingRoom;
        if (GUILayout.Button("By Recent"))
            LoadRooms(BrowseMode.Recent);

        if (GUILayout.Button("By Popular"))
            LoadRooms(BrowseMode.Leaderboard);

        if (GUILayout.Button("Close Menu"))
            CloseBrowser();

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (!string.IsNullOrWhiteSpace(statusMessage))
            GUILayout.Label(statusMessage);

        GUILayout.Space(8f);
        browserScrollPosition = GUILayout.BeginScrollView(browserScrollPosition, GUILayout.Height(scrollHeight));

        if (browserRooms.Count == 0 && !isLoadingRooms)
            GUILayout.Label("No quarries loaded yet.");

        for (var i = 0; i < browserRooms.Count; i++)
        {
            var room = browserRooms[i];
            if (room == null)
                continue;

            var label = string.Format(
                "{0}{1} | {2} artifacts | {3}",
                selectedRoom != null && selectedRoom.id == room.id ? "> " : string.Empty,
                RoomUiFormatter.GetRoomName(room),
                Mathf.Max(0, room.totalTokens),
                GetBrowserRoomTimeLabel(room));

            if (GUILayout.Button(label, GUILayout.Height(32f)))
                ViewRoom(room);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawViewOverlay()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 300f, 16f, 284f, 220f), GUI.skin.window);
        GUILayout.Label(RoomUiFormatter.GetRoomName(selectedRoom));
        GUILayout.Label("Artifacts deposited in this quarry: " + Mathf.Max(0, selectedRoom.totalTokens));

        if (!string.IsNullOrWhiteSpace(statusMessage))
            GUILayout.Label(statusMessage);

        GUI.enabled = !isLoadingRooms && !isDepositingTokens && !isCreatingRoom;
        if (GUILayout.Button("View Other Quarries"))
            OpenBrowser();

        GUI.enabled = !isLoadingRooms && !isDepositingTokens && !isCreatingRoom && currentRunTokens > 0;
        if (GUILayout.Button("Deposit My Artifacts"))
            DepositSelectedRoomTokens();

        GUI.enabled = !isLoadingRooms && !isDepositingTokens && !isCreatingRoom;
        if (GUILayout.Button("Back To Collecting"))
            BackToCollectScene();

        GUI.enabled = true;
        GUILayout.EndArea();
    }

    private static Transform FindTransformInScene(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(objectName))
            return null;

        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            var result = FindTransformRecursive(roots[i].transform, objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static string GetBrowserRoomTimeLabel(RoomDto room)
    {
        if (room == null)
            return "unknown time";

        var timestamp = string.IsNullOrWhiteSpace(room.updatedAt) ? room.createdAt : room.updatedAt;
        return ApiDateUtils.FormatIsoAsRelativeTime(timestamp);
    }

    private static Transform FindTransformRecursive(Transform current, string objectName)
    {
        if (current == null)
            return null;

        if (current.name == objectName)
            return current;

        for (var i = 0; i < current.childCount; i++)
        {
            var result = FindTransformRecursive(current.GetChild(i), objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private void UpsertBrowserRoom(RoomDto room)
    {
        if (room == null)
            return;

        for (var i = 0; i < browserRooms.Count; i++)
        {
            if (browserRooms[i] == null || browserRooms[i].id != room.id)
                continue;

            browserRooms[i] = room;
            return;
        }

        browserRooms.Insert(0, room);
    }

    private string ResolvePendingRoomName()
    {
        return string.IsNullOrWhiteSpace(pendingRoomName)
            ? playerName
            : pendingRoomName.Trim();
    }

    private void SyncPendingRoomName(string suggestedRoomName)
    {
        var sanitizedSuggestion = string.IsNullOrWhiteSpace(suggestedRoomName) ? "Player" : suggestedRoomName.Trim();
        if (string.IsNullOrWhiteSpace(pendingRoomName) || pendingRoomName == lastSuggestedRoomName)
            pendingRoomName = sanitizedSuggestion;

        lastSuggestedRoomName = sanitizedSuggestion;
    }
}
