using System.Collections.Generic;
using UnityEngine;

public enum EnemyKind
{
    骷髅敌人,
    通用敌人,
    小恶魔,
    混沌狼主,
    赤髓Boss
}

public enum EnemyRank
{
    普通敌人,
    精英敌人,
    小Boss,
    大Boss
}

public class EnemyLevelManager : MonoBehaviour
{
    private const string RuntimeObjectName = "Enemy Level Manager";

    public static EnemyLevelManager instance;

    [Header("默认敌人配置")]
    [SerializeField, Min(1)] private int defaultLevel = 1;
    [SerializeField] private EnemyRank defaultRank = EnemyRank.普通敌人;
    [SerializeField] private bool applyToExistingEnemiesOnAwake = true;

    [Header("按敌人类型配置等级 / 等阶")]
    [SerializeField] private List<EnemyLevelRule> enemyLevelRules = new List<EnemyLevelRule>();

    [Header("按等阶配置余烬掉落")]
    [SerializeField] private List<EnemyRankEmberReward> rankEmberRewards = new List<EnemyRankEmberReward>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureDefaultRules();
        EnsureDefaultRewards();

        if (applyToExistingEnemiesOnAwake)
            ApplySettingsToExistingEnemies();
    }

    public static EnemyLevelManager GetOrCreate()
    {
        if (instance != null)
            return instance;

        EnemyLevelManager existing = FindObjectOfType<EnemyLevelManager>(true);

        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject managerObject = new GameObject(RuntimeObjectName);
        return managerObject.AddComponent<EnemyLevelManager>();
    }

    public void ApplySettingsTo(EnemyStats enemyStats)
    {
        if (enemyStats == null)
            return;

        EnemyLevelRule rule = GetRule(enemyStats.Kind);

        if (rule != null)
            enemyStats.SetManagedLevelAndRank(rule.Level, rule.Rank);
        else
            enemyStats.SetManagedLevelAndRank(defaultLevel, defaultRank);
    }

    public int CalculateEmberReward(EnemyStats enemyStats)
    {
        if (enemyStats == null)
            return 0;

        return CalculateEmberReward(enemyStats.Level, enemyStats.Rank);
    }

    public int CalculateEmberReward(int enemyLevel, EnemyRank enemyRank)
    {
        EnemyRankEmberReward reward = GetReward(enemyRank);

        if (reward == null)
            reward = GetReward(defaultRank);

        if (reward == null)
            return 0;

        int safeLevel = Mathf.Max(1, enemyLevel);
        int rawReward = reward.BaseReward + (safeLevel - 1) * reward.RewardPerLevel;
        return Mathf.Max(0, Mathf.RoundToInt(rawReward * reward.RankMultiplier));
    }

    public void ApplySettingsToExistingEnemies()
    {
        EnemyStats[] enemyStats = FindObjectsOfType<EnemyStats>(true);

        for (int i = 0; i < enemyStats.Length; i++)
            enemyStats[i]?.ApplyManagedLevelSettings();
    }

    private EnemyLevelRule GetRule(EnemyKind enemyKind)
    {
        EnsureDefaultRules();

        for (int i = 0; i < enemyLevelRules.Count; i++)
        {
            EnemyLevelRule rule = enemyLevelRules[i];

            if (rule != null && rule.Kind == enemyKind)
                return rule;
        }

        return null;
    }

    private EnemyRankEmberReward GetReward(EnemyRank enemyRank)
    {
        EnsureDefaultRewards();

        for (int i = 0; i < rankEmberRewards.Count; i++)
        {
            EnemyRankEmberReward reward = rankEmberRewards[i];

            if (reward != null && reward.Rank == enemyRank)
                return reward;
        }

        return null;
    }

    private void EnsureDefaultRules()
    {
        if (enemyLevelRules == null)
            enemyLevelRules = new List<EnemyLevelRule>();

        AddRuleIfMissing(EnemyKind.骷髅敌人, defaultLevel, defaultRank);
        AddRuleIfMissing(EnemyKind.小恶魔, defaultLevel, defaultRank);
        AddRuleIfMissing(EnemyKind.混沌狼主, defaultLevel, EnemyRank.小Boss);
        AddRuleIfMissing(EnemyKind.赤髓Boss, defaultLevel, EnemyRank.大Boss);
    }

    private void AddRuleIfMissing(EnemyKind kind, int level, EnemyRank rank)
    {
        for (int i = 0; i < enemyLevelRules.Count; i++)
        {
            EnemyLevelRule rule = enemyLevelRules[i];

            if (rule != null && rule.Kind == kind)
                return;
        }

        enemyLevelRules.Add(new EnemyLevelRule(kind, level, rank));
    }

    private void EnsureDefaultRewards()
    {
        if (rankEmberRewards == null)
            rankEmberRewards = new List<EnemyRankEmberReward>();

        AddRewardIfMissing(EnemyRank.普通敌人, 8, 3, 1f);
        AddRewardIfMissing(EnemyRank.精英敌人, 24, 8, 1.25f);
        AddRewardIfMissing(EnemyRank.小Boss, 90, 24, 1.5f);
        AddRewardIfMissing(EnemyRank.大Boss, 260, 60, 2f);
    }

    private void AddRewardIfMissing(EnemyRank rank, int baseReward, int rewardPerLevel, float rankMultiplier)
    {
        for (int i = 0; i < rankEmberRewards.Count; i++)
        {
            EnemyRankEmberReward reward = rankEmberRewards[i];

            if (reward != null && reward.Rank == rank)
                return;
        }

        rankEmberRewards.Add(new EnemyRankEmberReward(rank, baseReward, rewardPerLevel, rankMultiplier));
    }

    private void OnValidate()
    {
        defaultLevel = Mathf.Max(1, defaultLevel);
        EnsureDefaultRules();
        EnsureDefaultRewards();

        for (int i = 0; i < enemyLevelRules.Count; i++)
            enemyLevelRules[i]?.Validate();

        for (int i = 0; i < rankEmberRewards.Count; i++)
            rankEmberRewards[i]?.Validate();
    }
}

[System.Serializable]
public class EnemyLevelRule
{
    [SerializeField] private EnemyKind kind;
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField] private EnemyRank rank = EnemyRank.普通敌人;

    public EnemyKind Kind => kind;
    public int Level => Mathf.Max(1, level);
    public EnemyRank Rank => rank;

    public EnemyLevelRule(EnemyKind kind, int level, EnemyRank rank)
    {
        this.kind = kind;
        this.level = Mathf.Max(1, level);
        this.rank = rank;
    }

    public void Validate()
    {
        level = Mathf.Max(1, level);
    }
}

[System.Serializable]
public class EnemyRankEmberReward
{
    [SerializeField] private EnemyRank rank;
    [SerializeField, Min(0)] private int baseReward;
    [SerializeField, Min(0)] private int rewardPerLevel;
    [SerializeField, Min(0f)] private float rankMultiplier = 1f;

    public EnemyRank Rank => rank;
    public int BaseReward => Mathf.Max(0, baseReward);
    public int RewardPerLevel => Mathf.Max(0, rewardPerLevel);
    public float RankMultiplier => Mathf.Max(0f, rankMultiplier);

    public EnemyRankEmberReward(EnemyRank rank, int baseReward, int rewardPerLevel, float rankMultiplier)
    {
        this.rank = rank;
        this.baseReward = Mathf.Max(0, baseReward);
        this.rewardPerLevel = Mathf.Max(0, rewardPerLevel);
        this.rankMultiplier = Mathf.Max(0f, rankMultiplier);
    }

    public void Validate()
    {
        baseReward = Mathf.Max(0, baseReward);
        rewardPerLevel = Mathf.Max(0, rewardPerLevel);
        rankMultiplier = Mathf.Max(0f, rankMultiplier);
    }
}
