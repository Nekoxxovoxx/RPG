using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ItemSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField ]protected Image itemImage;
    [SerializeField ]protected TextMeshProUGUI itemText;

    protected UI ui;
    public InventoryItem item;

    protected virtual void Awake()
    {
        EnsureSlotReferences();
    }

    protected virtual void Start()
    {
        ui = GetComponentInParent<UI>();
    }

    protected void EnsureSlotReferences()
    {
        if (itemImage == null)
            itemImage = GetComponentInChildren<Image>(true);

        if (itemText == null)
            itemText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public virtual void UpdateSlot(InventoryItem _newitem)
    {
        EnsureSlotReferences();
        item = _newitem;

        if (item == null || item.data == null)
        {
            CleanUpSlot();
            return;
        }

        if (itemImage != null)
        {
            itemImage.color = Color.white;
            itemImage.sprite = item.data.itemicon;
        }

        if (itemText != null)
        {
            if (item.stackSize > 1)
            {
                itemText.text = item.stackSize.ToString();
            }
            else
            {
                itemText.text = "";
            }
        }
    }

    public virtual void CleanUpSlot()
    {
        EnsureSlotReferences();
        item = null;

        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.color = Color.clear;
        }

        if (itemText != null)
            itemText.text = ""; 
    }

    public virtual  void OnPointerDown(PointerEventData eventData)
    {
        if (item == null || item.data == null || Inventory.instance == null)
            return;

        if(Input.GetKey(KeyCode.LeftControl))
        {
            Inventory.instance.RemoveItem(item.data);
            ui?.itemTooltip?.HideTooltip();
            return;
        }

        if (item.data.itemType == ItemType.Equipment && item.data is ItemData_Equipment)
            Inventory.instance.EquipItem(item.data);

        ui?.itemTooltip?.HideTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(item == null || item.data == null || ui == null || ui.itemTooltip == null)
            return;

       ui.itemTooltip.ShowTooltip(item.data);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (item == null || item.data == null || ui == null || ui.itemTooltip == null)
            return;

        ui.itemTooltip.UpdateTooltipPosition();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui?.itemTooltip?.HideTooltip();
    }
}
