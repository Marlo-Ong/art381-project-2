using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItemUI : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject selectedState;
    [SerializeField] private TMP_Text roomNameTmpText;
    [SerializeField] private TMP_Text ownerNameTmpText;
    [SerializeField] private TMP_Text totalTokensTmpText;
    [SerializeField] private TMP_Text updatedAtTmpText;

    private RoomDto roomData;
    private Action<RoomDto, RoomListItemUI> onSelected;

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(HandlePressed);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(HandlePressed);
    }

    public void Bind(RoomDto room, Action<RoomDto, RoomListItemUI> onSelectedCallback)
    {
        roomData = room;
        onSelected = onSelectedCallback;

        UiTextAdapter.SetText(roomNameTmpText, RoomUiFormatter.GetRoomName(roomData));
        UiTextAdapter.SetText(ownerNameTmpText, RoomUiFormatter.GetOwnerName(roomData));
        UiTextAdapter.SetText(totalTokensTmpText, roomData != null ? roomData.totalTokens.ToString() : "0");
        UiTextAdapter.SetText(updatedAtTmpText, roomData != null ? ApiDateUtils.FormatIsoForDisplay(roomData.updatedAt) : "N/A");
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedState != null)
            selectedState.SetActive(isSelected);
    }

    public void OnPressed()
    {
        HandlePressed();
    }

    private void HandlePressed()
    {
        onSelected?.Invoke(roomData, this);
    }
}
