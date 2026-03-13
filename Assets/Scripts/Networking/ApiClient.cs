using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    [Serializable]
    public sealed class ApiResponse
    {
        public bool Success;
        public long StatusCode;
        public string Text;
        public string ErrorMessage;
    }

    [Serializable]
    private sealed class ArrayWrapper<T>
    {
        public T[] items;
    }

    [SerializeField, Min(1)] private int requestTimeoutSeconds = 15;

    public int RequestTimeoutSeconds => Mathf.Max(1, requestTimeoutSeconds);

    public IEnumerator Get(string url, Action<ApiResponse> onComplete)
    {
        yield return SendRequest(UnityWebRequest.kHttpVerbGET, url, null, onComplete);
    }

    public IEnumerator PostJson(string url, object requestBody, Action<ApiResponse> onComplete)
    {
        yield return SendRequest(UnityWebRequest.kHttpVerbPOST, url, requestBody, onComplete);
    }

    public IEnumerator PutJson(string url, object requestBody, Action<ApiResponse> onComplete)
    {
        yield return SendRequest(UnityWebRequest.kHttpVerbPUT, url, requestBody, onComplete);
    }

    public IEnumerator PatchJson(string url, object requestBody, Action<ApiResponse> onComplete)
    {
        yield return SendRequest("PATCH", url, requestBody, onComplete);
    }

    public static T FromJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default(T);

        return JsonUtility.FromJson<T>(json);
    }

    public static T[] ArrayFromJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<T>();

        var wrappedJson = "{\"items\":" + json + "}";
        var wrapper = JsonUtility.FromJson<ArrayWrapper<T>>(wrappedJson);
        if (wrapper == null || wrapper.items == null)
            return Array.Empty<T>();

        return wrapper.items;
    }

    public void Configure(int timeoutSeconds)
    {
        requestTimeoutSeconds = Mathf.Max(1, timeoutSeconds);
    }

    private IEnumerator SendRequest(string method, string url, object requestBody, Action<ApiResponse> onComplete)
    {
        var response = new ApiResponse();
        if (string.IsNullOrWhiteSpace(url))
        {
            response.Success = false;
            response.ErrorMessage = "API URL is missing.";
            onComplete?.Invoke(response);
            yield break;
        }

        var jsonBody = requestBody == null ? null : JsonUtility.ToJson(requestBody);

        using (var request = new UnityWebRequest(url, method))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Mathf.Max(1, requestTimeoutSeconds);
            request.SetRequestHeader("Accept", "application/json");

            if (!string.IsNullOrEmpty(jsonBody))
            {
                var payload = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            yield return request.SendWebRequest();

            response.StatusCode = request.responseCode;
            response.Text = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            response.Success = request.result == UnityWebRequest.Result.Success &&
                               request.responseCode >= 200 &&
                               request.responseCode < 300;
            response.ErrorMessage = response.Success ? string.Empty : BuildErrorMessage(request, response.Text);
        }

        onComplete?.Invoke(response);
    }

    private static string BuildErrorMessage(UnityWebRequest request, string responseText)
    {
        var requestError = string.IsNullOrWhiteSpace(request.error) ? "Request failed." : request.error;
        if (!string.IsNullOrWhiteSpace(responseText))
            return request.responseCode + " " + requestError + ": " + responseText;

        return request.responseCode + " " + requestError;
    }
}
