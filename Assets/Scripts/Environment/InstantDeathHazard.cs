using UnityEngine;

public class InstantDeathHazard : MonoBehaviour
{
    [SerializeField] private HazardContactSettings hazardSettings = HazardContactSettings.SpikeDefaults();
    [SerializeField] private bool makeColliderTriggerOnReset = true;
    [SerializeField] private bool autoSelectSettingsFromName = true;

    private void Awake()
    {
        AutoSelectSettings();
    }

    private void Reset()
    {
        AutoSelectSettings();

        Collider2D hazardCollider = GetComponent<Collider2D>();

        if (hazardCollider != null && makeColliderTriggerOnReset)
            hazardCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ApplyHazard(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ApplyHazard(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ApplyHazard(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ApplyHazard(collision.collider);
    }

    private void ApplyHazard(Collider2D other)
    {
        Player player = other != null ? other.GetComponentInParent<Player>() : null;

        TryApplyTo(player);
    }

    public bool TryApplyTo(Player player)
    {
        if (player == null)
            return false;

        if (hazardSettings == null)
            return false;

        return hazardSettings.TryApplyTo(player, GetHazardSourcePosition(player));
    }

    private Vector2 GetHazardSourcePosition(Player player)
    {
        Collider2D hazardCollider = GetComponent<Collider2D>();

        if (hazardCollider == null || player == null)
            return transform.position;

        return hazardCollider.ClosestPoint(player.transform.position);
    }

    private void AutoSelectSettings()
    {
        if (!autoSelectSettingsFromName)
            return;

        string objectName = gameObject.name;

        for (Transform current = transform.parent; current != null; current = current.parent)
            objectName += current.name;

        if (ContainsAny(objectName, "lava", "magma", "岩浆"))
            hazardSettings = HazardContactSettings.LavaDefaults();
        else if (ContainsAny(objectName, "spike", "thorn", "地刺"))
            hazardSettings = HazardContactSettings.SpikeDefaults();
    }

    private bool ContainsAny(string value, params string[] keywords)
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
