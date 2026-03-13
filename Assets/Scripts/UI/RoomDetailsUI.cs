using TMPro;
using UnityEngine;

public class RoomDetailsUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text roomNameTmpText;
    [SerializeField] private TMP_Text totalTokensTmpText;
    [SerializeField] private TMP_Text createdAtTmpText;
    [SerializeField] private TMP_Text updatedAtTmpText;
    [SerializeField] private TMP_Text statusTmpText;
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

        UiTextAdapter.SetText(roomNameTmpText, RoomUiFormatter.GetRoomName(room));
        UiTextAdapter.SetText(totalTokensTmpText, room.totalTokens.ToString());
        UiTextAdapter.SetText(createdAtTmpText, ApiDateUtils.FormatIsoForDisplay(room.createdAt));
        UiTextAdapter.SetText(updatedAtTmpText, ApiDateUtils.FormatIsoForDisplay(room.updatedAt));
        UiTextAdapter.SetText(statusTmpText, string.Empty);

        if (roomDisplayController != null)
            roomDisplayController.ApplyRoom(room);
    }

    public void Clear()
    {
        UiTextAdapter.SetText(roomNameTmpText, "No room selected");
        UiTextAdapter.SetText(totalTokensTmpText, "0");
        UiTextAdapter.SetText(createdAtTmpText, "N/A");
        UiTextAdapter.SetText(updatedAtTmpText, "N/A");
        UiTextAdapter.SetText(statusTmpText, "Select a room to inspect its totals.");

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
}
