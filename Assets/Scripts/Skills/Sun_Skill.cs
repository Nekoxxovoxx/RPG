using UnityEngine;
using System.Collections.Generic;

public class Sun_Skill : Skill
{
    [Header("Cast")]
    [SerializeField, Min(0.05f)] private float castDuration = 0.6f;
    [SerializeField] private GameObject sunPrefab;

    [Header("Sun")]
    [SerializeField] private Vector2 spawnOffsetFromPlayerTop = new Vector2(0f, 0.8f);
    [SerializeField, Min(0.1f)] private float maxSunDiameter = 8f;
    [SerializeField, Min(0.05f)] private float growDuration = 0.75f;
    [SerializeField, Min(0f)] private float lifeTime = 5f;

    [Header("Skill Tree")]
    [SerializeField] private bool skillUnlocked;
    [SerializeField, Min(1)] private int baseMaxPullTargets = 5;
    [SerializeField, Min(1)] private int upgradedMaxPullTargets = 10;
    [SerializeField, Min(1f)] private float damageUpgradeMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float durationUpgradeSeconds = 2f;

    private bool damageUpgradeUnlocked;
    private bool durationUpgradeUnlocked;
    private bool maxTargetsUpgradeUnlocked;
    private bool wideDamageUnlocked;
    private readonly List<GameObject> activeSuns = new List<GameObject>();

    public float CastDuration => castDuration;
    public float LifeTime => GetCurrentLifeTime();

    private void Reset()
    {
        cooldown = 12f;
    }

    public bool TryStartCast()
    {
        if (!skillUnlocked)
            return false;

        if (!HasLivingPlayer())
            return false;

        if (!IsReady())
        {
            Debug.Log("Sun skill is cooling down.");
            return false;
        }

        StartCooldown();
        return true;
    }

    public void SpawnSun(Player caster)
    {
        if (sunPrefab == null || caster == null)
            return;

        CharacterStats casterStats = caster.stats != null
            ? caster.stats
            : caster.GetComponent<CharacterStats>();

        if (casterStats != null && casterStats.isDead)
            return;

        Bounds casterBounds = GetCasterBounds(caster);
        float targetDiameter = Mathf.Max(maxSunDiameter, 0.1f);
        Vector3 spawnPosition = new Vector3(
            casterBounds.center.x + spawnOffsetFromPlayerTop.x,
            casterBounds.max.y + targetDiameter * 0.5f + spawnOffsetFromPlayerTop.y,
            caster.transform.position.z);

        GameObject sun = Instantiate(sunPrefab, spawnPosition, Quaternion.identity);
        activeSuns.RemoveAll(instance => instance == null);
        activeSuns.Add(sun);
        SunSkillEffect sunEffect = sun.GetComponent<SunSkillEffect>();

        if (sunEffect != null)
        {
            sunEffect.ConfigureSkillTree(
                maxTargetsUpgradeUnlocked ? upgradedMaxPullTargets : baseMaxPullTargets,
                damageUpgradeUnlocked ? damageUpgradeMultiplier : 1f,
                wideDamageUnlocked);
            sunEffect.Play(targetDiameter, growDuration, GetCurrentLifeTime());
        }
    }

    public void CancelActiveSuns()
    {
        foreach (GameObject sun in activeSuns)
        {
            if (sun == null)
                continue;
            // Release control immediately, before Destroy completes at frame end.
            sun.SetActive(false);
            Destroy(sun);
        }
        activeSuns.Clear();
    }

    private void OnDisable() => CancelActiveSuns();

    public void ApplySkillTreeUnlocks(
        bool sunUnlocked,
        bool damageUpgrade,
        bool durationUpgrade,
        bool maxTargetsUpgrade,
        bool wideDamage)
    {
        skillUnlocked = sunUnlocked;
        damageUpgradeUnlocked = damageUpgrade;
        durationUpgradeUnlocked = durationUpgrade;
        maxTargetsUpgradeUnlocked = maxTargetsUpgrade;
        wideDamageUnlocked = wideDamage;
    }

    private float GetCurrentLifeTime()
    {
        return lifeTime + (durationUpgradeUnlocked ? durationUpgradeSeconds : 0f);
    }

    private Bounds GetCasterBounds(Player caster)
    {
        Collider2D casterCollider = caster.GetComponent<Collider2D>();

        if (casterCollider != null)
            return casterCollider.bounds;

        return new Bounds(caster.transform.position, Vector3.one * 2f);
    }
}
