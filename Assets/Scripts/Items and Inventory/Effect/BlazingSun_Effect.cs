using UnityEngine;

[CreateAssetMenu(fileName = "Blazing Sun effect", menuName = "Data/Item effect/Blazing Sun")]
public class BlazingSun_Effect : ItemEffect
{
    [SerializeField, Range(0f, 1f)] private float statBonusPercent = 0.1f;
    [SerializeField, Range(0f, 1f)] private float healthDrainPercentPerSecond = 0.02f;
    [SerializeField, Min(0.05f)] private float drainTickInterval = 1f;

    public override void OnEquip(ItemData_Equipment item, PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        BlazingSunRuntimeEffect runtimeEffect = playerStats.GetComponent<BlazingSunRuntimeEffect>();

        if (runtimeEffect == null)
            runtimeEffect = playerStats.gameObject.AddComponent<BlazingSunRuntimeEffect>();

        runtimeEffect.Activate(this, playerStats, statBonusPercent, healthDrainPercentPerSecond, drainTickInterval);
    }

    public override void OnUnequip(ItemData_Equipment item, PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        BlazingSunRuntimeEffect runtimeEffect = playerStats.GetComponent<BlazingSunRuntimeEffect>();

        if (runtimeEffect != null)
            runtimeEffect.Deactivate(this);
    }
}
