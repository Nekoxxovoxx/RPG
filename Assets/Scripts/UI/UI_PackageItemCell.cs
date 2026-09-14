using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_PackageItemCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text nameText;

    private ItemData itemData;
    private UI ui;

    private void Awake()
    {
        ResolveReferences();
        EnsurePointerRaycastTarget();
    }

    public void Setup(InventoryItem item, TMP_FontAsset font)
    {
        itemData = item != null ? item.data : null;
        ui = GetComponentInParent<UI>();

        ResolveReferences();
        EnsurePointerRaycastTarget();

        if (itemData == null)
        {
            Clear();
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = itemData.itemicon;
            iconImage.color = itemData.itemicon != null ? Color.white : Color.clear;
            iconImage.preserveAspect = true;
        }

        if (amountText != null)
        {
            amountText.font = font != null ? font : amountText.font;
            amountText.text = item.stackSize > 1 ? item.stackSize.ToString() : string.Empty;
        }

        if (nameText != null)
        {
            nameText.font = font != null ? font : nameText.font;
            nameText.text = itemData.itemName;
        }
    }

    public void Clear()
    {
        itemData = null;
        ResolveReferences();

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = Color.clear;
        }

        if (amountText != null)
            amountText.text = string.Empty;

        if (nameText != null)
            nameText.text = string.Empty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UI_ItemTooltip tooltip = GetTooltip();

        if (itemData != null && tooltip != null)
            tooltip.ShowTooltip(itemData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        UI_ItemTooltip tooltip = GetTooltip();

        if (itemData != null && tooltip != null)
            tooltip.UpdateTooltipPosition();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetTooltip()?.HideTooltip();
    }

    private void ResolveReferences()
    {
        if (iconImage == null)
            iconImage = transform.Find("Icon")?.GetComponent<Image>();

        if (amountText == null)
            amountText = transform.Find("Amount")?.GetComponent<TMP_Text>();

        if (nameText == null)
            nameText = transform.Find("Name")?.GetComponent<TMP_Text>();

        if (iconImage == null)
            iconImage = CreateIcon();

        if (amountText == null)
            amountText = CreateAmountText();

        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (amountText != null)
            amountText.raycastTarget = false;

        if (nameText != null)
            nameText.raycastTarget = false;
    }

    private void EnsureUI()
    {
        if (ui == null)
            ui = GetComponentInParent<UI>();

        if (ui == null)
            ui = FindObjectOfType<UI>(true);
    }

    private UI_ItemTooltip GetTooltip()
    {
        EnsureUI();
        return ui != null ? ui.GetItemTooltip() : null;
    }

    private void EnsurePointerRaycastTarget()
    {
        Graphic graphic = GetComponent<Graphic>();

        if (graphic == null)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.preserveAspect = true;
            graphic = image;
        }

        graphic.raycastTarget = true;
    }

    private Image CreateIcon()
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(transform, false);

        RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(72f, 72f);

        Image image = iconObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private TMP_Text CreateAmountText()
    {
        GameObject textObject = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(1f, 0f);
        rectTransform.anchoredPosition = new Vector2(-6f, 4f);
        rectTransform.sizeDelta = new Vector2(56f, 28f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        TMP_Text fontSource = GetComponentInParent<UI>()?.GetComponentInChildren<TMP_Text>(true);
        text.font = fontSource != null ? fontSource.font : text.font;
        text.fontSize = 22f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }
}
