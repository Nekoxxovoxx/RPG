using System.Collections.Generic;
using UnityEngine;

public class AwakeningAuraEffect : MonoBehaviour
{
    [Header("像素描边")]
    [SerializeField, Min(1)] private int outlinePixelWidth = 1;
    [SerializeField] private Color innerGold = new Color(1f, 0.9f, 0.18f, 1f);
    [SerializeField] private Color outerGold = new Color(1f, 0.55f, 0.04f, 0.9f);
    [SerializeField, Min(0f)] private float pulseSpeed = 5.5f;
    [SerializeField, Range(0f, 0.4f)] private float pulseAlpha = 0.12f;
    [SerializeField] private int sortingOrderOffset = -1;

    private readonly List<SpriteRenderer> outlineRenderers = new List<SpriteRenderer>();
    private readonly Vector2Int[] outlineDirections =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    private SpriteRenderer targetRenderer;
    private Material outlineMaterial;

    private void Awake()
    {
        DisableLegacyRenderers();
    }

    private void LateUpdate()
    {
        SyncOutlineRenderers();
    }

    private void OnDestroy()
    {
        if (outlineMaterial != null)
            Destroy(outlineMaterial);
    }

    public void AttachTo(SpriteRenderer sourceRenderer)
    {
        targetRenderer = sourceRenderer;

        if (targetRenderer != null)
        {
            transform.SetParent(targetRenderer.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        EnsureOutlineRenderers();
        SyncOutlineRenderers();
    }

    public void Configure(int pixelWidth, Color newInnerGold, Color newOuterGold)
    {
        outlinePixelWidth = Mathf.Max(1, pixelWidth);
        innerGold = newInnerGold;
        outerGold = newOuterGold;

        EnsureOutlineRenderers();
        SyncOutlineRenderers();
    }

    public void FollowSortingOf(SpriteRenderer sourceRenderer, int orderOffset = -1)
    {
        targetRenderer = sourceRenderer;
        sortingOrderOffset = orderOffset;

        EnsureOutlineRenderers();
        SyncOutlineRenderers();
    }

    private void EnsureOutlineRenderers()
    {
        if (outlineRenderers.Count == outlineDirections.Length)
            return;

        if (outlineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            outlineMaterial = shader != null ? new Material(shader) : null;
        }

        for (int i = outlineRenderers.Count; i < outlineDirections.Length; i++)
        {
            GameObject outlineObject = new GameObject("Awakening Pixel Outline " + i);
            outlineObject.transform.SetParent(transform, false);
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one;

            SpriteRenderer renderer = outlineObject.AddComponent<SpriteRenderer>();

            if (outlineMaterial != null)
                renderer.sharedMaterial = outlineMaterial;

            outlineRenderers.Add(renderer);
        }
    }

    private void SyncOutlineRenderers()
    {
        if (targetRenderer == null)
        {
            SetOutlineVisible(false);
            return;
        }

        EnsureOutlineRenderers();

        Sprite currentSprite = targetRenderer.sprite;

        if (currentSprite == null || !targetRenderer.enabled)
        {
            SetOutlineVisible(false);
            return;
        }

        float pixelSize = outlinePixelWidth / currentSprite.pixelsPerUnit;
        float pulse = 1f - pulseAlpha + Mathf.Sin(Time.time * pulseSpeed) * pulseAlpha;
        Color currentInnerGold = new Color(innerGold.r, innerGold.g, innerGold.b, Mathf.Clamp01(innerGold.a * pulse));
        Color currentOuterGold = new Color(outerGold.r, outerGold.g, outerGold.b, Mathf.Clamp01(outerGold.a * pulse));

        for (int i = 0; i < outlineRenderers.Count; i++)
        {
            SpriteRenderer outlineRenderer = outlineRenderers[i];

            if (outlineRenderer == null)
                continue;

            Vector2Int direction = outlineDirections[i];
            bool diagonal = direction.x != 0 && direction.y != 0;

            outlineRenderer.gameObject.SetActive(true);
            outlineRenderer.sprite = currentSprite;
            outlineRenderer.color = diagonal ? currentOuterGold : currentInnerGold;
            outlineRenderer.flipX = targetRenderer.flipX;
            outlineRenderer.flipY = targetRenderer.flipY;
            outlineRenderer.drawMode = targetRenderer.drawMode;
            outlineRenderer.size = targetRenderer.size;
            outlineRenderer.maskInteraction = targetRenderer.maskInteraction;
            outlineRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            outlineRenderer.sortingOrder = targetRenderer.sortingOrder + sortingOrderOffset;
            outlineRenderer.transform.localPosition = new Vector3(direction.x * pixelSize, direction.y * pixelSize, 0f);
        }
    }

    private void SetOutlineVisible(bool visible)
    {
        for (int i = 0; i < outlineRenderers.Count; i++)
        {
            if (outlineRenderers[i] != null)
                outlineRenderers[i].gameObject.SetActive(visible);
        }
    }

    private void DisableLegacyRenderers()
    {
        LineRenderer legacyRing = GetComponent<LineRenderer>();

        if (legacyRing != null)
            legacyRing.enabled = false;

        Transform legacyGlow = transform.Find("Awakening Glow");

        if (legacyGlow != null)
            legacyGlow.gameObject.SetActive(false);
    }
}
