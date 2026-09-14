using UnityEngine.SceneManagement;

public static class PlayerOpeningAwakingRequest
{
    private static bool hasRequest;
    private static string requestedSceneName;

    public static void Request(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        requestedSceneName = sceneName.Trim();
        hasRequest = true;
    }

    public static bool ConsumeIfMatchesActiveScene()
    {
        if (!hasRequest)
            return false;

        string activeSceneName = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrWhiteSpace(requestedSceneName) &&
            !string.Equals(requestedSceneName, activeSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        hasRequest = false;
        requestedSceneName = null;
        return true;
    }

    public static void Clear()
    {
        hasRequest = false;
        requestedSceneName = null;
    }
}
