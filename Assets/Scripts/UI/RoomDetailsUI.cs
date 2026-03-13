using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomDetailsUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text roomNameTmpText;
    [SerializeField] private Text roomNameText;
    [SerializeField] private TMP_Text ownerNameTmpText;
    [SerializeField] private Text ownerNameText;
    [SerializeField] private TMP_Text totalTokensTmpText;
    [SerializeField] private Text totalTokensText;
    [SerializeField] private TMP_Text createdAtTmpText;
    [SerializeField] private Text createdAtText;
    [SerializeField] private TMP_Text updatedAtTmpText;
    [SerializeField] private Text updatedAtText;
    [SerializeField] private TMP_Text statusTmpText;
    [SerializeField] private Text statusText;
    [SerializeField] private RoomDisplayController roomDisplayController;

    public void SetVisible(bool isVisible)
    {
        var target = root != null ? root : gameObject;
        target.SetActive(isVisible);
    }

    public void ShowRoom(RoomDto room)
    {
        if (room == null)
        {
            Clear();
            return;
        }

        UiTextAdapter.SetText(roomNameText, roomNameTmpText, RoomUiFormatter.GetRoomName(room));
        UiTextAdapter.SetText(ownerNameText, ownerNameTmpText, RoomUiFormatter.GetOwnerName(room));
        UiTextAdapter.SetText(totalTokensText, totalTokensTmpText, room.totalTokens.ToString());
        UiTextAdapter.SetText(createdAtText, createdAtTmpText, ApiDateUtils.FormatIsoForDisplay(room.createdAt));
        UiTextAdapter.SetText(updatedAtText, updatedAtTmpText, ApiDateUtils.FormatIsoForDisplay(room.updatedAt));
        UiTextAdapter.SetText(statusText, statusTmpText, string.Empty);

        if (roomDisplayController != null)
            roomDisplayController.ApplyRoom(room);
    }

    public void Clear()
    {
        UiTextAdapter.SetText(roomNameText, roomNameTmpText, "No room selected");
        UiTextAdapter.SetText(ownerNameText, ownerNameTmpText, "Owner: -");
        UiTextAdapter.SetText(totalTokensText, totalTokensTmpText, "0");
        UiTextAdapter.SetText(createdAtText, createdAtTmpText, "N/A");
        UiTextAdapter.SetText(updatedAtText, updatedAtTmpText, "N/A");
        UiTextAdapter.SetText(statusText, statusTmpText, "Select a room to inspect its totals.");

        if (roomDisplayController != null)
            roomDisplayController.Clear();
    }
}

internal static class RoomUiFormatter
{
    public static string GetRoomName(RoomDto room)
    {
        if (room == null || string.IsNullOrWhiteSpace(room.roomName))
            return "Unnamed Room";

        return room.roomName.Trim();
    }

    public static string GetOwnerName(RoomDto room)
    {
        if (room == null || string.IsNullOrWhiteSpace(room.ownerName))
            return "Anonymous";

        return room.ownerName.Trim();
    }
}
