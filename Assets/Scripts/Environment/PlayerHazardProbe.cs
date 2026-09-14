using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Environment/Player Hazard Probe")]
public class PlayerHazardProbe : MonoBehaviour
{
    private const int MaxHits = 32;

    [SerializeField, Min(0.01f)] private float probeInterval = 0.05f;
    [SerializeField, Min(0f)] private float footProbeDepth = 0.22f;
    [SerializeField, Range(0.1f, 1.5f)] private float bodyWidthScale = 0.9f;
    [SerializeField, Range(0.1f, 1.5f)] private float bodyHeightScale = 0.65f;
    [SerializeField] private LayerMask hazardLayerMask = ~0;

    private readonly Collider2D[] hits = new Collider2D[MaxHits];
    private Player player;
    private Collider2D playerCollider;
    private float nextProbeTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        InstallOnCurrentPlayer();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallOnCurrentPlayer();
    }

    public static void InstallOnCurrentPlayer()
    {
        Player currentPlayer = PlayerManager.instance != null
            ? PlayerManager.instance.player
            : FindObjectOfType<Player>(true);

        if (currentPlayer != null && currentPlayer.GetComponent<PlayerHazardProbe>() == null)
            currentPlayer.gameObject.AddComponent<PlayerHazardProbe>();
    }

    private void Awake()
    {
        player = GetComponent<Player>();
        playerCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (Time.time < nextProbeTime)
            return;

        nextProbeTime = Time.time + probeInterval;
        ProbeHazards();
    }

    private void ProbeHazards()
    {
        if (player == null || player.stats == null || player.stats.isDead)
            return;

        if (playerCollider == null)
            playerCollider = GetComponent<Collider2D>();

        if (playerCollider == null)
            return;

        Bounds bodyBounds = BuildBodyProbeBounds(playerCollider.bounds);
        Bounds footBounds = BuildFootProbeBounds(playerCollider.bounds);

        if (ProbeColliderHazards(bodyBounds) || ProbeColliderHazards(footBounds))
            return;

        ProbeTilemapHazards(bodyBounds);
        ProbeTilemapHazards(footBounds);
    }

    private Bounds BuildBodyProbeBounds(Bounds bounds)
    {
        Vector3 size = new Vector3(
            Mathf.Max(0.1f, bounds.size.x * bodyWidthScale),
            Mathf.Max(0.1f, bounds.size.y * bodyHeightScale),
            bounds.size.z);

        Vector3 center = new Vector3(
            bounds.center.x,
            bounds.min.y + size.y * 0.5f,
            bounds.center.z);

        return new Bounds(center, size);
    }

    private Bounds BuildFootProbeBounds(Bounds bounds)
    {
        Vector3 size = new Vector3(
            Mathf.Max(0.1f, bounds.size.x * bodyWidthScale),
            Mathf.Max(0.05f, footProbeDepth),
            bounds.size.z);

        Vector3 center = new Vector3(
            bounds.center.x,
            bounds.min.y - size.y * 0.5f,
            bounds.center.z);

        return new Bounds(center, size);
    }

    private bool ProbeColliderHazards(Bounds bounds)
    {
        int hitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0f, hits, hazardLayerMask);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || hit.GetComponentInParent<Player>() == player)
                continue;

            InstantDeathHazard instantHazard = hit.GetComponentInParent<InstantDeathHazard>();

            if (instantHazard != null && instantHazard.TryApplyTo(player))
                return true;

            TilemapInstantDeathHazard tilemapHazard = hit.GetComponentInParent<TilemapInstantDeathHazard>();

            if (tilemapHazard != null && tilemapHazard.TryApplyToPlayerFromBounds(player, bounds))
                return true;
        }

        return false;
    }

    private void ProbeTilemapHazards(Bounds bounds)
    {
        TilemapInstantDeathHazard[] tilemapHazards = FindObjectsOfType<TilemapInstantDeathHazard>(false);

        for (int i = 0; i < tilemapHazards.Length; i++)
        {
            TilemapInstantDeathHazard tilemapHazard = tilemapHazards[i];

            if (tilemapHazard != null && tilemapHazard.TryApplyToPlayerFromBounds(player, bounds))
                return;
        }
    }
}
