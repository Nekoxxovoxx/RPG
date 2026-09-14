using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DemonBossProjectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 8f;
    [SerializeField, Min(1)] private int damage = 18;
    [SerializeField, Min(0.1f)] private float lifeTime = 4f;
    [SerializeField, Min(0f)] private float explosionDelay = 0.25f;
    [SerializeField] private string explosionStateName = "projectile_explosion";

    private Enemy_DemonBoss owner;
    private Rigidbody2D rb;
    private Collider2D projectileCollider;
    private Animator animator;
    private bool exploding;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();
        animator = GetComponentInChildren<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        projectileCollider.isTrigger = true;

        EnsureFallbackVisual();
    }

    public void Setup(Enemy_DemonBoss projectileOwner, Vector2 direction, float projectileSpeed, int projectileDamage)
    {
        owner = projectileOwner;
        speed = Mathf.Max(0.1f, projectileSpeed);
        damage = Mathf.Max(1, projectileDamage);

        Vector2 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        rb.velocity = safeDirection * speed;
        transform.right = safeDirection;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (exploding)
            return;

        Player player = collision.GetComponentInParent<Player>();

        if (player == null)
            return;

        if (owner != null && player.TryStartPreciseDodge(owner.transform))
        {
            Explode();
            return;
        }

        PlayerStats playerStats = player.GetComponent<PlayerStats>();

        if (playerStats != null && !playerStats.isDead)
            playerStats.TakeDamage(damage);

        Explode();
    }

    private void Explode()
    {
        exploding = true;

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (projectileCollider != null)
            projectileCollider.enabled = false;

        if (animator != null && animator.HasState(0, Animator.StringToHash(explosionStateName)))
        {
            animator.Play(explosionStateName, 0, 0f);
            Destroy(gameObject, explosionDelay);
            return;
        }

        Destroy(gameObject, explosionDelay);
    }

    private void EnsureFallbackVisual()
    {
        if (GetComponentInChildren<SpriteRenderer>() != null)
            return;

        Texture2D texture = new Texture2D(8, 8);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f));
                Color color = distance < 2.2f ? new Color(1f, 0.85f, 0.15f, 1f) : new Color(1f, 0.18f, 0f, distance < 3.4f ? 1f : 0f);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16);
        GameObject visual = new GameObject("Projectile Visual");
        visual.transform.SetParent(transform, false);

        SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 20;
    }
}
