using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum FlaskEquipmentSlotKind
{
    Normal,
    Main,
    Sub
}

public class UI_EquipmentSlot : UI_ItemSlot
{
    [SerializeField] private Image icon;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(1, 1, 1, 0.3f);
    public EquipmentType slotType;

    [Header("Flask \u69fd\u4f4d")]
    [SerializeField] private FlaskEquipmentSlotKind flaskSlotKind = FlaskEquipmentSlotKind.Normal;
    [SerializeField, Range(0, 3)] private int subFlaskSlotIndex;

    [Header("\u65e7\u4e3b Flask \u69fd\u4f4d\u517c\u5bb9")]
    [SerializeField] private bool isMainFlaskSlot;

    private FlaskEquipmentSlotKind runtimeFlaskSlotKind = FlaskEquipmentSlotKind.Normal;
    private int runtimeSubFlaskSlotIndex;

    public int SubFlaskSlotIndex => EffectiveFlaskSlotKind == FlaskEquipmentSlotKind.Sub
        ? Mathf.Clamp(EffectiveSubFlaskSlotIndex, 0, 3)
        : -1;

    private FlaskEquipmentSlotKind EffectiveFlaskSlotKind
    {
        get
        {
            if (slotType != EquipmentType.Flask)
                return FlaskEquipmentSlotKind.Normal;

            if (isMainFlaskSlot)
                return FlaskEquipmentSlotKind.Main;

            if (flaskSlotKind != FlaskEquipmentSlotKind.Normal)
                return flaskSlotKind;

            return runtimeFlaskSlotKind;
        }
    }

    private int EffectiveSubFlaskSlotIndex => flaskSlotKind == FlaskEquipmentSlotKind.Sub
        ? subFlaskSlotIndex
        : runtimeSubFlaskSlotIndex;

    private void OnValidate()
    {
        string suffix = slotType.ToString();

        if (slotType == EquipmentType.Flask)
        {
            if (isMainFlaskSlot || flaskSlotKind == FlaskEquipmentSlotKind.Main)
                suffix = "\u4e3bFlask";
            else if (flaskSlotKind == FlaskEquipmentSlotKind.Sub)
                suffix = "\u526fFlask" + (subFlaskSlotIndex + 1);
            else
                suffix = "Flask";
        }

        gameObject.name = "\u88c5\u5907\u69fd-" + suffix;
    }

    public override void UpdateSlot(InventoryItem _newitem)
    {
        base.UpdateSlot(_newitem);

        if (slotType != EquipmentType.Flask || item == null || item.data == null || itemText == null)
            return;

        itemText.text = item.stackSize.ToString();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (IsMainFlaskSlot())
        {
            Debug.Log("\u4e3b\u836f\u74f6\u69fd\u4f4d\u5df2\u9501\u5b9a\uff0c\u65e0\u6cd5\u5378\u4e0b");
            return;
        }

        ItemData_Equipment equipmentData = item != null ? item.data as ItemData_Equipment : null;

        if (equipmentData == null || Inventory.instance == null)
            return;

        if (IsSubFlaskSlot())
        {
            Inventory.instance.UnequipItem(equipmentData);
            ui?.itemTooltip?.HideTooltip();
            CleanUpSlot();
            return;
        }

        Inventory.instance.UnequipItem(equipmentData);
        Inventory.instance.AddItem(equipmentData);

        ui?.itemTooltip?.HideTooltip();
        CleanUpSlot();
    }

    public override void CleanUpSlot()
    {
        base.CleanUpSlot();

        if (IsMainFlaskSlot())
            RefreshMainFlaskDisplay();
    }

    public void ConfigureAutoFlaskSlot(int flaskSlotOrder)
    {
        if (slotType != EquipmentType.Flask || flaskSlotKind != FlaskEquipmentSlotKind.Normal || isMainFlaskSlot)
            return;

        if (flaskSlotOrder <= 0)
        {
            runtimeFlaskSlotKind = FlaskEquipmentSlotKind.Main;
            runtimeSubFlaskSlotIndex = -1;
            return;
        }

        runtimeFlaskSlotKind = FlaskEquipmentSlotKind.Sub;
        runtimeSubFlaskSlotIndex = Mathf.Clamp(flaskSlotOrder - 1, 0, 3);
    }

    public void UpdateFlaskState(bool canUse)
    {
        Image targetIcon = icon != null ? icon : itemImage;

        if (targetIcon == null)
            return;

        if (item == null || item.data == null)
        {
            if (targetIcon == itemImage)
                targetIcon.color = Color.clear;

            return;
        }

        targetIcon.color = normalColor;
    }

    public bool IsMainFlaskSlot()
    {
        return EffectiveFlaskSlotKind == FlaskEquipmentSlotKind.Main;
    }

    public bool IsSubFlaskSlot()
    {
        return EffectiveFlaskSlotKind == FlaskEquipmentSlotKind.Sub;
    }

    public bool IsFlaskDisplaySlot()
    {
        return IsMainFlaskSlot() || IsSubFlaskSlot();
    }

    private void RefreshMainFlaskDisplay()
    {
        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();

        if (flask == null || flask.MainFlaskItemData == null)
            return;

        InventoryItem displayItem = new InventoryItem(flask.MainFlaskItemData);
        displayItem.stackSize = flask.MainCurrentCharges;
        UpdateSlot(displayItem);
    }
}
