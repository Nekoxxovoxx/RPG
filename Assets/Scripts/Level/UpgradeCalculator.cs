using UnityEngine;

public static class UpgradeCalculator
{
    public static bool IsPrimaryStat(StatType statType)
    {
        return statType == StatType.力量 ||
               statType == StatType.敏捷 ||
               statType == StatType.智慧 ||
               statType == StatType.活力;
    }

    public static int GetCostForNextLevel(int currentLevel, int baseCost, int linearGrowth, int curveGrowth)
    {
        int safeLevel = Mathf.Max(0, currentLevel);
        int safeBaseCost = Mathf.Max(0, baseCost);
        int safeLinearGrowth = Mathf.Max(0, linearGrowth);
        int safeCurveGrowth = Mathf.Max(0, curveGrowth);

        return safeBaseCost + safeLevel * safeLinearGrowth + safeLevel * safeLevel * safeCurveGrowth;
    }

    public static int CalculateTotalUpgradeCost(int currentLevel, int pendingLevelIncrease, int baseCost, int linearGrowth, int curveGrowth)
    {
        if (pendingLevelIncrease <= 0)
            return 0;

        int totalCost = 0;

        for (int i = 0; i < pendingLevelIncrease; i++)
            totalCost += GetCostForNextLevel(currentLevel + i, baseCost, linearGrowth, curveGrowth);

        return totalCost;
    }

    public static int CalculateUpgradeCost(int currentLevel, int pendingLevelIncrease, int baseCost, int costGrowthPerLevel)
    {
        return CalculateTotalUpgradeCost(currentLevel, pendingLevelIncrease, baseCost, costGrowthPerLevel, 0);
    }

    public static int GetCurrentStatValue(PlayerStats stats, StatType statType)
    {
        if (stats == null)
            return 0;

        if (statType == StatType.生命)
            return stats.GetMaxHealthValue();
        if (statType == StatType.攻击力)
            return stats.damage.GetValue() + stats.strength.GetValue();
        if (statType == StatType.暴击伤害)
            return stats.critPower.GetValue() + stats.strength.GetValue();
        if (statType == StatType.暴击几率)
            return stats.critChance.GetValue() + stats.agility.GetValue();
        if (statType == StatType.闪避)
            return stats.evasion.GetValue() + stats.agility.GetValue();
        if (statType == StatType.魔法抗性)
            return stats.magicResistance.GetValue() + stats.intelligence.GetValue() * 3;

        Stat stat = stats.GetStat(statType);
        return stat != null ? stat.GetValue() : 0;
    }

    public static int GetPreviewStatValue(PlayerStats stats, UpgradePreviewData preview, StatType statType)
    {
        int current = GetCurrentStatValue(stats, statType);

        if (stats == null || preview == null)
            return current;

        int pendingStrength = preview.GetPending(StatType.力量);
        int pendingAgility = preview.GetPending(StatType.敏捷);
        int pendingIntelligence = preview.GetPending(StatType.智慧);
        int pendingVitality = preview.GetPending(StatType.活力);

        if (statType == StatType.力量)
            return current + pendingStrength;
        if (statType == StatType.敏捷)
            return current + pendingAgility;
        if (statType == StatType.智慧)
            return current + pendingIntelligence;
        if (statType == StatType.活力)
            return current + pendingVitality;
        if (statType == StatType.生命)
            return current + pendingVitality * 5;
        if (statType == StatType.攻击力)
            return current + pendingStrength;
        if (statType == StatType.暴击伤害)
            return current + pendingStrength;
        if (statType == StatType.暴击几率)
            return current + pendingAgility;
        if (statType == StatType.闪避)
            return current + pendingAgility;
        if (statType == StatType.魔法抗性)
            return current + pendingIntelligence * 3;

        return current;
    }
}
