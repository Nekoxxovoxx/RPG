using UnityEngine;

[CreateAssetMenu(fileName = "Frost Chime mark effect", menuName = "Data/Item effect/Frost Chime Mark")]
public class FrostChimeSeverSnow_Effect : ItemEffect
{
    [SerializeField, Range(0f, 1f)] private float markChance = 0.35f;
    [SerializeField, Min(1)] private int requiredStacks = 5;
    [SerializeField, Min(0f)] private float freezeDuration = 3f;

    public override void ExecuteEffect(Transform _enemyPosition)
    {
        if (_enemyPosition == null || markChance <= 0f)
            return;

        EnemyStats enemyStats = _enemyPosition.GetComponentInParent<EnemyStats>();

        if (!CanAffect(enemyStats))
            return;

        if (Random.value >= markChance)
            return;

        FrostChimeMark mark = enemyStats.GetComponent<FrostChimeMark>();

        if (mark == null)
            mark = enemyStats.gameObject.AddComponent<FrostChimeMark>();

        if (mark.AddStack(requiredStacks))
            FreezeEnemy(enemyStats);
    }

    private bool CanAffect(EnemyStats enemyStats)
    {
        if (enemyStats == null || enemyStats.isDead)
            return false;

        Enemy enemy = enemyStats.GetComponentInParent<Enemy>();

        if (enemy == null)
            enemy = enemyStats.GetComponentInChildren<Enemy>();

        if (enemy == null)
            return false;

        return !(enemy is IGenericControlImmuneEnemy immuneEnemy) ||
               !immuneEnemy.IsImmuneToGenericEnemyControl;
    }

    private void FreezeEnemy(EnemyStats enemyStats)
    {
        Enemy enemy = enemyStats.GetComponentInParent<Enemy>();

        if (enemy == null)
            enemy = enemyStats.GetComponentInChildren<Enemy>();

        if (enemy == null)
            return;

        float duration = Mathf.Max(0f, freezeDuration);

        if (duration > 0f)
            enemy.FreezeTimeFor(duration);

        EntityFX fx = enemyStats.GetComponent<EntityFX>();

        if (fx == null)
            fx = enemyStats.GetComponentInChildren<EntityFX>();

        if (fx == null)
            fx = enemyStats.gameObject.AddComponent<EntityFX>();

        fx.FreezeFxFor(duration);
    }
}
