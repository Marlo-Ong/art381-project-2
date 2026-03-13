using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text playerNameTmpText;
    [SerializeField] private TMP_Text tokenCountTmpText;
    [SerializeField] private Button endRunButton;

    private GameManager gameManager;
    private PlayerSession playerSession;

    private void Awake()
    {
        if (endRunButton != null)
            endRunButton.onClick.AddListener(OnEndRunPressed);
    }

    public void Initialize(GameManager manager, PlayerSession session)
    {
        gameManager = manager;
        playerSession = session;

        if (playerSession != null)
        {
            playerSession.PlayerNameChanged += HandlePlayerNameChanged;
            playerSession.TokenCountChanged += HandleTokenCountChanged;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (endRunButton != null)
            endRunButton.onClick.RemoveListener(OnEndRunPressed);

        if (playerSession != null)
        {
            playerSession.PlayerNameChanged -= HandlePlayerNameChanged;
            playerSession.TokenCountChanged -= HandleTokenCountChanged;
        }
    }

    public void SetVisible(bool isVisible)
    {
        var target = root != null ? root : gameObject;
        target.SetActive(isVisible);

        if (isVisible)
            Refresh();
    }

    public void OnEndRunPressed()
    {
        if (endRunButton != null && !endRunButton.interactable)
        {
            LogFailedRequest(nameof(OnEndRunPressed), "The end run button is not currently interactable.");
            return;
        }

        if (gameManager == null)
        {
            LogFailedRequest(nameof(OnEndRunPressed), "GameManager reference is missing.");
            return;
        }

        gameManager.EndRun();
    }

    private void Refresh()
    {
        if (playerSession == null)
            return;

        UiTextAdapter.SetText(playerNameTmpText, playerSession.PlayerName);
        UiTextAdapter.SetText(tokenCountTmpText, playerSession.CurrentRunTokens.ToString());
    }

    private void HandlePlayerNameChanged(string _)
    {
        Refresh();
    }

    private void HandleTokenCountChanged(int _)
    {
        Refresh();
    }

    private void LogFailedRequest(string requestName, string reason)
    {
        UiRequestLogger.LogFailedRequest(this, nameof(HUDUI), requestName, reason);
    }
}
