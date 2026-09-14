using UnityEngine;

public class DemonBossStats : EnemyStats
{
    private Enemy_DemonBoss boss;

    protected override void Start()
    {
        SetEnemyKind(EnemyKind.赤髓Boss);
        EnsureStatObjects();
        boss = GetComponent<Enemy_DemonBoss>();
        base.Start();
        boss?.SyncStatsForCurrentPhase();
    }

    protected override void Die()
    {
        if (boss == null)
            boss = GetComponent<Enemy_DemonBoss>();

        if (boss != null && boss.IsPhaseTransitionInvulnerable)
            return;

        if (boss != null && boss.TryHandlePhaseOneDefeat())
            return;

        base.Die();
    }

    public override void TakeDamage(int _damage)
    {
        if (ShouldIgnoreDamage())
            return;

        base.TakeDamage(_damage);
    }

    protected override void DecreaseHealthBy(int _damage)
    {
        if (ShouldIgnoreDamage())
            return;

        base.DecreaseHealthBy(_damage);
    }

    protected override void DecreaseHealthByStatusDamage(int _damage)
    {
        if (ShouldIgnoreDamage())
            return;

        base.DecreaseHealthByStatusDamage(_damage);
    }

    public void SetBossHealth(int maxHealthValue)
    {
        EnsureStatObjects();
        int safeMaxHealth = Mathf.Max(1, maxHealthValue);
        maxHealth.SetDefaultValue(safeMaxHealth);
        currentHealth = GetMaxHealthValue();
        onHealthChanged?.Invoke();
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

    private bool ShouldIgnoreDamage()
    {
        if (boss == null)
            boss = GetComponent<Enemy_DemonBoss>();

        return boss != null && boss.IsPhaseTransitionInvulnerable;
    }
}
