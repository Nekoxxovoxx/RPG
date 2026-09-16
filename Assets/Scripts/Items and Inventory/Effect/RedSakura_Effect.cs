using UnityEngine;

[CreateAssetMenu(fileName = "Red Sakura mark effect", menuName = "Data/Item effect/Red Sakura mark")]
public class RedSakura_Effect : ItemEffect
{
    private const int EliteEnemyRankIndex = 1;

    [SerializeField, Range(0f, 1f)] private float markChance = 0.35f;
    [SerializeField, Min(1)] private int requiredStacks = 5;
    [SerializeField, Range(0f, 1f)] private float maxHealthDamagePercent = 0.1f;

    public override void ExecuteEffect(Transform _enemyPosition)
    {
        if (_enemyPosition == null)
            return;

        EnemyStats enemyStats = _enemyPosition.GetComponentInParent<EnemyStats>();

        if (enemyStats == null || enemyStats.isDead || !CanAffect(enemyStats))
            return;

        if (markChance <= 0f || Random.value >= markChance)
            return;

        RedSakuraMark mark = enemyStats.GetComponent<RedSakuraMark>();

        if (mark == null)
            mark = enemyStats.gameObject.AddComponent<RedSakuraMark>();

        if (mark.AddStack(requiredStacks))
            DealScarletBloomDamage(enemyStats);
    }

    private bool CanAffect(EnemyStats enemyStats)
    {
        return enemyStats != null && (int)enemyStats.Rank <= EliteEnemyRankIndex &&
               enemyStats.GetComponent<Enemy_ChaosWolfLord>() == null &&
               enemyStats.GetComponent<Enemy_DemonBoss>() == null;
    }

    private void DealScarletBloomDamage(EnemyStats enemyStats)
    {
        int maxHealth = Mathf.Max(1, enemyStats.GetMaxHealthValue());
        int damage = Mathf.Max(1, Mathf.RoundToInt(maxHealth * maxHealthDamagePercent));

        DamageAttribution.RunAsPlayerDamage(() => enemyStats.TakeDamage(damage));
    }
}
