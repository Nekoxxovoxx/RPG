using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parry_Skill : Skill
{
    private const float DefaultCooldown = 2f;

    [Header("Parry")]
    [SerializeField] private UI_SkillTreeSlot parryUnlockButton;
    public bool parryUnlocked;

    [Header("Parry restore")]
    [SerializeField] private UI_SkillTreeSlot restoreUnlockButton;
    public bool restoreUnlocked;

    [Header("Parry with mirage")]
    [SerializeField] private UI_SkillTreeSlot parryWithMirageUnlockButton;
    public bool parryWithMirageUnlocked;

    [Header("Skill Tree Effects")]
    [SerializeField, Range(0f, 1f)] private float restoreMaxHealthPercent = 0.1f;
    [SerializeField, Min(0)] private int bonusParryDamage;

    private void Reset()
    {
        cooldown = DefaultCooldown;
    }

    protected override void Start()
    {
        base.Start();

        if (cooldown <= 0)
            cooldown = DefaultCooldown;
    }

    public bool CanAttemptParry()
    {
        return parryUnlocked && HasLivingPlayer() && IsReady();
    }

    public void StartCooldownOnSuccess()
    {
        if (!HasLivingPlayer())
            return;

        if (cooldown <= 0)
            cooldown = DefaultCooldown;

        StartCooldown();
    }

    public override bool CanUseSkill()
    {
        return CanAttemptParry();
    }

    public override void UseSkill()
    {
        base.UseSkill();
    }

    public void ApplySkillTreeUnlocks(bool parryBaseUnlocked, bool restoreOnSuccessUnlocked, bool damageOnSuccessUnlocked)
    {
        parryUnlocked = parryBaseUnlocked;
        restoreUnlocked = restoreOnSuccessUnlocked;
        parryWithMirageUnlocked = damageOnSuccessUnlocked;
    }

    public void HandleSuccessfulParry(Enemy parriedEnemy)
    {
        ResolvePlayerReference();

        if (!HasLivingPlayer())
            return;

        if (restoreUnlocked)
            RestoreHealth();

        if (parryWithMirageUnlocked)
            DamageParriedEnemy(parriedEnemy);
    }

    private void RestoreHealth()
    {
        PlayerStats playerStats = player != null ? player.stats as PlayerStats : null;

        if (playerStats == null)
            return;

        int healAmount = Mathf.Max(1, Mathf.RoundToInt(playerStats.GetMaxHealthValue() * restoreMaxHealthPercent));
        playerStats.IncreaseHealthBy(healAmount);
    }

    private void DamageParriedEnemy(Enemy parriedEnemy)
    {
        if (parriedEnemy == null || player == null || player.stats == null)
            return;

        EnemyStats targetStats = parriedEnemy.GetComponent<EnemyStats>();

        if (targetStats == null || targetStats.isDead)
            return;

        int damage = player.stats.damage.GetValue() + player.stats.strength.GetValue() + bonusParryDamage;
        targetStats.TakeDamage(Mathf.Max(1, damage));
    }
}
