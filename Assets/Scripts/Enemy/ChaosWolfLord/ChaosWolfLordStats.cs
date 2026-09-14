using UnityEngine;

[DisallowMultipleComponent]
public class ChaosWolfLordStats : EnemyStats
{
    [Header("Chaos Wolf Lord")]
    [SerializeField, Min(1)] private int defaultMaxHealth = 180;

    private void Awake()
    {
        EnsureInitializedStats();
    }

    protected override void Start()
    {
        SetEnemyKind(EnemyKind.混沌狼主);
        EnsureInitializedStats();

        base.Start();

        // EnemyRank index 2 is the existing small-boss rank. This avoids depending on
        // localized enum member spelling while keeping ember reward logic aligned.
        SetManagedLevelAndRank(Level, (EnemyRank)2);
    }

    public void EnsureInitializedStats()
    {
        EnsureStatObjects();

        if (maxHealth.GetValue() <= 0)
            maxHealth.SetDefaultValue(defaultMaxHealth);
    }

    private void EnsureStatObjects()
    {
        strength ??= new Stat();
        agility ??= new Stat();
        intelligence ??= new Stat();
        vitality ??= new Stat();
        damage ??= new Stat();
        critChance ??= new Stat();
        critPower ??= new Stat();
        maxHealth ??= new Stat();
        armor ??= new Stat();
        evasion ??= new Stat();
        magicResistance ??= new Stat();
        fireDamage ??= new Stat();
        iceDamage ??= new Stat();
        lightingDamage ??= new Stat();
    }
}
