using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDropManager : MonoBehaviour
{
    private static readonly EnemyKind[] SupportedDropKinds =
    {
        EnemyKind.骷髅敌人,
        EnemyKind.小恶魔,
        EnemyKind.混沌狼主,
        EnemyKind.赤髓Boss
    };

    private const string RuntimeObjectName = "怪物掉落系统";

    public static EnemyDropManager instance;

    [Header("默认掉落设置")]
    [SerializeField] private GameObject defaultDropPrefab;
    [SerializeField] private bool useLegacyDropPrefabFallback = true;
    [SerializeField] private Vector2 dropVelocityXRange = new Vector2(-5f, 5f);
    [SerializeField] private Vector2 dropVelocityYRange = new Vector2(15f, 20f);
    [SerializeField] private Vector2 randomSpawnXOffset = new Vector2(-0.25f, 0.25f);
    [SerializeField] private bool logDrops;

    [Header("按怪物类型配置材料掉落")]
    [SerializeField] private List<EnemyDropRule> enemyDropRules = new List<EnemyDropRule>();

    private readonly HashSet<int> droppedEnemyIds = new HashSet<int>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureDefaultRules();
    }

    public static EnemyDropManager GetOrCreate()
    {
        if (instance != null)
            return instance;

        EnemyDropManager existing = FindObjectOfType<EnemyDropManager>(true);

        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject managerObject = new GameObject(RuntimeObjectName);
        return managerObject.AddComponent<EnemyDropManager>();
    }

    public void DropForEnemy(EnemyStats enemyStats)
    {
        if (enemyStats == null)
            return;

        int enemyId = enemyStats.GetInstanceID();

        if (droppedEnemyIds.Contains(enemyId))
            return;

        droppedEnemyIds.Add(enemyId);

        EnemyDropRule rule = GetRule(enemyStats.Kind);

        if (rule == null || !rule.Enabled)
            return;

        GameObject prefabToUse = rule.DropPrefabOverride != null ? rule.DropPrefabOverride : defaultDropPrefab;

        if (prefabToUse == null && useLegacyDropPrefabFallback)
            prefabToUse = FindLegacyDropPrefab(enemyStats);

        if (prefabToUse == null)
        {
            Debug.LogWarning("怪物掉落系统缺少掉落物预制体，请在 Inspector 的默认掉落物预制体中绑定 Item object-No data。", this);
            return;
        }

        List<EnemyItemDropEntry> entries = rule.DropEntries;

        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            EnemyItemDropEntry entry = entries[i];

            if (entry == null || !entry.ShouldDrop())
                continue;

            int amount = entry.RollAmount();

            for (int count = 0; count < amount; count++)
                SpawnDrop(prefabToUse, entry.Item, enemyStats.transform.position);

            if (logDrops && entry.Item != null && amount > 0)
                Debug.Log($"{enemyStats.name} 掉落 {entry.Item.itemName} x{amount}", this);
        }
    }

    private void SpawnDrop(GameObject dropPrefab, ItemData itemData, Vector3 origin)
    {
        if (dropPrefab == null || itemData == null)
            return;

        Vector3 spawnPosition = origin + new Vector3(UnityEngine.Random.Range(randomSpawnXOffset.x, randomSpawnXOffset.y), 0f, 0f);
        GameObject newDrop = Instantiate(dropPrefab, spawnPosition, Quaternion.identity);
        ItemObject itemObject = newDrop.GetComponent<ItemObject>();

        if (itemObject == null)
            itemObject = newDrop.GetComponentInChildren<ItemObject>();

        if (itemObject == null)
        {
            Debug.LogWarning("掉落物预制体缺少 ItemObject 组件：" + dropPrefab.name, this);
            Destroy(newDrop);
            return;
        }

        Vector2 randomVelocity = new Vector2(
            UnityEngine.Random.Range(dropVelocityXRange.x, dropVelocityXRange.y),
            UnityEngine.Random.Range(dropVelocityYRange.x, dropVelocityYRange.y));

        itemObject.SetupItem(itemData, randomVelocity);
    }

    private EnemyDropRule GetRule(EnemyKind enemyKind)
    {
        if (!IsSupportedDropKind(enemyKind))
            return null;

        EnsureDefaultRules();

        for (int i = 0; i < enemyDropRules.Count; i++)
        {
            EnemyDropRule rule = enemyDropRules[i];

            if (rule != null && rule.Kind == enemyKind)
                return rule;
        }

        return null;
    }

    private GameObject FindLegacyDropPrefab(EnemyStats enemyStats)
    {
        ItemDrop itemDrop = enemyStats != null ? enemyStats.GetComponent<ItemDrop>() : null;

        if (itemDrop != null && itemDrop.DropPrefab != null)
            return itemDrop.DropPrefab;

        ItemDrop[] itemDrops = FindObjectsOfType<ItemDrop>(true);

        for (int i = 0; i < itemDrops.Length; i++)
        {
            if (itemDrops[i] != null && itemDrops[i].DropPrefab != null)
                return itemDrops[i].DropPrefab;
        }

        return null;
    }

    private void EnsureDefaultRules()
    {
        if (enemyDropRules == null)
            enemyDropRules = new List<EnemyDropRule>();

        RemoveUnsupportedRules();

        for (int i = 0; i < SupportedDropKinds.Length; i++)
            AddRuleIfMissing(SupportedDropKinds[i]);
    }

    private void RemoveUnsupportedRules()
    {
        for (int i = enemyDropRules.Count - 1; i >= 0; i--)
        {
            EnemyDropRule rule = enemyDropRules[i];

            if (rule == null || !IsSupportedDropKind(rule.Kind))
                enemyDropRules.RemoveAt(i);
        }
    }

    private bool IsSupportedDropKind(EnemyKind kind)
    {
        for (int i = 0; i < SupportedDropKinds.Length; i++)
        {
            if (SupportedDropKinds[i] == kind)
                return true;
        }

        return false;
    }

    private void AddRuleIfMissing(EnemyKind kind)
    {
        for (int i = 0; i < enemyDropRules.Count; i++)
        {
            EnemyDropRule rule = enemyDropRules[i];

            if (rule != null && rule.Kind == kind)
                return;
        }

        enemyDropRules.Add(new EnemyDropRule(kind));
    }

    private void OnValidate()
    {
        EnsureDefaultRules();

        for (int i = 0; i < enemyDropRules.Count; i++)
            enemyDropRules[i]?.Validate();
    }
}

[Serializable]
public class EnemyDropRule
{
    [SerializeField] private EnemyKind kind;
    [SerializeField] private bool enabled = true;
    [SerializeField] private GameObject dropPrefabOverride;
    [SerializeField] private List<EnemyItemDropEntry> dropEntries = new List<EnemyItemDropEntry>();

    public EnemyKind Kind => kind;
    public bool Enabled => enabled;
    public GameObject DropPrefabOverride => dropPrefabOverride;
    public List<EnemyItemDropEntry> DropEntries => dropEntries;

    public EnemyDropRule(EnemyKind kind)
    {
        this.kind = kind;
    }

    public void Validate()
    {
        if (dropEntries == null)
            dropEntries = new List<EnemyItemDropEntry>();

        for (int i = 0; i < dropEntries.Count; i++)
            dropEntries[i]?.Validate();
    }
}

[Serializable]
public class EnemyItemDropEntry
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int minAmount = 1;
    [SerializeField, Min(1)] private int maxAmount = 1;
    [SerializeField, Range(0f, 100f)] private float dropChance = 100f;

    public ItemData Item => item;

    public bool ShouldDrop()
    {
        return item != null && UnityEngine.Random.Range(0f, 100f) <= dropChance;
    }

    public int RollAmount()
    {
        Validate();
        return UnityEngine.Random.Range(minAmount, maxAmount + 1);
    }

    public void Validate()
    {
        minAmount = Mathf.Max(1, minAmount);
        maxAmount = Mathf.Max(minAmount, maxAmount);
        dropChance = Mathf.Clamp(dropChance, 0f, 100f);
    }
}
