using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerFlaskSystem : MonoBehaviour
{
    private const string RuntimeObjectName = "Player Flask System";
    private const string MainFlaskAssetPath = "Assets/Data/Equipment/Flask/\u6062\u590d\u836f\u6c34.asset";
    private const string MainFlaskItemName = "\u6b8b\u606f\u9732\u6ef4";
    private const string MainFlaskAssetName = "\u6062\u590d\u836f\u6c34";
    private const string MaxDewFlaskKey = "DewFlaskMaxCharges";
    private const string CurrentDewFlaskKey = "DewFlaskCurrentCharges";
    private const string DewFlaskHealPercentKey = "DewFlaskHealPercent";
    private const string DewFlaskCapacityLevelKey = "DewFlaskCapacityLevel";
    private const string DewFlaskHealingLevelKey = "DewFlaskHealingLevel";

    public static PlayerFlaskSystem instance;

    [Header("\u4e3b\u836f\u74f6 - \u6b8b\u606f\u9732\u6ef4")]
    [SerializeField] private ItemData_Equipment mainFlaskItemData;
    [SerializeField, Min(0)] private int mainMaxCharges = 3;
    [SerializeField, Min(0)] private int mainCurrentCharges = 3;
    [SerializeField, Range(0.01f, 1f)] private float mainHealPercent = 0.3f;
    [SerializeField, Min(0)] private int mainCapacityUpgradeLevel;
    [SerializeField, Min(0)] private int mainHealingUpgradeLevel;
    [SerializeField] private bool saveFlaskProgress = true;

    [Header("\u526f\u836f\u74f6\u69fd\u4f4d")]
    [SerializeField] private ItemData_Equipment[] initialSubFlasks = new ItemData_Equipment[4];

    [Header("\u51b7\u5374")]
    [SerializeField, Min(0.1f)] private float cooldownDuration = 5f;

    private ItemData_Equipment[] subFlaskItems = new ItemData_Equipment[4];
    private int[] subFlaskOwnedCounts = new int[4];
    private float mainCooldownTimer;
    private float[] subCooldownTimers = new float[4];

    public int MainCurrentCharges => Mathf.Clamp(mainCurrentCharges, 0, MainMaxCharges);
    public int MainMaxCharges => Mathf.Max(0, mainMaxCharges);
    public float MainHealPercent => Mathf.Clamp01(mainHealPercent);
    public int MainCapacityUpgradeLevel => Mathf.Max(0, mainCapacityUpgradeLevel);
    public int MainHealingUpgradeLevel => Mathf.Max(0, mainHealingUpgradeLevel);
    public bool HasMainFlask => mainFlaskItemData != null && MainMaxCharges > 0;
    public float CooldownDuration => cooldownDuration;
    public ItemData_Equipment MainFlaskItemData => mainFlaskItemData;

    public int MaxDewFlaskCharges => MainMaxCharges;
    public int CurrentDewFlaskCharges => MainCurrentCharges;
    public float DewFlaskHealPercent => MainHealPercent;
    public bool HasDewFlask => HasMainFlask;

    public int SubSlotCount => subFlaskItems.Length;

    public event Action OnMainFlaskChanged;
    public event Action OnSubFlasksChanged;
    public event Action OnFlasksChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        EnsureArrays();
        ResolveMainFlaskAsset();
        ApplyInitialSubFlasks();
        Load();
        ClampCharges();
    }

    private void Start()
    {
        StartCoroutine(AutoEquipMainFlaskNextFrame());
    }

    private void Update()
    {
        if (mainCooldownTimer > 0f)
            mainCooldownTimer = Mathf.Max(0f, mainCooldownTimer - Time.deltaTime);

        for (int i = 0; i < subCooldownTimers.Length; i++)
        {
            if (subCooldownTimers[i] > 0f)
                subCooldownTimers[i] = Mathf.Max(0f, subCooldownTimers[i] - Time.deltaTime);
        }
    }

    private void OnValidate()
    {
        EnsureArrays();
        cooldownDuration = Mathf.Max(0.1f, cooldownDuration);
        mainMaxCharges = Mathf.Max(0, mainMaxCharges);
        mainCurrentCharges = Mathf.Clamp(mainCurrentCharges, 0, mainMaxCharges);
        mainHealPercent = Mathf.Clamp01(mainHealPercent);
    }

    public ItemData_Equipment GetSubFlaskItem(int index)
    {
        if (!IsValidSubIndex(index))
            return null;

        RefreshSubCounts();
        return subFlaskItems[index];
    }

    public int GetSubFlaskCount(int index)
    {
        if (!IsValidSubIndex(index))
            return 0;

        RefreshSubCounts();
        return subFlaskOwnedCounts[index];
    }

    public bool HasSubFlask(int index)
    {
        return GetSubFlaskItem(index) != null && GetSubFlaskCount(index) > 0;
    }

    public int GetSubFlaskSlotIndex(ItemData_Equipment item)
    {
        if (item == null)
            return -1;

        for (int i = 0; i < subFlaskItems.Length; i++)
        {
            if (subFlaskItems[i] == item)
                return i;
        }

        return -1;
    }

    public float GetMainCooldownProgress()
    {
        if (cooldownDuration <= 0f)
            return 0f;

        return Mathf.Clamp01(mainCooldownTimer / cooldownDuration);
    }

    public float GetSubCooldownProgress(int index)
    {
        if (!IsValidSubIndex(index) || cooldownDuration <= 0f)
            return 0f;

        return Mathf.Clamp01(subCooldownTimers[index] / cooldownDuration);
    }

    public void StartMainFlaskCooldown()
    {
        mainCooldownTimer = cooldownDuration;
    }

    public void StartSubFlaskCooldown(int index)
    {
        if (!IsValidSubIndex(index))
            return;

        subCooldownTimers[index] = cooldownDuration;
    }

    public bool CanUseMainFlask(PlayerStats targetStats = null)
    {
        if (!HasMainFlask || MainCurrentCharges <= 0 || mainCooldownTimer > 0f)
            return false;

        if (targetStats == null)
            targetStats = ResolvePlayerStats();

        return targetStats != null && !targetStats.isDead && targetStats.currentHealth > 0 &&
            MainHealPercent > 0f && targetStats.currentHealth < targetStats.GetMaxHealthValue();
    }

    public bool CanUseSubFlask(int index)
    {
        if (!IsValidSubIndex(index) || subCooldownTimers[index] > 0f)
            return false;

        PlayerStats targetStats = ResolvePlayerStats();
        if (targetStats == null || targetStats.isDead)
            return false;

        RefreshSubCounts();
        return subFlaskItems[index] != null && subFlaskOwnedCounts[index] > 0;
    }

    public bool UseMainFlask()
    {
        PlayerStats targetStats = ResolvePlayerStats();

        if (!CanUseMainFlask(targetStats))
            return false;

        int healAmount = Mathf.Max(1, Mathf.RoundToInt(targetStats.GetMaxHealthValue() * MainHealPercent));
        mainCurrentCharges = Mathf.Max(0, mainCurrentCharges - 1);
        mainCooldownTimer = cooldownDuration;
        targetStats.IncreaseHealthBy(healAmount);

        Save();
        NotifyMainFlaskChanged();
        return true;
    }

    public bool TryUseSubFlask(int index)
    {
        if (!CanUseSubFlask(index))
            return false;

        ItemData_Equipment item = subFlaskItems[index];

        if (!TryConsumeOwnedItem(item))
            return false;

        ExecuteSubFlaskEffects(item);
        RefreshSubCounts();
        StartSubFlaskCooldown(index);

        if (subFlaskOwnedCounts[index] <= 0)
            ClearSubFlask(index);
        else
            NotifySubFlasksChanged();

        return true;
    }

    public bool SetSubFlask(int slotIndex, ItemData_Equipment item)
    {
        if (!IsValidSubIndex(slotIndex) || item == null || item.equipmentType != EquipmentType.Flask)
            return false;

        if (IsMainFlaskItem(item))
            return false;

        if (GetSubFlaskSlotIndex(item) >= 0)
            return false;

        if (CountOwnedItem(item) <= 0)
            return false;

        subFlaskItems[slotIndex] = item;
        RefreshSubCounts();
        NotifySubFlasksChanged();
        return true;
    }

    public void ClearSubFlask(int slotIndex)
    {
        if (!IsValidSubIndex(slotIndex))
            return;

        subFlaskItems[slotIndex] = null;
        subFlaskOwnedCounts[slotIndex] = 0;
        subCooldownTimers[slotIndex] = 0f;
        NotifySubFlasksChanged();
    }

    public int FindEmptySubSlot()
    {
        RefreshSubCounts();

        for (int i = 0; i < subFlaskItems.Length; i++)
        {
            if (subFlaskItems[i] == null)
                return i;
        }

        return -1;
    }

    public void RefreshSubCounts()
    {
        for (int i = 0; i < subFlaskItems.Length; i++)
        {
            ItemData_Equipment item = subFlaskItems[i];
            int count = CountOwnedItem(item);
            subFlaskOwnedCounts[i] = count;

            if (item != null && count <= 0)
            {
                subFlaskItems[i] = null;
                subCooldownTimers[i] = 0f;
            }
        }
    }

    public void UpgradeMainFlaskCapacity(int amount)
    {
        if (amount <= 0)
            return;

        mainCapacityUpgradeLevel++;
        mainMaxCharges += amount;
        mainCurrentCharges = MainMaxCharges;
        Save();
        NotifyMainFlaskChanged();
    }

    public void UpgradeMainFlaskHealing(float extraHealPercent)
    {
        if (extraHealPercent <= 0f)
            return;

        mainHealingUpgradeLevel++;
        mainHealPercent = Mathf.Clamp01(mainHealPercent + extraHealPercent);
        Save();
        NotifyMainFlaskChanged();
    }

    public void RefillMainFlask()
    {
        mainCurrentCharges = MainMaxCharges;
        mainCooldownTimer = 0f;
        Save();
        NotifyMainFlaskChanged();
    }

    public void Load()
    {
        if (!saveFlaskProgress)
        {
            ClampCharges();
            return;
        }

        bool hadHealPercentKey = PlayerPrefs.HasKey(DewFlaskHealPercentKey);
        bool migratedOldDefaultHealPercent = false;

        mainMaxCharges = PlayerPrefs.GetInt(MaxDewFlaskKey, mainMaxCharges);
        mainCurrentCharges = PlayerPrefs.GetInt(CurrentDewFlaskKey, mainMaxCharges);
        mainHealPercent = PlayerPrefs.GetFloat(DewFlaskHealPercentKey, mainHealPercent);
        mainCapacityUpgradeLevel = PlayerPrefs.GetInt(DewFlaskCapacityLevelKey, mainCapacityUpgradeLevel);
        mainHealingUpgradeLevel = PlayerPrefs.GetInt(DewFlaskHealingLevelKey, mainHealingUpgradeLevel);

        if (hadHealPercentKey && mainHealingUpgradeLevel <= 0 && Mathf.Approximately(mainHealPercent, 0.45f))
        {
            mainHealPercent = 0.3f;
            migratedOldDefaultHealPercent = true;
        }

        ClampCharges();

        if (migratedOldDefaultHealPercent)
            Save();
    }

    public void Save()
    {
        if (!saveFlaskProgress)
            return;

        ClampCharges();
        PlayerPrefs.SetInt(MaxDewFlaskKey, mainMaxCharges);
        PlayerPrefs.SetInt(CurrentDewFlaskKey, mainCurrentCharges);
        PlayerPrefs.SetFloat(DewFlaskHealPercentKey, mainHealPercent);
        PlayerPrefs.SetInt(DewFlaskCapacityLevelKey, mainCapacityUpgradeLevel);
        PlayerPrefs.SetInt(DewFlaskHealingLevelKey, mainHealingUpgradeLevel);
        PlayerPrefs.Save();
    }

    public bool IsMainFlaskItem(ItemData item)
    {
        if (item == null)
            return false;

        if (mainFlaskItemData != null && item == mainFlaskItemData)
            return true;

        return IsNamedMainFlaskItem(item);
    }

    public bool RegisterMainFlaskItem(ItemData item)
    {
        if (!IsMainFlaskItem(item))
            return false;

        ItemData_Equipment equipment = item as ItemData_Equipment;

        if (equipment == null)
            return false;

        mainFlaskItemData = equipment;
        ClampCharges();
        NotifyMainFlaskChanged();
        return true;
    }

    public static PlayerFlaskSystem GetOrCreate()
    {
        if (instance != null)
            return instance;

        PlayerFlaskSystem existing = FindObjectOfType<PlayerFlaskSystem>(true);
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject host = PlayerManager.instance != null && PlayerManager.instance.player != null
            ? PlayerManager.instance.player.gameObject
            : new GameObject(RuntimeObjectName);

        PlayerFlaskSystem system = host.GetComponent<PlayerFlaskSystem>();
        if (system == null)
            system = host.AddComponent<PlayerFlaskSystem>();

        instance = system;
        return instance;
    }

    private System.Collections.IEnumerator AutoEquipMainFlaskNextFrame()
    {
        while (Inventory.instance == null || !Inventory.instance.IsInitialized)
            yield return null;
        AutoEquipMainFlask();
    }

    private void AutoEquipMainFlask()
    {
        ResolveMainFlaskAsset();
        ResolveMainFlaskFromInventory();

        if (mainFlaskItemData == null || Inventory.instance == null)
            return;

        Inventory.instance.ForceEquipMainFlask(mainFlaskItemData);
    }

    private void EnsureArrays()
    {
        if (initialSubFlasks == null || initialSubFlasks.Length != 4)
            Array.Resize(ref initialSubFlasks, 4);

        if (subFlaskItems == null || subFlaskItems.Length != 4)
            subFlaskItems = new ItemData_Equipment[4];

        if (subFlaskOwnedCounts == null || subFlaskOwnedCounts.Length != 4)
            subFlaskOwnedCounts = new int[4];

        if (subCooldownTimers == null || subCooldownTimers.Length != 4)
            subCooldownTimers = new float[4];
    }

    private void ApplyInitialSubFlasks()
    {
        for (int i = 0; i < initialSubFlasks.Length && i < subFlaskItems.Length; i++)
        {
            ItemData_Equipment item = initialSubFlasks[i];

            if (item != null && item.equipmentType == EquipmentType.Flask && !IsMainFlaskItem(item))
                subFlaskItems[i] = item;
        }
    }

    private void ResolveMainFlaskAsset()
    {
        if (mainFlaskItemData != null)
            return;

#if UNITY_EDITOR
        mainFlaskItemData = AssetDatabase.LoadAssetAtPath<ItemData_Equipment>(MainFlaskAssetPath);

        if (mainFlaskItemData != null)
            return;

        string[] guids = AssetDatabase.FindAssets("t:ItemData_Equipment", new[] { "Assets/Data/Equipment/Flask" });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemData_Equipment candidate = AssetDatabase.LoadAssetAtPath<ItemData_Equipment>(path);

            if (candidate != null && candidate.itemName == MainFlaskItemName)
            {
                mainFlaskItemData = candidate;
                return;
            }
        }
#endif

        ResolveMainFlaskFromInventory();

        if (mainFlaskItemData != null)
            return;

        ResolveMainFlaskFromLoadedAssets();
    }

    private void ResolveMainFlaskFromInventory()
    {
        if (mainFlaskItemData != null || Inventory.instance == null)
            return;

        if (TryRegisterMainFlaskFromItems(Inventory.instance.startingItems))
            return;

        if (TryRegisterMainFlaskFromInventoryItems(Inventory.instance.GetEquipmentList()))
            return;

        if (TryRegisterMainFlaskFromInventoryItems(Inventory.instance.GetInventoryList()))
            return;

        TryRegisterMainFlaskFromInventoryItems(Inventory.instance.GetStashList());
    }

    private bool TryRegisterMainFlaskFromItems(List<ItemData> items)
    {
        if (items == null)
            return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (RegisterMainFlaskItem(items[i]))
                return true;
        }

        return false;
    }

    private bool TryRegisterMainFlaskFromInventoryItems(List<InventoryItem> items)
    {
        if (items == null)
            return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && RegisterMainFlaskItem(items[i].data))
                return true;
        }

        return false;
    }

    private void ResolveMainFlaskFromLoadedAssets()
    {
        if (mainFlaskItemData != null)
            return;

        ItemData_Equipment[] loadedEquipment = Resources.FindObjectsOfTypeAll<ItemData_Equipment>();

        for (int i = 0; i < loadedEquipment.Length; i++)
        {
            if (RegisterMainFlaskItem(loadedEquipment[i]))
                return;
        }
    }

    private bool IsNamedMainFlaskItem(ItemData item)
    {
        return item is ItemData_Equipment equipment &&
               equipment.equipmentType == EquipmentType.Flask &&
               (item.itemName == MainFlaskItemName || item.name == MainFlaskAssetName);
    }

    private void ExecuteSubFlaskEffects(ItemData_Equipment item)
    {
        if (item == null || item.itemEffects == null)
            return;

        Transform target = PlayerManager.instance != null && PlayerManager.instance.player != null
            ? PlayerManager.instance.player.transform
            : null;

        foreach (ItemEffect effect in item.itemEffects)
        {
            if (effect != null)
                effect.ExecuteEffect(target);
        }
    }

    private PlayerStats ResolvePlayerStats()
    {
        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            return PlayerManager.instance.player.stats as PlayerStats
                ?? PlayerManager.instance.player.GetComponent<PlayerStats>();

        return FindObjectOfType<PlayerStats>();
    }

    private int CountOwnedItem(ItemData_Equipment item)
    {
        if (item == null || Inventory.instance == null)
            return 0;

        int count = 0;
        count += CountInList(Inventory.instance.GetInventoryList(), item);
        count += CountInList(Inventory.instance.GetStashList(), item);
        return count;
    }

    private int CountInList(System.Collections.Generic.List<InventoryItem> items, ItemData_Equipment item)
    {
        if (items == null || item == null)
            return 0;

        int count = 0;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].data == item)
                count += Mathf.Max(0, items[i].stackSize);
        }

        return count;
    }

    private bool TryConsumeOwnedItem(ItemData_Equipment item)
    {
        if (item == null || Inventory.instance == null || CountOwnedItem(item) <= 0)
            return false;

        Inventory.instance.RemoveItem(item);
        return true;
    }

    private bool IsValidSubIndex(int index)
    {
        return index >= 0 && index < subFlaskItems.Length;
    }

    private void ClampCharges()
    {
        mainMaxCharges = Mathf.Max(0, mainMaxCharges);
        mainCurrentCharges = Mathf.Clamp(mainCurrentCharges, 0, mainMaxCharges);
        mainHealPercent = Mathf.Clamp01(mainHealPercent);
        mainCapacityUpgradeLevel = Mathf.Max(0, mainCapacityUpgradeLevel);
        mainHealingUpgradeLevel = Mathf.Max(0, mainHealingUpgradeLevel);
    }

    private void NotifyMainFlaskChanged()
    {
        OnMainFlaskChanged?.Invoke();
        OnFlasksChanged?.Invoke();
    }

    private void NotifySubFlasksChanged()
    {
        OnSubFlasksChanged?.Invoke();
        OnFlasksChanged?.Invoke();
    }
}
