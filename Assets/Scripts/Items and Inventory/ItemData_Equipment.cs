using System.Collections.Generic;
using UnityEngine;

public enum EquipmentType
{
    Weapon,
    Armor,
    Amulet,
    Flask
}
[CreateAssetMenu(fileName = "New Item Data", menuName = "Data/Equipment")]
public class ItemData_Equipment : ItemData
{
    public EquipmentType equipmentType;

    public float itemCooldown;
    public ItemEffect[] itemEffects;

    [Header("特殊效果")]
    [SerializeField, TextArea(2, 5), InspectorName("特殊效果描述")]
    private string specialEffectDescription;

    [Header("主要属性")]
    public int strength;
    public int agility;
    public int intelligence;
    public int vitality;
    [Header("伤害属性")]
    public int damage;
    public int critChance;
    public int critPower;
    [Header("防御属性")]
    public int health;
    public int armor;
    public int evasion;
    public int magicResistance;
    [Header("魔法属性")]
    public int fireDamage;
    public int iceDamage;
    public int lightingDamage;

    [Header("制作要求")]
    public List<InventoryItem> craftingMaterials;

    private int descriptionLength;

    public string SpecialEffectDescription => specialEffectDescription;

    public void Effect(Transform _enemyPosition)
    {
        foreach (var item in itemEffects)
        {
            item.ExecuteEffect(_enemyPosition);
        }
    }
    public void AddModifiers()
    {
        PlayerStats playerStats = PlayerManager.instance.player.GetComponent<PlayerStats>();
        playerStats.strength.AddModifier(strength);
        playerStats.agility.AddModifier(agility);
        playerStats.intelligence.AddModifier(intelligence);
        playerStats.vitality.AddModifier(vitality);

        playerStats.damage.AddModifier(damage);
        playerStats.critChance.AddModifier(critChance);
        playerStats.critPower.AddModifier(critPower);

        playerStats.maxHealth.AddModifier(health);
        playerStats.armor.AddModifier(armor);
        playerStats.evasion.AddModifier(evasion);
        playerStats.magicResistance.AddModifier(magicResistance);

        playerStats.fireDamage.AddModifier(fireDamage);
        playerStats.iceDamage.AddModifier(iceDamage);
        playerStats.lightingDamage.AddModifier(lightingDamage);
    }

    public void RemoveModifiers()
    {
        PlayerStats playerStats = PlayerManager.instance.player.GetComponent<PlayerStats>();
        playerStats.strength.RemoveModifier(strength);
        playerStats.agility.RemoveModifier(agility);
        playerStats.intelligence.RemoveModifier(intelligence);
        playerStats.vitality.RemoveModifier(vitality);

        playerStats.damage.RemoveModifier(damage);
        playerStats.critChance.RemoveModifier(critChance);
        playerStats.critPower.RemoveModifier(critPower);

        playerStats.maxHealth.RemoveModifier(health);
        playerStats.armor.RemoveModifier(armor);
        playerStats.evasion.RemoveModifier(evasion);
        playerStats.magicResistance.RemoveModifier(magicResistance);

        playerStats.fireDamage.RemoveModifier(fireDamage);
        playerStats.iceDamage.RemoveModifier(iceDamage);
        playerStats.lightingDamage.RemoveModifier(lightingDamage);
    }

    public override string GetDescription()
    {
        sb.Length = 0;
        descriptionLength = 0;

        AppendSection("物品描述：", ItemDescription);

        bool hasBasicStats = HasAnyBasicStat();

        if (hasBasicStats)
        {
            if (sb.Length > 0)
                sb.AppendLine().AppendLine();

            sb.AppendLine("基础属性：");
            AppendBasicStats();
        }

        if (!string.IsNullOrWhiteSpace(specialEffectDescription))
            AppendSection("特殊效果：", specialEffectDescription);

        return sb.ToString();
    }

    public string GetCraftDescription()
    {
        sb.Length = 0;
        descriptionLength = 0;

        AppendBasicStats();

        return sb.ToString();
    }

    private void AppendSection(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        if (sb.Length > 0)
            sb.AppendLine().AppendLine();

        sb.AppendLine(title);
        sb.Append(content.Trim());
    }

    private bool HasAnyBasicStat()
    {
        return strength != 0 ||
               agility != 0 ||
               intelligence != 0 ||
               vitality != 0 ||
               damage != 0 ||
               critChance != 0 ||
               critPower != 0 ||
               health != 0 ||
               armor != 0 ||
               evasion != 0 ||
               magicResistance != 0 ||
               fireDamage != 0 ||
               iceDamage != 0 ||
               lightingDamage != 0;
    }

    private void AppendBasicStats()
    {
        AddItemDescription(strength, "力量");
        AddItemDescription(agility, "敏捷");
        AddItemDescription(intelligence, "智慧");
        AddItemDescription(vitality, "活力");

        AddItemDescription(damage, "攻击力");
        AddItemDescription(critChance, "暴击率", true);
        AddItemDescription(critPower, "暴击伤害", true);

        AddItemDescription(health, "生命值");
        AddItemDescription(evasion, "闪避率", true);
        AddItemDescription(armor, "防御力");
        AddItemDescription(magicResistance, "魔法抗性");

        AddItemDescription(fireDamage, "火焰伤害");
        AddItemDescription(iceDamage, "冰霜伤害");
        AddItemDescription(lightingDamage, "雷电伤害");
    }

    private void AddItemDescription(int _value, string _name, bool isPercent = false)
    {
        if (_value != 0)
        {
            if (descriptionLength > 0)
                sb.AppendLine();

            if (_value > 0)
                sb.Append(_name + " +" + _value);
            else
                sb.Append(_name + " " + _value);

            if (isPercent)
                sb.Append("%");

            descriptionLength++;
        }
    }

}

