using UnityEngine;

internal static class UiRequestLogger
{
    public static void LogFailedRequest(Object context, string source, string requestName, string reason)
    {
        Debug.LogWarning($"[{source}] {requestName} did not succeed: {reason}", context);
    }
}
