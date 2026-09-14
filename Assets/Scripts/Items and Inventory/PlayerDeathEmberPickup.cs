using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerDeathEmberPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private LevelCurrencyData emberData;
    [SerializeField, Min(0)] private int amount;
    [SerializeField] private string pickupPrompt = "按 E 拾取";
    [SerializeField] private float autoPickupDelay = 0.25f;

    private bool picked;
    private float spawnTime;

    public string InteractionPrompt
    {
        get
        {
            string displayName = emberData != null ? emberData.DisplayName : "余烬";
            return $"{pickupPrompt} {amount} {displayName}";
        }
    }

    public Transform InteractionTransform => transform;

    private void Awake()
    {
        spawnTime = Time.time;

        Collider2D pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
    }

    public void Setup(LevelCurrencyData data, int emberAmount)
    {
        emberData = data;
        amount = Mathf.Max(0, emberAmount);
        ApplyVisual();
        gameObject.name = $"死亡余烬 x{amount}";
    }

    public bool CanInteract(Player player)
    {
        return !picked && amount > 0 && player != null && (player.stats == null || !player.stats.isDead);
    }

    public void Interact(Player player)
    {
        TryPickup(player);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < spawnTime + autoPickupDelay)
            return;

        Player player = other.GetComponentInParent<Player>();
        TryPickup(player);
    }

    private void TryPickup(Player player)
    {
        if (!CanInteract(player))
            return;

        picked = true;
        PlayerEmberWallet.GetOrCreate().AddEmbers(amount);
        Destroy(gameObject);
    }

    private void ApplyVisual()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (emberData != null && emberData.Icon != null)
            spriteRenderer.sprite = emberData.Icon;

        spriteRenderer.sortingOrder = 4;
    }
}
