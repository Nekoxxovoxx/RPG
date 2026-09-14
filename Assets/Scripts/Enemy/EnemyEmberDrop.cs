using UnityEngine;

public class EnemyEmberDrop : MonoBehaviour
{
    [Header("余烬掉落")]
    [SerializeField] private bool useManagerFormula = true;
    [SerializeField, Min(0)] private int fixedReward;
    [SerializeField, Min(0)] private int randomBonusMax;
    [SerializeField, Min(0f)] private float rewardMultiplier = 1f;
    [SerializeField] private bool logReward;

    private bool dropped;

    public void DropEmbers(EnemyStats enemyStats)
    {
        if (dropped)
            return;

        dropped = true;

        int reward = CalculateReward(enemyStats);

        if (reward <= 0)
            return;

        PlayerEmberWallet.GetOrCreate().AddEmbers(reward);

        if (logReward)
            Debug.Log($"{gameObject.name} 掉落余烬：{reward}");
    }

    public int CalculateReward(EnemyStats enemyStats)
    {
        int reward = fixedReward;

        if (useManagerFormula)
        {
            EnemyLevelManager manager = EnemyLevelManager.GetOrCreate();
            reward = manager != null ? manager.CalculateEmberReward(enemyStats) : 0;
        }

        if (randomBonusMax > 0)
            reward += Random.Range(0, randomBonusMax + 1);

        return Mathf.Max(0, Mathf.RoundToInt(reward * rewardMultiplier));
    }

    private void OnEnable()
    {
        dropped = false;
    }

    private void OnValidate()
    {
        fixedReward = Mathf.Max(0, fixedReward);
        randomBonusMax = Mathf.Max(0, randomBonusMax);
        rewardMultiplier = Mathf.Max(0f, rewardMultiplier);
    }
}
