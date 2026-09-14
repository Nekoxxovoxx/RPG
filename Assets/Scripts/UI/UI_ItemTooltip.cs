using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ItemTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private Vector2 mouseOffset = new Vector2(150, 150);
    [SerializeField] private Vector2 screenPadding = new Vector2(12, 12);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        MakeTooltipIgnoreRaycasts();
    }

    private void Update()
    {
        UpdateTooltipPosition();
    }

    public void ShowTooltip(ItemData item)
    {
        MakeTooltipIgnoreRaycasts();

        if (item == null)
        {
            HideTooltip();
            return;
        }

        ItemData_Equipment equipment = item as ItemData_Equipment;

        if (itemNameText != null)
            itemNameText.text = item.itemName;

        if (itemTypeText != null)
            itemTypeText.text = equipment != null ? GetEquipmentTypeName(equipment.equipmentType) : GetItemTypeName(item.itemType);

        if (itemDescription != null)
            itemDescription.text = item.GetDescription();

        gameObject.SetActive(true);

        if (rectTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        UpdateTooltipPosition();
    }

    public void UpdateTooltipPosition()
    {
        Vector2 mousePosition = Input.mousePosition;

        float xOffset = mousePosition.x > Screen.width * .5f ? -mouseOffset.x : mouseOffset.x;
        float yOffset = mousePosition.y > Screen.height * .5f ? -mouseOffset.y : mouseOffset.y;

        Vector2 tooltipPosition = new Vector2(mousePosition.x + xOffset, mousePosition.y + yOffset);

        if (rectTransform != null)
        {
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;

            float minX = screenPadding.x + size.x * pivot.x;
            float maxX = Screen.width - screenPadding.x - size.x * (1f - pivot.x);
            float minY = screenPadding.y + size.y * pivot.y;
            float maxY = Screen.height - screenPadding.y - size.y * (1f - pivot.y);

            tooltipPosition.x = Mathf.Clamp(tooltipPosition.x, minX, maxX);
            tooltipPosition.y = Mathf.Clamp(tooltipPosition.y, minY, maxY);
        }

        transform.position = tooltipPosition;
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }

    private void MakeTooltipIgnoreRaycasts()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    private string GetItemTypeName(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Material:
                return "材料";
            case ItemType.Equipment:
                return "装备";
            case ItemType.Item:
                return "道具";
            case ItemType.Ember:
                return "余烬";
            default:
                return itemType.ToString();
        }
    }

    private string GetEquipmentTypeName(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                return "武器";
            case EquipmentType.Armor:
                return "护甲";
            case EquipmentType.Amulet:
                return "护符";
            case EquipmentType.Flask:
                return "药瓶";
            default:
                return equipmentType.ToString();
        }
    }
}
