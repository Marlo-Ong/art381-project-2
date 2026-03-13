using UnityEngine;

public class TokenPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int tokenAmount = 1;
    [SerializeField] private bool destroyOnCollect;

    private bool isCollected;

    public int TokenAmount => Mathf.Max(1, tokenAmount);

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected)
            return;

        var collector = other.GetComponentInParent<TokenCollector>();
        if (collector != null)
            collector.TryCollect(this);
    }

    public bool Consume()
    {
        if (isCollected)
            return false;

        isCollected = true;

        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);

        return true;
    }

    public void ResetPickup()
    {
        isCollected = false;
        if (!destroyOnCollect)
            gameObject.SetActive(true);
    }
}
