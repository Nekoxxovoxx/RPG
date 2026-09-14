using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[RequireComponent(typeof(Tilemap))]
public class TilemapSlopeCollider : MonoBehaviour
{
    private enum SlopeDirection
    {
        None,
        UpRight,
        UpLeft
    }

    [SerializeField] private TileBase[] slopeUpRightTiles;
    [SerializeField] private TileBase[] slopeUpLeftTiles;
    [SerializeField] private string[] slopeUpRightTileNames =
    {
        "GandalfHardcore Hell Tiles 32x32_26",
        "GandalfHardcore Hell Tiles 32x32_54"
    };
    [SerializeField] private string[] slopeUpLeftTileNames =
    {
        "GandalfHardcore Hell Tiles 32x32_27",
        "GandalfHardcore Hell Tiles 32x32_57"
    };
    [SerializeField] private string generatedContainerName = "__GeneratedSlopeColliders";
    [SerializeField, Min(1)] private int stairStepsPerTile = 4;
    [SerializeField, Range(0f, 0.45f)] private float stairTopInsetRatio = 0.2f;
    [SerializeField, Range(0f, 0.25f)] private float stairBottomLiftRatio = 0f;
    [SerializeField] private bool disableOriginalSlopeTileCollider = true;
    [SerializeField] private bool rebuildInEditMode = true;
    [SerializeField] private bool watchTilemapChangesInEditMode = true;
    [SerializeField] private float editModeRebuildInterval = 0.25f;

    private readonly HashSet<TileBase> slopeUpRightSet = new HashSet<TileBase>();
    private readonly HashSet<TileBase> slopeUpLeftSet = new HashSet<TileBase>();
    private Tilemap tilemap;
    private int lastTilemapHash;
    private bool hasTilemapHash;

#if UNITY_EDITOR
    private double nextEditModeCheckTime;
#endif

    private void OnEnable()
    {
        if (Application.isPlaying || rebuildInEditMode)
            Rebuild();
    }

    private void Start()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        RebuildTileSets();

#if UNITY_EDITOR
        if (!Application.isPlaying && rebuildInEditMode)
        {
            UnityEditor.EditorApplication.delayCall -= RebuildInEditor;
            UnityEditor.EditorApplication.delayCall += RebuildInEditor;
        }
#endif
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Application.isPlaying ||
            !rebuildInEditMode ||
            !watchTilemapChangesInEditMode ||
            UnityEditor.EditorApplication.timeSinceStartup < nextEditModeCheckTime)
            return;

        nextEditModeCheckTime = UnityEditor.EditorApplication.timeSinceStartup + editModeRebuildInterval;

        if (HasTilemapChanged())
            Rebuild();
#endif
    }

    [ContextMenu("Rebuild Slope Colliders")]
    public void Rebuild()
    {
        tilemap = GetComponent<Tilemap>();

        if (tilemap == null)
            return;

        RebuildTileSets();
        ClearGeneratedContainer();

        Transform container = CreateGeneratedContainer();
        BoundsInt bounds = tilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(cell);
            SlopeDirection direction = GetSlopeDirection(tile);

            if (direction == SlopeDirection.None)
                continue;

            DisableNativeSlopeCollider(cell);
            CreateSlopeCollider(container, cell, direction);
        }

        ProcessTilemapColliderChanges();
        StoreTilemapHash();
    }

    private void RebuildTileSets()
    {
        slopeUpRightSet.Clear();
        slopeUpLeftSet.Clear();

        AddTilesToSet(slopeUpRightTiles, slopeUpRightSet);
        AddTilesToSet(slopeUpLeftTiles, slopeUpLeftSet);
    }

    private void AddTilesToSet(TileBase[] tiles, HashSet<TileBase> tileSet)
    {
        if (tiles == null)
            return;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] != null)
                tileSet.Add(tiles[i]);
        }
    }

    private SlopeDirection GetSlopeDirection(TileBase tile)
    {
        if (tile == null)
            return SlopeDirection.None;

        if (slopeUpRightSet.Contains(tile) || MatchesTileName(tile, slopeUpRightTileNames))
            return SlopeDirection.UpRight;

        if (slopeUpLeftSet.Contains(tile) || MatchesTileName(tile, slopeUpLeftTileNames))
            return SlopeDirection.UpLeft;

        return SlopeDirection.None;
    }

    public bool IsSlopeTile(TileBase tile)
    {
        return GetSlopeDirection(tile) != SlopeDirection.None;
    }

    private bool MatchesTileName(TileBase tile, string[] names)
    {
        if (tile == null || names == null)
            return false;

        string tileName = tile.name;

        if (string.IsNullOrEmpty(tileName))
            return false;

        for (int i = 0; i < names.Length; i++)
        {
            if (!string.IsNullOrEmpty(names[i]) && tileName == names[i])
                return true;
        }

        return false;
    }

    private bool HasTilemapChanged()
    {
        tilemap = tilemap != null ? tilemap : GetComponent<Tilemap>();

        if (tilemap == null)
            return false;

        int currentHash = CalculateTilemapHash();

        if (!hasTilemapHash)
        {
            lastTilemapHash = currentHash;
            hasTilemapHash = true;
            return false;
        }

        return currentHash != lastTilemapHash;
    }

    private void StoreTilemapHash()
    {
        if (tilemap == null)
            return;

        lastTilemapHash = CalculateTilemapHash();
        hasTilemapHash = true;
    }

    private int CalculateTilemapHash()
    {
        unchecked
        {
            int hash = 17;
            BoundsInt bounds = tilemap.cellBounds;
            hash = hash * 31 + bounds.xMin;
            hash = hash * 31 + bounds.yMin;
            hash = hash * 31 + bounds.zMin;
            hash = hash * 31 + bounds.xMax;
            hash = hash * 31 + bounds.yMax;
            hash = hash * 31 + bounds.zMax;

            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                TileBase tile = tilemap.GetTile(cell);

                if (tile == null)
                    continue;

                hash = hash * 31 + cell.GetHashCode();
                hash = hash * 31 + tile.GetInstanceID();
            }

            return hash;
        }
    }

    private Transform CreateGeneratedContainer()
    {
        GameObject container = new GameObject(generatedContainerName);
        container.layer = gameObject.layer;

        Transform containerTransform = container.transform;
        containerTransform.SetParent(transform, false);
        containerTransform.localPosition = Vector3.zero;
        containerTransform.localRotation = Quaternion.identity;
        containerTransform.localScale = Vector3.one;

        return containerTransform;
    }

    private void ClearGeneratedContainer()
    {
        Transform existing = transform.Find(generatedContainerName);

        if (existing != null)
            DestroyGeneratedObject(existing.gameObject);
    }

    private void CreateSlopeCollider(Transform container, Vector3Int cell, SlopeDirection direction)
    {
        Vector3 cellOrigin = tilemap.CellToLocal(cell);
        Vector3 cellSize = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize : Vector3.one;

        GameObject slopeObject = new GameObject($"Slope_{direction}_{cell.x}_{cell.y}");
        slopeObject.layer = gameObject.layer;

        Transform slopeTransform = slopeObject.transform;
        slopeTransform.SetParent(container, false);
        slopeTransform.localPosition = cellOrigin;
        slopeTransform.localRotation = Quaternion.identity;
        slopeTransform.localScale = Vector3.one;

        PolygonCollider2D slopeCollider = slopeObject.AddComponent<PolygonCollider2D>();
        slopeCollider.isTrigger = false;
        slopeCollider.pathCount = 1;
        slopeCollider.SetPath(0, GetSteppedSlopePath(direction, cellSize));

        StairSurface stairSurface = slopeObject.AddComponent<StairSurface>();
        stairSurface.Setup(direction == SlopeDirection.UpRight ? 1 : -1);
    }

    private void DisableNativeSlopeCollider(Vector3Int cell)
    {
        if (!disableOriginalSlopeTileCollider || tilemap == null)
            return;

        tilemap.SetColliderType(cell, Tile.ColliderType.None);
        tilemap.RefreshTile(cell);
    }

    private void ProcessTilemapColliderChanges()
    {
        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();

        if (tilemapCollider != null)
            tilemapCollider.ProcessTilemapChanges();
    }

    private Vector2[] GetSteppedSlopePath(SlopeDirection direction, Vector3 cellSize)
    {
        float width = Mathf.Abs(cellSize.x);
        float height = Mathf.Abs(cellSize.y);
        int stepCount = Mathf.Max(1, stairStepsPerTile);
        float stepWidth = width / stepCount;
        float bottomLift = height * stairBottomLiftRatio;
        float usableHeight = Mathf.Max(0.05f, height - bottomLift - height * stairTopInsetRatio);
        float stepHeight = usableHeight / stepCount;
        List<Vector2> path = new List<Vector2>(stepCount * 2 + 3)
        {
            Vector2.zero,
            new Vector2(width, 0f)
        };

        if (direction == SlopeDirection.UpRight)
        {
            for (int step = stepCount; step >= 1; step--)
            {
                float stepY = bottomLift + step * stepHeight;
                path.Add(new Vector2(step * stepWidth, stepY));
                path.Add(new Vector2((step - 1) * stepWidth, stepY));
            }

            return path.ToArray();
        }

        for (int step = 1; step <= stepCount; step++)
        {
            float stepY = bottomLift + step * stepHeight;
            path.Add(new Vector2(width - (step - 1) * stepWidth, stepY));
            path.Add(new Vector2(width - step * stepWidth, stepY));
        }

        return path.ToArray();
    }

    private void DestroyGeneratedObject(GameObject target)
    {
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

#if UNITY_EDITOR
    private void RebuildInEditor()
    {
        if (this == null || Application.isPlaying || !isActiveAndEnabled)
            return;

        Rebuild();
    }
#endif
}
