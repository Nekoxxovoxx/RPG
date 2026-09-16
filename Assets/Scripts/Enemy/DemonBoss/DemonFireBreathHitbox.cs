using System;
using System.Collections.Generic;
using UnityEngine;

// Baked opaque pixel runs use Unity's collision queries without adding physical
// colliders to the boss or retaining readable copies of its animation textures.
public sealed class DemonFireBreathHitbox
{
#pragma warning disable CS0649 // Populated by JsonUtility from the baked TextAsset.
    [Serializable]
    private sealed class Data
    {
        public Frame[] frames;
    }

    [Serializable]
    private sealed class Frame
    {
        public string spriteName;
        public int width;
        public int height;
        public PixelRect[] rectangles;
    }

    [Serializable]
    private struct PixelRect
    {
        public int x;
        public int y;
        public int width;
        public int height;
    }

#pragma warning restore CS0649
    private sealed class Geometry
    {
        public Rect bounds;
        public Rect[] rectangles;
    }

    private readonly Dictionary<string, Frame> frames = new Dictionary<string, Frame>(StringComparer.Ordinal);
    private readonly Dictionary<Sprite, Geometry> geometryBySprite = new Dictionary<Sprite, Geometry>();

    public DemonFireBreathHitbox(TextAsset source)
    {
        if (source == null)
            return;

        Data data = JsonUtility.FromJson<Data>(source.text);
        if (data?.frames == null)
            return;

        foreach (Frame frame in data.frames)
            if (frame != null && !string.IsNullOrEmpty(frame.spriteName))
                frames[frame.spriteName] = frame;
    }

    public bool TryFindPlayer(SpriteRenderer renderer, ContactFilter2D filter, Collider2D[] results, out Player player)
    {
        player = null;
        Geometry geometry = ResolveGeometry(renderer);
        if (geometry == null || geometry.rectangles.Length == 0 || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            return false;

        if (Query(renderer, geometry.bounds, filter, results) == 0)
            return false;

        foreach (Rect rectangle in geometry.rectangles)
        {
            int count = Query(renderer, rectangle, filter, results);
            for (int i = 0; i < count; i++)
            {
                Player candidate = results[i] != null ? results[i].GetComponentInParent<Player>() : null;
                if (candidate != null && candidate.stats != null && !candidate.stats.isDead)
                {
                    player = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    // Also used by the editor regression checks with an isolated physics layer.
    public bool Overlaps(SpriteRenderer renderer, ContactFilter2D filter, Collider2D[] results)
    {
        Geometry geometry = ResolveGeometry(renderer);
        if (geometry == null || geometry.rectangles.Length == 0)
            return false;

        if (Query(renderer, geometry.bounds, filter, results) == 0)
            return false;

        foreach (Rect rectangle in geometry.rectangles)
            if (Query(renderer, rectangle, filter, results) > 0)
                return true;

        return false;
    }

    public void DrawGizmos(SpriteRenderer renderer, Color color)
    {
        Geometry geometry = ResolveGeometry(renderer);
        if (geometry == null)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = renderer.transform.localToWorldMatrix * Matrix4x4.Scale(
            new Vector3(renderer.flipX ? -1f : 1f, renderer.flipY ? -1f : 1f, 1f));
        Gizmos.color = color;
        foreach (Rect rect in geometry.rectangles)
            Gizmos.DrawCube(rect.center, new Vector3(rect.width, rect.height, 0f));
        Gizmos.matrix = previousMatrix;
    }

    private Geometry ResolveGeometry(SpriteRenderer renderer)
    {
        Sprite sprite = renderer != null ? renderer.sprite : null;
        if (sprite == null)
            return null;

        if (geometryBySprite.TryGetValue(sprite, out Geometry cached))
            return cached;

        if (!frames.TryGetValue(sprite.name, out Frame frame) || frame.rectangles == null ||
            frame.width != Mathf.RoundToInt(sprite.rect.width) || frame.height != Mathf.RoundToInt(sprite.rect.height))
            return null;

        Geometry geometry = new Geometry { rectangles = new Rect[frame.rectangles.Length] };
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;
        for (int i = 0; i < frame.rectangles.Length; i++)
        {
            PixelRect pixel = frame.rectangles[i];
            Rect rect = new Rect((pixel.x - pivot.x) / pixelsPerUnit,
                (frame.height - pixel.y - pixel.height - pivot.y) / pixelsPerUnit,
                pixel.width / pixelsPerUnit, pixel.height / pixelsPerUnit);
            geometry.rectangles[i] = rect;
            geometry.bounds = i == 0 ? rect : Rect.MinMaxRect(
                Mathf.Min(geometry.bounds.xMin, rect.xMin), Mathf.Min(geometry.bounds.yMin, rect.yMin),
                Mathf.Max(geometry.bounds.xMax, rect.xMax), Mathf.Max(geometry.bounds.yMax, rect.yMax));
        }

        geometryBySprite[sprite] = geometry;
        return geometry;
    }

    private static int Query(SpriteRenderer renderer, Rect rect, ContactFilter2D filter, Collider2D[] results)
    {
        Vector3 center = rect.center;
        if (renderer.flipX)
            center.x = -center.x;
        if (renderer.flipY)
            center.y = -center.y;
        Vector3 right = renderer.transform.TransformVector(Vector3.right);
        Vector3 up = renderer.transform.TransformVector(Vector3.up);
        float angle = Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg;
        Vector2 size = new Vector2(rect.width * right.magnitude, rect.height * up.magnitude);
        return Physics2D.OverlapBox(renderer.transform.TransformPoint(center), size, angle, filter, results);
    }
}
