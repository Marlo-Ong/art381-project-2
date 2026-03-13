using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockApiRoomService : MonoBehaviour
{
    private const string RoomsResource = "rooms";

    [SerializeField] private ApiConfig apiConfig;
    [SerializeField] private ApiClient apiClient;
    [SerializeField] private bool usePatchForUpdates = true;

    public ApiConfig Config => apiConfig;
    public ApiClient Client => apiClient;

    public IEnumerator CreateRoom(CreateRoomRequest request, Action<RoomDto> onSuccess, Action<string> onError)
    {
        string setupError;
        if (!TryValidateSetup(out setupError))
        {
            onError?.Invoke(setupError);
            yield break;
        }

        ApiClient.ApiResponse response = null;
        yield return apiClient.PostJson(apiConfig.GetUrl(RoomsResource), request, value => response = value);

        RoomDto room;
        if (!TryParseRoomResponse(response, out room, out setupError))
        {
            onError?.Invoke(setupError);
            yield break;
        }

        onSuccess?.Invoke(room);
    }

    public IEnumerator GetRecentRooms(Action<List<RoomDto>> onSuccess, Action<string> onError)
    {
        var limit = apiConfig != null ? apiConfig.RecentPageSize : 20;
        yield return GetRooms("updatedAt", "desc", 1, limit, onSuccess, onError);
    }

    public IEnumerator GetLeaderboardRooms(Action<List<RoomDto>> onSuccess, Action<string> onError)
    {
        var limit = apiConfig != null ? apiConfig.LeaderboardPageSize : 20;
        yield return GetRooms("totalTokens", "desc", 1, limit, onSuccess, onError);
    }

    public IEnumerator GetRoom(string roomId, Action<RoomDto> onSuccess, Action<string> onError)
    {
        string setupError;
        if (!TryValidateSetup(out setupError))
        {
            onError?.Invoke(setupError);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(roomId))
        {
            onError?.Invoke("A room id is required.");
            yield break;
        }

        ApiClient.ApiResponse response = null;
        yield return apiClient.Get(apiConfig.GetUrl(RoomsResource + "/" + roomId.Trim()), value => response = value);

        RoomDto room;
        if (!TryParseRoomResponse(response, out room, out setupError))
        {
            onError?.Invoke(setupError);
            yield break;
        }

        onSuccess?.Invoke(room);
    }

    public IEnumerator UpdateRoom(string roomId, UpdateRoomRequest request, Action<RoomDto> onSuccess, Action<string> onError)
    {
        string setupError;
        if (!TryValidateSetup(out setupError))
        {
            onError?.Invoke(setupError);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(roomId))
        {
            onError?.Invoke("A room id is required.");
            yield break;
        }

        ApiClient.ApiResponse response = null;
        var roomUrl = apiConfig.GetUrl(RoomsResource + "/" + roomId.Trim());
        if (usePatchForUpdates)
            yield return apiClient.PatchJson(roomUrl, request, value => response = value);
        else
            yield return apiClient.PutJson(roomUrl, request, value => response = value);

        RoomDto room;
        if (!TryParseRoomResponse(response, out room, out setupError))
        {
            onError?.Invoke(setupError);
            yield break;
        }

        onSuccess?.Invoke(room);
    }

    public IEnumerator DepositToRoom(string roomId, int tokensToDeposit, Action<RoomDto> onSuccess, Action<string> onError)
    {
        if (tokensToDeposit <= 0)
        {
            onError?.Invoke("You need at least one token to deposit.");
            yield break;
        }

        RoomDto selectedRoom = null;
        string requestError = null;

        yield return GetRoom(roomId, room => selectedRoom = room, error => requestError = error);
        if (!string.IsNullOrWhiteSpace(requestError))
        {
            onError?.Invoke(requestError);
            yield break;
        }

        if (selectedRoom == null)
        {
            onError?.Invoke("The selected room could not be loaded.");
            yield break;
        }

        var updateRequest = new UpdateRoomRequest
        {
            roomName = selectedRoom.roomName,
            totalTokens = Mathf.Max(0, selectedRoom.totalTokens) + tokensToDeposit,
            createdAt = ApiDateUtils.GetExistingIsoOrNow(selectedRoom.createdAt),
            updatedAt = ApiDateUtils.GetCurrentUtcIsoString()
        };

        yield return UpdateRoom(roomId, updateRequest, onSuccess, onError);
    }

    public void Configure(ApiConfig config, ApiClient client, bool patchForUpdates)
    {
        apiConfig = config;
        apiClient = client;
        usePatchForUpdates = patchForUpdates;
    }

    private IEnumerator GetRooms(string sortBy, string order, int page, int limit, Action<List<RoomDto>> onSuccess, Action<string> onError)
    {
        string setupError;
        if (!TryValidateSetup(out setupError))
        {
            onError?.Invoke(setupError);
            yield break;
        }

        var resource = string.Format(
            "{0}?sortBy={1}&order={2}&page={3}&limit={4}",
            RoomsResource,
            sortBy,
            order,
            Mathf.Max(1, page),
            Mathf.Max(1, limit));

        ApiClient.ApiResponse response = null;
        yield return apiClient.Get(apiConfig.GetUrl(resource), value => response = value);

        if (!TryValidateResponse(response, out setupError))
        {
            onError?.Invoke(setupError);
            yield break;
        }

        var rooms = ApiClient.ArrayFromJson<RoomDto>(response.Text);
        onSuccess?.Invoke(new List<RoomDto>(rooms));
    }

    private bool TryValidateSetup(out string error)
    {
        if (apiConfig == null)
        {
            error = "ApiConfig reference is missing.";
            return false;
        }

        if (apiClient == null)
        {
            error = "ApiClient reference is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(apiConfig.BaseUrl))
        {
            error = "MockAPI base URL is empty.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateResponse(ApiClient.ApiResponse response, out string error)
    {
        if (response == null)
        {
            error = "No response received from the server.";
            return false;
        }

        if (!response.Success)
        {
            error = string.IsNullOrWhiteSpace(response.ErrorMessage) ? "Request failed." : response.ErrorMessage;
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseRoomResponse(ApiClient.ApiResponse response, out RoomDto room, out string error)
    {
        room = null;
        if (!TryValidateResponse(response, out error))
            return false;

        room = ApiClient.FromJson<RoomDto>(response.Text);
        if (room == null)
        {
            error = "The server returned an unexpected room payload.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
