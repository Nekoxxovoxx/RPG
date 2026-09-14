using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    public List<ItemData> startingItems;

    public List<InventoryItem> equipment;
    public Dictionary<ItemData_Equipment, InventoryItem> equipmentDictionary;

    public List<InventoryItem> inventory;
    public Dictionary<ItemData, InventoryItem> inventoryDictionary;

    public List<InventoryItem> stash;
    public Dictionary<ItemData, InventoryItem> stashDictionary;

    [Header("仓库管理")]
    [SerializeField] private Transform inventorySlotParent;
    [SerializeField] private Transform stashSlotParent;
    [SerializeField] private Transform equipmentSlotParent;
    [SerializeField] private Transform statSlotParent;

    private UI_ItemSlot[] inventoryItemSlot;
    private UI_ItemSlot[] stashItemSlot;
    private UI_EquipmentSlot[] equipmentSlot;
    private UI_StatSlot[] statSlot;

    [Header("物品冷却")]
    private float lastTimeUsedArmor;
    private float armorCooldown;

    [Header("制作调试 - 余烬")]
    [SerializeField, Min(0), InspectorName("目标余烬数量")] private int inspectorTargetEmbers = 99999;
    [SerializeField, InspectorName("勾选应用目标余烬")] private bool applyInspectorTargetEmbers;
    [SerializeField, Min(0), InspectorName("当前余烬显示")] private int inspectorCurrentEmbers;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void OnValidate()
    {
        inspectorTargetEmbers = Mathf.Max(0, inspectorTargetEmbers);

        if (!applyInspectorTargetEmbers)
            return;

        applyInspectorTargetEmbers = false;
        SetDebugEmbers(inspectorTargetEmbers);
    }

    private void Start()
    {
        inventory = new List<InventoryItem>();
        inventoryDictionary = new Dictionary<ItemData, InventoryItem>();

        stash = new List<InventoryItem>();
        stashDictionary = new Dictionary<ItemData, InventoryItem>();

        equipment = new List<InventoryItem>();
        equipmentDictionary = new Dictionary<ItemData_Equipment, InventoryItem>();

        inventoryItemSlot = inventorySlotParent != null ? inventorySlotParent.GetComponentsInChildren<UI_ItemSlot>(true) : new UI_ItemSlot[0];
        stashItemSlot = stashSlotParent != null ? stashSlotParent.GetComponentsInChildren<UI_ItemSlot>(true) : new UI_ItemSlot[0];
        equipmentSlot = equipmentSlotParent != null ? equipmentSlotParent.GetComponentsInChildren<UI_EquipmentSlot>(true) : new UI_EquipmentSlot[0];
        statSlot = statSlotParent != null ? statSlotParent.GetComponentsInChildren<UI_StatSlot>(true) : new UI_StatSlot[0];

        AddStatringItems();
        PlayerFlaskSystem.GetOrCreate();
        RefreshInspectorCurrentEmbers();
    }

    private void AddStatringItems()
    {
        if (startingItems == null)
            return;

        for (int i = 0; i < startingItems.Count; i++)
        {
            if (startingItems[i] != null)
                AddItem(startingItems[i]);
        }
    }

    public void EquipItem(ItemData _item)
    {
        ItemData_Equipment newEquipment = _item as ItemData_Equipment;

        if (newEquipment == null)
            return;

        // Flask 类型 → 分配到副槽位
        if (newEquipment.equipmentType == EquipmentType.Flask)
        {
            PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();
            // 主药瓶不允许通过此入口装备
            if (flask.IsMainFlaskItem(newEquipment))
            {
                Debug.Log("主药瓶已固定装备，无需重复操作");
                return;
            }

            if (flask.GetSubFlaskSlotIndex(newEquipment) >= 0)
            {
                Debug.Log("该副药瓶已经装备在副槽位中");
                return;
            }

            int slot = flask.FindEmptySubSlot();
            if (slot < 0)
            {
                Debug.Log("副药瓶槽位已满（最多4个）");
                return;
            }

            if (!flask.SetSubFlask(slot, newEquipment))
                Debug.Log("无法装备该副药瓶");

            UpdateSlotUI();
            return;
        }

        // 原有装备逻辑（非 Flask 类型）
        InventoryItem newitem = new InventoryItem(newEquipment);
        ItemData_Equipment oldEquipment = null;

        foreach (KeyValuePair<ItemData_Equipment, InventoryItem> item in equipmentDictionary)
        {
            if (item.Key != null && item.Key.equipmentType == newEquipment.equipmentType)
                oldEquipment = item.Key;
        }

        if (oldEquipment != null)
        {
            UnequipItem(oldEquipment);
            AddItem(oldEquipment);
        }

        equipment.Add(newitem);
        equipmentDictionary.Add(newEquipment, newitem);
        newEquipment.AddModifiers();

        RemoveItem(_item);
        UpdateSlotUI();
    }

    public void UnequipItem(ItemData_Equipment itemToRemove)
    {
        if (itemToRemove == null || equipmentDictionary == null)
            return;

        // Flask 类型：主药瓶拒绝卸载，副槽位清空
        if (itemToRemove.equipmentType == EquipmentType.Flask)
        {
            PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();
            if (flask.IsMainFlaskItem(itemToRemove))
            {
                Debug.Log("主药瓶无法卸下");
                return;
            }
            // 从副槽位清空
            for (int i = 0; i < flask.SubSlotCount; i++)
            {
                if (flask.GetSubFlaskItem(i) == itemToRemove)
                {
                    flask.ClearSubFlask(i);
                    UpdateSlotUI();
                    return;
                }
            }
            return;
        }

        if (equipmentDictionary.TryGetValue(itemToRemove, out InventoryItem value))
        {
            equipment.Remove(value);
            equipmentDictionary.Remove(itemToRemove);
            itemToRemove.RemoveModifiers();
        }

        UpdateSlotUI();
    }

    // 仅供 PlayerFlaskSystem 自动装备主药瓶使用
    public void ForceEquipMainFlask(ItemData_Equipment item)
    {
        if (item == null) return;

        if (equipmentDictionary == null)
            return;

        if (!equipmentDictionary.TryGetValue(item, out InventoryItem newItem))
        {
            newItem = new InventoryItem(item);
            equipment.Add(newItem);
            equipmentDictionary.Add(item, newItem);
        }

        UpdateSlotUI();
    }

    private void UpdateSlotUI()
    {
        if (equipmentSlot != null)
        {
            ConfigureFlaskSlots();

            for (int i = 0; i < equipmentSlot.Length; i++)
                equipmentSlot[i].CleanUpSlot();

            foreach (KeyValuePair<ItemData_Equipment, InventoryItem> item in equipmentDictionary)
            {
                if (item.Key != null && item.Key.equipmentType == EquipmentType.Flask)
                    continue;

                for (int i = 0; i < equipmentSlot.Length; i++)
                {
                    if (item.Key != null &&
                        equipmentSlot[i].slotType == item.Key.equipmentType &&
                        !equipmentSlot[i].IsFlaskDisplaySlot())
                        equipmentSlot[i].UpdateSlot(item.Value);
                }
            }

            UpdateFlaskEquipmentSlots();
        }

        if (inventoryItemSlot != null)
        {
            for (int i = 0; i < inventoryItemSlot.Length; i++)
                inventoryItemSlot[i].CleanUpSlot();

            for (int i = 0; i < inventory.Count && i < inventoryItemSlot.Length; i++)
                inventoryItemSlot[i].UpdateSlot(inventory[i]);
        }

        if (stashItemSlot != null)
        {
            for (int i = 0; i < stashItemSlot.Length; i++)
                stashItemSlot[i].CleanUpSlot();

            int slotIndex = 0;

            for (int i = 0; i < stash.Count && slotIndex < stashItemSlot.Length; i++)
            {
                InventoryItem stashItem = stash[i];

                if (stashItem == null || stashItem.data == null || stashItem.data.itemType != ItemType.Material)
                    continue;

                stashItemSlot[slotIndex].UpdateSlot(stashItem);
                slotIndex++;
            }
        }

        if (statSlot != null)
        {
            for (int i = 0; i < statSlot.Length; i++)
                statSlot[i].UpdateStatValueUI();
        }

        UpdateFlaskUI();
    }

    public void AddItem(ItemData _item)
    {
        if (_item == null) return;

        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();

        if (flask != null && flask.IsMainFlaskItem(_item))
        {
            flask.RegisterMainFlaskItem(_item);
            flask.RefillMainFlask();
            ForceEquipMainFlask(_item as ItemData_Equipment);
            UpdateSlotUI();
            return;
        }

        if (_item.itemType == ItemType.Equipment)
        {
            if (!CanAddItem()) return;
            AddToInventory(_item);
        }
        else if (_item.itemType == ItemType.Material || _item.itemType == ItemType.Item)
        {
            AddToStash(_item);
        }
        else if (_item.itemType == ItemType.Ember)
        {
            PlayerEmberWallet.GetOrCreate().AddEmbers(1);
        }

        UpdateSlotUI();
    }

    private void AddToStash(ItemData _item)
    {
        if (stashDictionary.TryGetValue(_item, out InventoryItem value))
        {
            value.AddStack();
        }
        else
        {
            InventoryItem newItem = new InventoryItem(_item);
            stash.Add(newItem);
            stashDictionary.Add(_item, newItem);
        }
    }

    private void AddToInventory(ItemData _item)
    {
        if (inventoryDictionary.TryGetValue(_item, out InventoryItem value))
        {
            value.AddStack();
        }
        else
        {
            InventoryItem newItem = new InventoryItem(_item);
            inventory.Add(newItem);
            inventoryDictionary.Add(_item, newItem);
        }
    }

    public void RemoveItem(ItemData _item)
    {
        if (_item == null)
            return;

        if (inventoryDictionary.TryGetValue(_item, out InventoryItem value))
            RemoveFromListAndDictionary(inventory, inventoryDictionary, _item, value);

        if (stashDictionary.TryGetValue(_item, out InventoryItem stashValue))
            RemoveFromListAndDictionary(stash, stashDictionary, _item, stashValue);

        UpdateSlotUI();
    }

    private void RemoveFromListAndDictionary(List<InventoryItem> list, Dictionary<ItemData, InventoryItem> dictionary, ItemData itemData, InventoryItem item)
    {
        if (item.stackSize <= 1)
        {
            list.Remove(item);
            dictionary.Remove(itemData);
        }
        else
        {
            item.RemoveStack();
        }
    }

    public bool CanAddItem()
    {
        if (inventoryItemSlot != null && inventory.Count >= inventoryItemSlot.Length)
        {
            Debug.Log("背包已满");
            return false;
        }

        return true;
    }

    public bool CanCraft(ItemData_Equipment _itemToCraft, List<InventoryItem> _requiredMaterials)
    {
        if (_itemToCraft == null)
            return false;

        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();

        if (flask != null && flask.IsMainFlaskItem(_itemToCraft))
        {
            Debug.Log("残息露滴无法制造");
            return false;
        }

        List<InventoryItem> materialsToRemove = new List<InventoryItem>();

        if (_requiredMaterials != null)
        {
            for (int i = 0; i < _requiredMaterials.Count; i++)
            {
                InventoryItem required = _requiredMaterials[i];

                if (required == null || required.data == null)
                    continue;

                if (!stashDictionary.TryGetValue(required.data, out InventoryItem stashValue) ||
                    stashValue.stackSize < required.stackSize)
                {
                    Debug.Log("材料不足");
                    return false;
                }

                materialsToRemove.Add(required);
            }
        }

        for (int i = 0; i < materialsToRemove.Count; i++)
        {
            for (int count = 0; count < materialsToRemove[i].stackSize; count++)
                RemoveItem(materialsToRemove[i].data);
        }

        AddItem(_itemToCraft);
        Debug.Log("制造完成物品：" + _itemToCraft.name);

        return true;
    }

    public List<InventoryItem> GetEquipmentList() => equipment;

    public List<InventoryItem> GetInventoryList() => inventory;

    public List<InventoryItem> GetStashList() => stash;

    public int CountItem(ItemData itemData, bool includeInventory = true, bool includeStash = true)
    {
        if (itemData == null)
            return 0;

        int count = 0;

        if (includeInventory && inventoryDictionary != null &&
            inventoryDictionary.TryGetValue(itemData, out InventoryItem inventoryItem) &&
            inventoryItem != null)
        {
            count += Mathf.Max(0, inventoryItem.stackSize);
        }

        if (includeStash && stashDictionary != null &&
            stashDictionary.TryGetValue(itemData, out InventoryItem stashItem) &&
            stashItem != null)
        {
            count += Mathf.Max(0, stashItem.stackSize);
        }

        return count;
    }

    public bool HasItemNamed(string itemName, bool includeStash)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return false;

        if (ContainsItemNamed(inventory, itemName))
            return true;

        return includeStash && ContainsItemNamed(stash, itemName);
    }

    public ItemData_Equipment GetEquipment(EquipmentType _type)
    {
        ItemData_Equipment equipedItem = null;

        if (equipmentDictionary == null)
            return null;

        foreach (KeyValuePair<ItemData_Equipment, InventoryItem> item in equipmentDictionary)
        {
            if (item.Key != null && item.Key.equipmentType == _type)
                equipedItem = item.Key;
        }

        return equipedItem;
    }

    private void UpdateFlaskUI()
    {
        if (equipmentSlot == null)
            return;

        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();

        for (int i = 0; i < equipmentSlot.Length; i++)
        {
            UI_EquipmentSlot slot = equipmentSlot[i];

            if (slot == null || slot.slotType != EquipmentType.Flask)
                continue;

            if (slot.IsMainFlaskSlot())
                slot.UpdateFlaskState(flask != null && flask.CanUseMainFlask());
            else if (slot.IsSubFlaskSlot())
                slot.UpdateFlaskState(flask != null && flask.CanUseSubFlask(slot.SubFlaskSlotIndex));
        }
    }

    public bool CanUseFlask()
    {
        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();
        return flask != null && flask.CanUseMainFlask();
    }

    private void ConfigureFlaskSlots()
    {
        int flaskSlotOrder = 0;

        for (int i = 0; i < equipmentSlot.Length; i++)
        {
            UI_EquipmentSlot slot = equipmentSlot[i];

            if (slot == null || slot.slotType != EquipmentType.Flask)
                continue;

            slot.ConfigureAutoFlaskSlot(flaskSlotOrder);
            flaskSlotOrder++;
        }
    }

    private void UpdateFlaskEquipmentSlots()
    {
        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();

        if (flask == null)
            return;

        for (int i = 0; i < equipmentSlot.Length; i++)
        {
            UI_EquipmentSlot slot = equipmentSlot[i];

            if (slot == null || slot.slotType != EquipmentType.Flask)
                continue;

            if (slot.IsMainFlaskSlot())
            {
                ItemData_Equipment main = flask.MainFlaskItemData;

                if (main != null)
                    slot.UpdateSlot(CreateDisplayItem(main, flask.MainCurrentCharges));

                continue;
            }

            if (!slot.IsSubFlaskSlot())
                continue;

            int subIndex = slot.SubFlaskSlotIndex;
            ItemData_Equipment subItem = flask.GetSubFlaskItem(subIndex);
            int count = flask.GetSubFlaskCount(subIndex);

            if (subItem != null && count > 0)
                slot.UpdateSlot(CreateDisplayItem(subItem, count));
            else
                slot.CleanUpSlot();
        }
    }

    private InventoryItem CreateDisplayItem(ItemData itemData, int stackSize)
    {
        InventoryItem displayItem = new InventoryItem(itemData);
        displayItem.stackSize = Mathf.Max(0, stackSize);
        return displayItem;
    }

    public bool CanUseArmor()
    {
        ItemData_Equipment currentArmor = GetEquipment(EquipmentType.Armor);

        if (currentArmor == null)
            return false;

        if (Time.time > lastTimeUsedArmor + armorCooldown)
        {
            armorCooldown = currentArmor.itemCooldown;
            lastTimeUsedArmor = Time.time;
            return true;
        }

        Debug.Log("护甲技能冷却中");
        return false;
    }

    [ContextMenu("调试/应用目标余烬数量")]
    private void ApplyInspectorTargetEmbers()
    {
        SetDebugEmbers(inspectorTargetEmbers);
    }

    [ContextMenu("调试/刷新当前余烬显示")]
    private void RefreshInspectorCurrentEmbers()
    {
        PlayerEmberWallet wallet = PlayerEmberWallet.GetOrCreate();

        if (wallet == null)
        {
            inspectorCurrentEmbers = PlayerPrefs.GetInt("PlayerEmbers", 0);
            return;
        }

        wallet.Load();
        inspectorCurrentEmbers = wallet.CurrentEmbers;
    }

    private void SetDebugEmbers(int amount)
    {
        amount = Mathf.Max(0, amount);

        if (!Application.isPlaying)
        {
            PlayerPrefs.SetInt("PlayerEmbers", amount);
            PlayerPrefs.Save();
            inspectorCurrentEmbers = amount;
            return;
        }

        PlayerEmberWallet wallet = PlayerEmberWallet.GetOrCreate();

        if (wallet != null)
        {
            wallet.SetEmbers(amount);
            inspectorCurrentEmbers = wallet.CurrentEmbers;
            return;
        }

        PlayerPrefs.SetInt("PlayerEmbers", amount);
        PlayerPrefs.Save();
        inspectorCurrentEmbers = amount;
    }

    private bool ContainsItemNamed(List<InventoryItem> items, string itemName)
    {
        if (items == null)
            return false;

        for (int i = 0; i < items.Count; i++)
        {
            ItemData data = items[i] != null ? items[i].data : null;

            if (data == null)
                continue;

            if (data.itemName == itemName || data.name == itemName)
                return true;
        }

        return false;
    }
}
