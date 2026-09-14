using UnityEngine.EventSystems;

public class UI_CraftSlot : UI_ItemSlot
{

    protected override void Start()
    {
        base.Start();
    }
    public void SetupCraftSlot(ItemData_Equipment _data)
    {
        if(_data == null)
            return;

        EnsureSlotReferences();
        item = new InventoryItem(_data);

        if (itemImage != null)
            itemImage.sprite = _data.itemicon;

        if (itemText != null)
        {
            itemText.text = _data.itemName;

            if(itemText.text.Length >12)
                itemText.fontSize = itemText.fontSize*0.7f;
            else
                itemText.fontSize = 24;
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        ItemData_Equipment craftData = item != null ? item.data as ItemData_Equipment : null;

        if (craftData == null)
            return;

        if (ui != null && ui.craftWindow != null)
            ui.craftWindow.SetupCraftWindow(craftData);
    }
}
