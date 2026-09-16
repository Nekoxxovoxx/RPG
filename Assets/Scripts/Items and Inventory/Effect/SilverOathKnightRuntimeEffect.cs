using UnityEngine;

[DisallowMultipleComponent]
public class SilverOathKnightRuntimeEffect : MonoBehaviour
{
    private SilverOathKnight_Effect source;
    private PlayerStats playerStats;
    private float triggerHealthPercent;
    private float restoreHealthPercent;
    private bool active;
    private bool usedThisRest;
    private bool handlingHealthChange;
    private int lastResetRestVersion = -1;

    public void Activate(SilverOathKnight_Effect effectSource, PlayerStats stats, float triggerPercent, float restorePercent)
    {
        if (effectSource == null || stats == null)
            return;

        if (active)
            Deactivate(source);

        source = effectSource;
        playerStats = stats;
        triggerHealthPercent = Mathf.Clamp01(triggerPercent);
        restoreHealthPercent = Mathf.Clamp01(restorePercent);
        active = true;
        RefreshRestState();

        playerStats.onHealthChanged -= HandleHealthChanged;
        playerStats.onHealthChanged += HandleHealthChanged;
        WorldRestManager.PlayerReturnedToRest -= HandlePlayerReturnedToRest;
        WorldRestManager.PlayerReturnedToRest += HandlePlayerReturnedToRest;

        HandleHealthChanged();
    }

    public void Deactivate(SilverOathKnight_Effect effectSource)
    {
        if (!active || effectSource != source)
            return;

        if (playerStats != null)
            playerStats.onHealthChanged -= HandleHealthChanged;

        WorldRestManager.PlayerReturnedToRest -= HandlePlayerReturnedToRest;

        active = false;
        source = null;
        playerStats = null;
        handlingHealthChange = false;
    }

    private void HandleHealthChanged()
    {
        if (!active || usedThisRest || handlingHealthChange || playerStats == null || playerStats.isDead)
            return;

        int maxHealth = Mathf.Max(1, playerStats.GetMaxHealthValue());
        int triggerHealth = Mathf.Max(1, Mathf.CeilToInt(maxHealth * triggerHealthPercent));

        if (playerStats.currentHealth > triggerHealth)
            return;

        usedThisRest = true;
        handlingHealthChange = true;

        int restoreHealth = Mathf.Clamp(Mathf.CeilToInt(maxHealth * restoreHealthPercent), 1, maxHealth);
        playerStats.currentHealth = restoreHealth;
        playerStats.onHealthChanged?.Invoke();

        handlingHealthChange = false;
    }

    private void HandlePlayerReturnedToRest(Player player)
    {
        if (playerStats == null || player == null || player.stats != playerStats)
            return;

        lastResetRestVersion = WorldRestManager.RestVersion;
        usedThisRest = false;
    }

    private void RefreshRestState()
    {
        if (lastResetRestVersion == WorldRestManager.RestVersion)
            return;

        lastResetRestVersion = WorldRestManager.RestVersion;
        usedThisRest = false;
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.onHealthChanged -= HandleHealthChanged;

        WorldRestManager.PlayerReturnedToRest -= HandlePlayerReturnedToRest;
    }
}
