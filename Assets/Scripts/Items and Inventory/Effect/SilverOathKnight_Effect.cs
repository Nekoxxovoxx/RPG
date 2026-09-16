using UnityEngine;

[CreateAssetMenu(fileName = "Silver Oath Knight effect", menuName = "Data/Item effect/Silver Oath Knight")]
public class SilverOathKnight_Effect : ItemEffect
{
    [SerializeField, Range(0f, 1f)] private float triggerHealthPercent = 0.1f;
    [SerializeField, Range(0f, 1f)] private float restoreHealthPercent = 0.5f;

    public override void OnEquip(ItemData_Equipment item, PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        SilverOathKnightRuntimeEffect runtimeEffect = playerStats.GetComponent<SilverOathKnightRuntimeEffect>();

        if (runtimeEffect == null)
            runtimeEffect = playerStats.gameObject.AddComponent<SilverOathKnightRuntimeEffect>();

        runtimeEffect.Activate(this, playerStats, triggerHealthPercent, restoreHealthPercent);
    }

    public override void OnUnequip(ItemData_Equipment item, PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        SilverOathKnightRuntimeEffect runtimeEffect = playerStats.GetComponent<SilverOathKnightRuntimeEffect>();

        if (runtimeEffect != null)
            runtimeEffect.Deactivate(this);
    }
}
