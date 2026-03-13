using UnityEngine;

public class TokenCollector : MonoBehaviour
{
    [SerializeField] private PlayerSession playerSession;
    [SerializeField] private GameManager gameManager;

    private void Awake()
    {
        if (playerSession == null)
            playerSession = FindFirstObjectByType<PlayerSession>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.TryGetComponent<TokenPickup>(out var collectible))
            this.TryCollect(collectible);
    }

    public bool TryCollect(TokenPickup pickup)
    {
        if (pickup == null || playerSession == null)
            return false;

        if (gameManager != null && !gameManager.IsSessionRunning)
            return false;

        if (!pickup.Consume())
            return false;

        playerSession.AddTokens(pickup.TokenAmount);
        return true;
    }
}
