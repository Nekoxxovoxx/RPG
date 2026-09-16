using System.Collections;
using UnityEngine;

public class MoonShadowLastHomelandRuntimeEffect : MonoBehaviour
{
    private MoonShadowLastHomeland_Effect source;
    private PlayerStats playerStats;
    private Coroutine deathWindowRoutine;
    private float survivalDuration;
    private float healPercentOnKill;
    private bool equipped;
    private bool defyingDeath;
    public bool IsDefyingDeath => defyingDeath;

    public void CancelDeathWindow()
    {
        defyingDeath = false;
        StopDeathWindowRoutine();
        EnemyStats.AnyEnemyKilledByPlayer -= HandleEnemyDied;
    }

    public void Activate(MoonShadowLastHomeland_Effect effectSource, PlayerStats stats, float duration, float healPercent)
    {
        source = effectSource;
        playerStats = stats;
        survivalDuration = Mathf.Max(0.1f, duration);
        healPercentOnKill = Mathf.Clamp01(healPercent);
        equipped = source != null && playerStats != null;
    }

    public void Deactivate(MoonShadowLastHomeland_Effect effectSource)
    {
        if (effectSource != source)
            return;

        equipped = false;

        if (defyingDeath)
            ForceDelayedDeath();

        Cleanup();
    }

    public bool TryPreventDeath()
    {
        if (!equipped || playerStats == null || playerStats.isDead)
            return false;

        if (defyingDeath)
        {
            ClampHealthToZero();
            return true;
        }

        BeginDeathWindow();
        return true;
    }

    private void BeginDeathWindow()
    {
        defyingDeath = true;
        ClampHealthToZero();

        EnemyStats.AnyEnemyKilledByPlayer -= HandleEnemyDied;
        EnemyStats.AnyEnemyKilledByPlayer += HandleEnemyDied;

        if (deathWindowRoutine != null)
            StopCoroutine(deathWindowRoutine);

        deathWindowRoutine = StartCoroutine(DeathWindowRoutine());
    }

    private IEnumerator DeathWindowRoutine()
    {
        yield return new WaitForSeconds(survivalDuration);

        if (defyingDeath)
            ForceDelayedDeath();
    }

    private void HandleEnemyDied(EnemyStats enemyStats)
    {
        if (!defyingDeath || enemyStats == null || !enemyStats.WasKilledByPlayer)
            return;

        ResolveWithKill();
    }

    private void ResolveWithKill()
    {
        defyingDeath = false;
        StopDeathWindowRoutine();
        EnemyStats.AnyEnemyKilledByPlayer -= HandleEnemyDied;

        if (playerStats == null)
            return;

        int maxHealth = Mathf.Max(1, playerStats.GetMaxHealthValue());
        int healAmount = Mathf.Max(1, Mathf.RoundToInt(maxHealth * healPercentOnKill));
        playerStats.ReviveWithHealth(healAmount);
    }

    private void ForceDelayedDeath()
    {
        defyingDeath = false;
        StopDeathWindowRoutine();
        EnemyStats.AnyEnemyKilledByPlayer -= HandleEnemyDied;

        if (playerStats != null)
            playerStats.ForceMoonShadowDelayedDeath(this);
    }

    private void StopDeathWindowRoutine()
    {
        if (deathWindowRoutine == null)
            return;

        StopCoroutine(deathWindowRoutine);
        deathWindowRoutine = null;
    }

    private void Cleanup()
    {
        StopDeathWindowRoutine();
        EnemyStats.AnyEnemyKilledByPlayer -= HandleEnemyDied;
        source = null;
        playerStats = null;
    }

    private void ClampHealthToZero()
    {
        if (playerStats == null)
            return;

        playerStats.currentHealth = 0;
        playerStats.onHealthChanged?.Invoke();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnDisable()
    {
        if (defyingDeath && playerStats != null && !playerStats.isDead)
            ForceDelayedDeath();
        CancelDeathWindow();
    }
}
