using UnityEngine;

public enum HazardDamageType
{
    Generic,
    Spike,
    Lava
}

[System.Serializable]
public class HazardContactSettings
{
    [SerializeField] private HazardDamageType hazardType = HazardDamageType.Generic;
    [SerializeField, Min(0)] private int damage = 20;
    [SerializeField, Min(0.01f)] private float contactCooldown = 0.8f;
    [SerializeField, Min(0f)] private float horizontalKnockback = 6f;
    [SerializeField, Min(0f)] private float upwardKnockback = 8f;
    [SerializeField, Min(0f)] private float controlLockDuration = 0.25f;

    public HazardDamageType HazardType => hazardType;
    public int Damage => damage;
    public float ContactCooldown => contactCooldown;
    public float HorizontalKnockback => horizontalKnockback;
    public float UpwardKnockback => upwardKnockback;
    public float ControlLockDuration => controlLockDuration;

    public static HazardContactSettings SpikeDefaults()
    {
        return new HazardContactSettings
        {
            hazardType = HazardDamageType.Spike,
            damage = 25,
            contactCooldown = 0.8f,
            horizontalKnockback = 7.5f,
            upwardKnockback = 8.5f,
            controlLockDuration = 0.25f
        };
    }

    public static HazardContactSettings LavaDefaults()
    {
        return new HazardContactSettings
        {
            hazardType = HazardDamageType.Lava,
            damage = 20,
            contactCooldown = 0.8f,
            horizontalKnockback = 5.5f,
            upwardKnockback = 8f,
            controlLockDuration = 0.25f
        };
    }

    public bool TryApplyTo(Player player, Vector2 sourcePosition)
    {
        if (player == null)
            return false;

        return player.TryApplyHazardContact(
            hazardType,
            damage,
            sourcePosition,
            contactCooldown,
            horizontalKnockback,
            upwardKnockback,
            controlLockDuration);
    }

}
