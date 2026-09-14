using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Animated Hazard Tile", menuName = "Tiles/Animated Hazard Tile")]
public class AnimatedHazardTile : TileBase
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(0.01f)] private float animationSpeed = 12f;
    [SerializeField, Min(0f)] private float animationStartTimeOffset;
    [SerializeField] private Tile.ColliderType colliderType = Tile.ColliderType.Grid;
    [SerializeField] private HazardContactSettings hazardSettings = HazardContactSettings.LavaDefaults();

    public HazardContactSettings HazardSettings => hazardSettings;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockTransform;
        tileData.colliderType = colliderType;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (frames == null || frames.Length <= 1)
            return false;

        tileAnimationData.animatedSprites = frames;
        tileAnimationData.animationSpeed = animationSpeed;
        tileAnimationData.animationStartTime = animationStartTimeOffset;
        return true;
    }
}
