using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class PixelArtTilemapSeamGuard : MonoBehaviour
{
    [SerializeField] private Vector3 chunkCullingPadding = new Vector3(0.5f, 0.5f, 0f);

    private static PixelArtTilemapSeamGuard instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;

        GameObject guard = new GameObject(nameof(PixelArtTilemapSeamGuard));
        DontDestroyOnLoad(guard);
        instance = guard.AddComponent<PixelArtTilemapSeamGuard>();
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
    }

    private void Start()
    {
        ApplyToSceneTilemaps();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToSceneTilemaps();
    }

    private void ApplyToSceneTilemaps()
    {
        TilemapRenderer[] renderers = FindObjectsOfType<TilemapRenderer>(true);

        foreach (TilemapRenderer tilemapRenderer in renderers)
        {
            if (tilemapRenderer == null)
                continue;

            tilemapRenderer.chunkCullingBounds = chunkCullingPadding;
        }
    }
}
