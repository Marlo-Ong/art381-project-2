using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text playerNameTmpText;
    [SerializeField] private Text playerNameText;
    [SerializeField] private TMP_Text tokenCountTmpText;
    [SerializeField] private Text tokenCountText;
    [SerializeField] private Button endRunButton;

    private GameManager gameManager;
    private PlayerSession playerSession;

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
            return;

        if (gameManager != null)
            gameManager.EndRun();
    }

    private void Refresh()
    {
        if (playerSession == null)
            return;

        UiTextAdapter.SetText(playerNameText, playerNameTmpText, playerSession.PlayerName);
        UiTextAdapter.SetText(tokenCountText, tokenCountTmpText, playerSession.CurrentRunTokens.ToString());
    }

    private void HandlePlayerNameChanged(string _)
    {
        Refresh();
    }

    private void HandleTokenCountChanged(int _)
    {
        Refresh();
    }
}
