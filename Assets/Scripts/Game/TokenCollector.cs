using UnityEngine;

public class TokenCollector : MonoBehaviour
{
    [SerializeField] private PlayerSession playerSession;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RootSoundEffectPlayer soundEffectPlayer;

    private void Awake()
    {
        if (playerSession == null)
            playerSession = FindFirstObjectByType<PlayerSession>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (soundEffectPlayer == null)
            soundEffectPlayer = RootSoundEffectPlayer.FindInstance();
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

        if (!pickup.Consume())
            return false;

        playerSession.AddTokens(pickup.TokenAmount);
        PlayCollectedTokenSound();
        return true;
    }

    private void PlayCollectedTokenSound()
    {
        if (soundEffectPlayer == null)
            soundEffectPlayer = RootSoundEffectPlayer.FindInstance();

        if (soundEffectPlayer != null)
            soundEffectPlayer.PlaySoundEffect(RootSoundEffectPlayer.TokenCollectedSoundEffectIndex);
    }
}
