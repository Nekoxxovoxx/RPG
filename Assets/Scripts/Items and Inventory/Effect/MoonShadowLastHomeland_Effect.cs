using UnityEngine;

[CreateAssetMenu(fileName = "Moon Shadow Last Homeland effect", menuName = "Data/Item effect/Moon Shadow Last Homeland")]
public class MoonShadowLastHomeland_Effect : ItemEffect
{
    [SerializeField, Min(0.1f)] private float survivalDuration = 5f;
    [SerializeField, Range(0f, 1f)] private float healPercentOnKill = 0.1f;

    public override void OnEquip(ItemData_Equipment item, PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        MoonShadowLastHomelandRuntimeEffect runtimeEffect = playerStats.GetComponent<MoonShadowLastHomelandRuntimeEffect>();

        if (runtimeEffect == null)
            runtimeEffect = playerStats.gameObject.AddComponent<MoonShadowLastHomelandRuntimeEffect>();

        runtimeEffect.Activate(this, playerStats, survivalDuration, healPercentOnKill);
    }

    public override void OnUnequip(ItemData_Equipment item, PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        MoonShadowLastHomelandRuntimeEffect runtimeEffect = playerStats.GetComponent<MoonShadowLastHomelandRuntimeEffect>();

        if (runtimeEffect != null)
            runtimeEffect.Deactivate(this);
    }
}
