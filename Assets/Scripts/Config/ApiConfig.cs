using UnityEngine;

public class ApiConfig : MonoBehaviour
{
    [SerializeField] private string baseUrl = "https://your-project.mockapi.io/api/v1";
    [SerializeField, Min(1)] private int recentPageSize = 20;
    [SerializeField, Min(1)] private int leaderboardPageSize = 20;

    public string BaseUrl => string.IsNullOrWhiteSpace(baseUrl) ? string.Empty : baseUrl.Trim().TrimEnd('/');
    public int RecentPageSize => Mathf.Max(1, recentPageSize);
    public int LeaderboardPageSize => Mathf.Max(1, leaderboardPageSize);

    public string GetUrl(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(resourcePath))
            return BaseUrl;

        return BaseUrl + "/" + resourcePath.Trim().TrimStart('/');
    }

    public void Configure(string configuredBaseUrl, int configuredRecentPageSize, int configuredLeaderboardPageSize)
    {
        baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? string.Empty
            : configuredBaseUrl.Trim();
        recentPageSize = Mathf.Max(1, configuredRecentPageSize);
        leaderboardPageSize = Mathf.Max(1, configuredLeaderboardPageSize);
    }
}
