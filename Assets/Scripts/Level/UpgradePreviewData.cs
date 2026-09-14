public class UpgradePreviewData
{
    public int Strength { get; private set; }
    public int Agility { get; private set; }
    public int Intelligence { get; private set; }
    public int Vitality { get; private set; }

    public int TotalPendingPoints => Strength + Agility + Intelligence + Vitality;

    public void AddPending(StatType statType)
    {
        if (statType == StatType.力量)
            Strength++;
        else if (statType == StatType.敏捷)
            Agility++;
        else if (statType == StatType.智慧)
            Intelligence++;
        else if (statType == StatType.活力)
            Vitality++;
    }

    public bool RemovePending(StatType statType)
    {
        if (statType == StatType.力量 && Strength > 0)
        {
            Strength--;
            return true;
        }

        if (statType == StatType.敏捷 && Agility > 0)
        {
            Agility--;
            return true;
        }

        if (statType == StatType.智慧 && Intelligence > 0)
        {
            Intelligence--;
            return true;
        }

        if (statType == StatType.活力 && Vitality > 0)
        {
            Vitality--;
            return true;
        }

        return false;
    }

    public int GetPending(StatType statType)
    {
        if (statType == StatType.力量)
            return Strength;
        if (statType == StatType.敏捷)
            return Agility;
        if (statType == StatType.智慧)
            return Intelligence;
        if (statType == StatType.活力)
            return Vitality;

        return 0;
    }

    public void Clear()
    {
        Strength = 0;
        Agility = 0;
        Intelligence = 0;
        Vitality = 0;
    }
}
