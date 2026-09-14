using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[AddComponentMenu("Environment/Hazard Auto Binder")]
public class HazardAutoBinder : MonoBehaviour
{
    private static readonly string[] HazardKeywords =
    {
        "lava",
        "magma",
        "spike",
        "thorn",
        "hazard",
        "\u5ca9\u6d46",
        "\u5730\u523a"
    };

    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            subscribed = true;
        }

        BindSceneHazards();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindSceneHazards();
    }

    public static void BindSceneHazards()
    {
        BindTilemapHazards();
        BindColliderHazards();
        PlayerHazardProbe.InstallOnCurrentPlayer();
    }

    private static void BindTilemapHazards()
    {
        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>(true);

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];

            if (tilemap == null || tilemap.GetComponent<TilemapInstantDeathHazard>() != null)
                continue;

            if (LooksLikeHazardTilemap(tilemap))
                tilemap.gameObject.AddComponent<TilemapInstantDeathHazard>();
        }
    }

    private static void BindColliderHazards()
    {
        Collider2D[] colliders = FindObjectsOfType<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];

            if (collider == null ||
                collider.GetComponent<TilemapCollider2D>() != null ||
                collider.GetComponentInParent<Tilemap>() != null ||
                collider.GetComponentInParent<InstantDeathHazard>() != null ||
                collider.GetComponentInParent<Player>() != null ||
                collider.GetComponentInParent<Enemy>() != null)
            {
                continue;
            }

            if (NameContainsHazardKeyword(collider.transform))
                collider.gameObject.AddComponent<InstantDeathHazard>();
        }
    }

    private static bool LooksLikeHazardTilemap(Tilemap tilemap)
    {
        if (tilemap == null)
            return false;

        if (NameContainsHazardKeyword(tilemap.transform))
            return true;

        BoundsInt bounds = tilemap.cellBounds;
        int checkedCells = 0;
        const int maxCheckedCells = 100000;

        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            if (++checkedCells > maxCheckedCells)
                break;

            TileBase tile = tilemap.GetTile(position);

            if (tile != null && ContainsAny(tile.name, HazardKeywords))
                return true;
        }

        return false;
    }

    private static bool NameContainsHazardKeyword(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (ContainsAny(current.name, HazardKeywords))
                return true;
        }

        return false;
    }

    private static bool ContainsAny(string value, params string[] keywords)
    {
        if (string.IsNullOrEmpty(value) || keywords == null)
            return false;

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];

            if (!string.IsNullOrEmpty(keyword) &&
                value.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}
