using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomDisplayController : MonoBehaviour
{
    [SerializeField] private Transform tokenModel;
    [SerializeField, Min(0.1f)] private float minScaleMultiplier = 0.75f;
    [SerializeField, Min(0.1f)] private float maxScaleMultiplier = 4f;
    [SerializeField, Min(0.01f)] private float scalePerLogStep = 0.75f;
    [SerializeField] private TMP_Text roomNameTmpText;
    [SerializeField] private Text roomNameText;
    [SerializeField] private TMP_Text ownerNameTmpText;
    [SerializeField] private Text ownerNameText;
    [SerializeField] private TMP_Text totalTokensTmpText;
    [SerializeField] private Text totalTokensText;

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
        UiTextAdapter.SetText(roomNameText, roomNameTmpText, RoomUiFormatter.GetRoomName(room));
        UiTextAdapter.SetText(ownerNameText, ownerNameTmpText, RoomUiFormatter.GetOwnerName(room));
        UiTextAdapter.SetText(totalTokensText, totalTokensTmpText, room.totalTokens.ToString());
    }

    public void SetTokenTotal(int totalTokens)
    {
        CacheInitialScale();
        var target = tokenModel != null ? tokenModel : transform;
        var logValue = totalTokens <= 0 ? 0f : Mathf.Log10(totalTokens + 1f);
        var multiplier = Mathf.Clamp(minScaleMultiplier + (logValue * scalePerLogStep), minScaleMultiplier, maxScaleMultiplier);
        target.localScale = initialScale * multiplier;
        UiTextAdapter.SetText(totalTokensText, totalTokensTmpText, Mathf.Max(0, totalTokens).ToString());
    }

    public void Clear()
    {
        SetTokenTotal(0);
        UiTextAdapter.SetText(roomNameText, roomNameTmpText, "Room Preview");
        UiTextAdapter.SetText(ownerNameText, ownerNameTmpText, "Owner");
    }

    private void CacheInitialScale()
    {
        if (cachedScale)
            return;

        var target = tokenModel != null ? tokenModel : transform;
        initialScale = target.localScale;
        cachedScale = true;
    }
}
