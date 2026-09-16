using UnityEngine;

public class BlazingSunRuntimeEffect : MonoBehaviour
{
    private BlazingSun_Effect source;
    private PlayerStats playerStats;
    private Stat[] affectedStats;
    private int[] appliedModifiers;
    private float healthDrainPercentPerSecond;
    private float drainTickInterval;
    private float drainTimer;
    private bool active;

    public void Activate(BlazingSun_Effect effectSource, PlayerStats stats, float statBonusPercent, float drainPercentPerSecond, float tickInterval)
    {
        if (effectSource == null || stats == null)
            return;

        if (active)
            Deactivate(source);

        source = effectSource;
        playerStats = stats;
        affectedStats = GetAffectedStats(stats);
        appliedModifiers = new int[affectedStats.Length];
        healthDrainPercentPerSecond = Mathf.Max(0f, drainPercentPerSecond);
        drainTickInterval = Mathf.Max(0.05f, tickInterval);
        drainTimer = drainTickInterval;

        for (int i = 0; i < affectedStats.Length; i++)
        {
            Stat stat = affectedStats[i];

            if (stat == null)
                continue;

            int value = stat.GetValue();
            int modifier = value == 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(value) * statBonusPercent));

            if (value < 0)
                modifier = -modifier;

            appliedModifiers[i] = modifier;

            if (modifier != 0)
                stat.AddModifier(modifier);
        }

        active = true;
    }

    public void Deactivate(BlazingSun_Effect effectSource)
    {
        if (!active || effectSource != source)
            return;

        int previousMaxHealth = playerStats != null ? playerStats.GetMaxHealthValue() : 0;

        if (affectedStats != null && appliedModifiers != null)
        {
            for (int i = 0; i < affectedStats.Length && i < appliedModifiers.Length; i++)
            {
                if (affectedStats[i] != null && appliedModifiers[i] != 0)
                    affectedStats[i].RemoveModifier(appliedModifiers[i]);
            }
        }

        if (playerStats != null)
            playerStats.IncreaseCurrentHealthByMaxHealthDelta(previousMaxHealth);

        active = false;
        source = null;
        playerStats = null;
        affectedStats = null;
        appliedModifiers = null;
        healthDrainPercentPerSecond = 0f;
        drainTimer = 0f;
    }

    private void Update()
    {
        if (!active || playerStats == null || playerStats.isDead || healthDrainPercentPerSecond <= 0f)
            return;

        drainTimer -= Time.deltaTime;

        if (drainTimer > 0f)
            return;

        drainTimer += drainTickInterval;
        int maxHealth = Mathf.Max(1, playerStats.GetMaxHealthValue());
        int damage = Mathf.Max(1, Mathf.RoundToInt(maxHealth * healthDrainPercentPerSecond * drainTickInterval));
        playerStats.TakeBlazingSunDrainDamage(damage);
    }

    private void OnDestroy()
    {
        if (active)
            Deactivate(source);
    }

    private Stat[] GetAffectedStats(PlayerStats stats)
    {
        return new[]
        {
            stats.strength,
            stats.agility,
            stats.intelligence,
            stats.vitality,
            stats.damage,
            stats.critChance,
            stats.critPower,
            stats.maxHealth,
            stats.armor,
            stats.evasion,
            stats.magicResistance,
            stats.fireDamage,
            stats.iceDamage,
            stats.lightingDamage
        };
    }
}
