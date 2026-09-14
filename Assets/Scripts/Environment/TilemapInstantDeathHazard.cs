using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapInstantDeathHazard : MonoBehaviour
{
    [SerializeField] private bool killEveryTile;
    [SerializeField] private bool treatEveryTileAsHazard;
    [SerializeField] private HazardDamageType everyTileHazardType = HazardDamageType.Generic;
    [SerializeField] private TileBase[] lethalTiles;
    [SerializeField] private string[] lethalTileNameKeywords =
    {
        "lava",
        "magma",
        "spike",
        "thorn",
        "\u5ca9\u6d46",
        "\u5730\u523a"
    };
    [SerializeField] private float contactProbeDistance = 0.08f;
    [SerializeField] private HazardContactSettings defaultHazardSettings = HazardContactSettings.LavaDefaults();
    [SerializeField] private HazardContactSettings lavaHazardSettings = HazardContactSettings.LavaDefaults();
    [SerializeField] private HazardContactSettings spikeHazardSettings = HazardContactSettings.SpikeDefaults();

    private readonly HashSet<TileBase> lethalTileSet = new HashSet<TileBase>();
    private Tilemap tilemap;
    private TilemapSlopeCollider slopeCollider;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        slopeCollider = GetComponent<TilemapSlopeCollider>();
        RebuildTileSet();
    }

    private void OnValidate()
    {
        RebuildTileSet();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryApplyHazardFromCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryApplyHazardFromCollision(collision);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyHazardFromCollider(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyHazardFromCollider(other);
    }

    private void TryApplyHazardFromCollision(Collision2D collision)
    {
        Player player = GetPlayer(collision.collider);

        if (player == null)
            return;

        if (TryGetHazardFromCollision(collision, out HazardContactSettings hazardSettings, out Vector2 sourcePosition))
            hazardSettings.TryApplyTo(player, sourcePosition);
    }

    private void TryApplyHazardFromCollider(Collider2D other)
    {
        Player player = GetPlayer(other);

        if (player == null)
            return;

        if (TryGetHazardFromBounds(other.bounds, out HazardContactSettings hazardSettings, out Vector2 sourcePosition))
            hazardSettings.TryApplyTo(player, sourcePosition);
    }

    private bool TryGetHazardFromCollision(Collision2D collision, out HazardContactSettings hazardSettings, out Vector2 sourcePosition)
    {
        int contactCount = collision.contactCount;

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            if (TryGetHazardAtWorld(contact.point, out hazardSettings, out sourcePosition))
                return true;

            Vector2 normalOffset = contact.normal * contactProbeDistance;

            if (TryGetHazardAtWorld(contact.point + normalOffset, out hazardSettings, out sourcePosition))
                return true;

            if (TryGetHazardAtWorld(contact.point - normalOffset, out hazardSettings, out sourcePosition))
                return true;
        }

        hazardSettings = null;
        sourcePosition = default;
        return false;
    }

    private bool TryGetHazardFromBounds(Bounds bounds, out HazardContactSettings hazardSettings, out Vector2 sourcePosition)
    {
        Vector2 center = bounds.center;

        return TryGetHazardAtWorld(center, out hazardSettings, out sourcePosition) ||
               TryGetHazardAtWorld(new Vector2(bounds.min.x, bounds.min.y), out hazardSettings, out sourcePosition) ||
               TryGetHazardAtWorld(new Vector2(bounds.min.x, bounds.max.y), out hazardSettings, out sourcePosition) ||
               TryGetHazardAtWorld(new Vector2(bounds.max.x, bounds.min.y), out hazardSettings, out sourcePosition) ||
               TryGetHazardAtWorld(new Vector2(bounds.max.x, bounds.max.y), out hazardSettings, out sourcePosition) ||
               TryGetHazardAtWorld(new Vector2(bounds.center.x, bounds.min.y), out hazardSettings, out sourcePosition);
    }

    private bool TryGetHazardAtWorld(Vector2 worldPosition, out HazardContactSettings hazardSettings, out Vector2 sourcePosition)
    {
        hazardSettings = null;
        sourcePosition = worldPosition;

        if (tilemap == null)
            return false;

        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
        TileBase tile = tilemap.GetTile(cellPosition);

        if (!TryGetHazardSettingsForTile(tile, out hazardSettings))
            return false;

        sourcePosition = tilemap.GetCellCenterWorld(cellPosition);
        return hazardSettings != null;
    }

    public bool IsLethalAtWorldPosition(Vector2 worldPosition)
    {
        return TryGetHazardAtWorld(worldPosition, out _, out _);
    }

    public bool OverlapsLethalTileBounds(Bounds bounds)
    {
        return TryGetHazardFromBounds(bounds, out _, out _);
    }

    public bool TryApplyToPlayerFromBounds(Player player, Bounds bounds)
    {
        if (player == null)
            return false;

        if (!TryGetHazardFromBounds(bounds, out HazardContactSettings hazardSettings, out Vector2 sourcePosition))
            return false;

        return hazardSettings != null && hazardSettings.TryApplyTo(player, sourcePosition);
    }

    private bool TryGetHazardSettingsForTile(TileBase tile, out HazardContactSettings hazardSettings)
    {
        hazardSettings = null;

        if (tile == null)
            return false;

        if (slopeCollider == null)
            slopeCollider = GetComponent<TilemapSlopeCollider>();

        if (slopeCollider != null && slopeCollider.IsSlopeTile(tile))
            return false;

        if (tile is AnimatedHazardTile animatedHazardTile)
        {
            hazardSettings = animatedHazardTile.HazardSettings;
            return hazardSettings != null;
        }

        if (killEveryTile || treatEveryTileAsHazard)
        {
            hazardSettings = GetEveryTileHazardSettings();
            return hazardSettings != null;
        }

        if (!lethalTileSet.Contains(tile) && !NameContainsHazardKeyword(tile.name))
            return false;

        hazardSettings = GetSettingsForTileName(tile.name);
        return hazardSettings != null;
    }

    private HazardContactSettings GetSettingsForTileName(string tileName)
    {
        if (NameContainsAny(tileName, "lava", "magma", "\u5ca9\u6d46"))
            return lavaHazardSettings;

        if (NameContainsAny(tileName, "spike", "thorn", "\u5730\u523a"))
            return spikeHazardSettings;

        return defaultHazardSettings;
    }

    private HazardContactSettings GetEveryTileHazardSettings()
    {
        switch (everyTileHazardType)
        {
            case HazardDamageType.Spike:
                return spikeHazardSettings;
            case HazardDamageType.Lava:
                return lavaHazardSettings;
            default:
                return defaultHazardSettings;
        }
    }

    private bool NameContainsHazardKeyword(string tileName)
    {
        if (string.IsNullOrEmpty(tileName) || lethalTileNameKeywords == null)
            return false;

        for (int i = 0; i < lethalTileNameKeywords.Length; i++)
        {
            string keyword = lethalTileNameKeywords[i];

            if (!string.IsNullOrEmpty(keyword) &&
                tileName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private bool NameContainsAny(string tileName, params string[] keywords)
    {
        if (string.IsNullOrEmpty(tileName) || keywords == null)
            return false;

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];

            if (!string.IsNullOrEmpty(keyword) &&
                tileName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private Player GetPlayer(Collider2D other)
    {
        return other != null ? other.GetComponentInParent<Player>() : null;
    }

    private void RebuildTileSet()
    {
        lethalTileSet.Clear();

        if (lethalTiles == null)
            return;

        for (int i = 0; i < lethalTiles.Length; i++)
        {
            if (lethalTiles[i] != null)
                lethalTileSet.Add(lethalTiles[i]);
        }
    }
}
