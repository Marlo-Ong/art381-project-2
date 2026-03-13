using TMPro;
using UnityEngine;

public class RoomDisplayController : MonoBehaviour
{
    [SerializeField] private Transform tokenModel;
    [SerializeField, Min(0.1f)] private float minScaleMultiplier = 0.75f;
    [SerializeField, Min(0.1f)] private float maxScaleMultiplier = 4f;
    [SerializeField, Min(0.01f)] private float scalePerLogStep = 0.75f;
    [SerializeField] private TMP_Text roomNameTmpText;
    [SerializeField] private TMP_Text ownerNameTmpText;
    [SerializeField] private TMP_Text totalTokensTmpText;

    private Vector3 initialScale = Vector3.one;
    private bool cachedScale;

    private void Awake()
    {
        CacheInitialScale();
    }

    public void ApplyRoom(RoomDto room)
    {
        if (room == null)
        {
            Clear();
            return;
        }

        SetTokenTotal(room.totalTokens);
        UiTextAdapter.SetText(roomNameTmpText, RoomUiFormatter.GetRoomName(room));
        UiTextAdapter.SetText(ownerNameTmpText, RoomUiFormatter.GetOwnerName(room));
        UiTextAdapter.SetText(totalTokensTmpText, room.totalTokens.ToString());
    }

    public void SetTokenTotal(int totalTokens)
    {
        CacheInitialScale();
        var target = tokenModel != null ? tokenModel : transform;
        var multiplier = CalculateScaleMultiplier(totalTokens, minScaleMultiplier, maxScaleMultiplier, scalePerLogStep);
        target.localScale = initialScale * multiplier;
        UiTextAdapter.SetText(totalTokensTmpText, Mathf.Max(0, totalTokens).ToString());
    }

    public void Clear()
    {
        SetTokenTotal(0);
        UiTextAdapter.SetText(roomNameTmpText, "Room Preview");
        UiTextAdapter.SetText(ownerNameTmpText, "Owner");
    }

    private void CacheInitialScale()
    {
        if (cachedScale)
            return;

        var target = tokenModel != null ? tokenModel : transform;
        initialScale = target.localScale;
        cachedScale = true;
    }

    public static float CalculateScaleMultiplier(int totalTokens, float minMultiplier, float maxMultiplier, float multiplierPerLogStep)
    {
        var logValue = totalTokens <= 0 ? 0f : Mathf.Log10(totalTokens + 1f);
        return Mathf.Clamp(minMultiplier + (logValue * multiplierPerLogStep), minMultiplier, maxMultiplier);
    }
}
