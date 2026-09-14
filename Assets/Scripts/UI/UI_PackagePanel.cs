using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PackagePanel : MonoBehaviour
{
    private enum PackageFilter
    {
        All,
        Equipment,
        Material,
        Item
    }

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Vector2 scrollAnchorMin = new Vector2(0.16f, 0.14f);
    [SerializeField] private Vector2 scrollAnchorMax = new Vector2(0.84f, 0.68f);
    [SerializeField] private Vector2 cellSize = new Vector2(112f, 130f);
    [SerializeField] private Vector2 cellSpacing = new Vector2(16f, 14f);
    [SerializeField] private int fixedColumnCount = 8;
    [SerializeField] private bool useDesignedItemSlots = true;
    [SerializeField] private bool includeEquippedItems = true;

    private readonly List<UI_PackageItemCell> cells = new List<UI_PackageItemCell>();
    private PackageFilter currentFilter = PackageFilter.All;
    private TMP_FontAsset font;
    private bool initialized;
    private bool keepEmptyCellsVisible;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Open();
    }

    public void Open()
    {
        Initialize();
        Refresh();
    }

    public void Refresh()
    {
        Initialize();

        Inventory inventory = Inventory.instance;

        if (inventory == null || contentRoot == null)
            return;

        List<InventoryItem> displayItems = BuildDisplayItems(inventory);
        EnsureCellCount(displayItems.Count);

        for (int i = 0; i < cells.Count; i++)
        {
            bool active = i < displayItems.Count;

            if (active)
            {
                cells[i].gameObject.SetActive(true);
                cells[i].Setup(displayItems[i], font);
            }
            else if (keepEmptyCellsVisible)
            {
                cells[i].gameObject.SetActive(true);
                cells[i].Clear();
            }
            else
            {
                cells[i].gameObject.SetActive(false);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        font = GetComponentInChildren<TMP_Text>(true)?.font;

        if (useDesignedItemSlots && TryBindDesignedItemSlots())
            keepEmptyCellsVisible = true;
        else
            EnsureScrollView();

        BindFilterButtons();
    }

    private List<InventoryItem> BuildDisplayItems(Inventory inventory)
    {
        List<InventoryItem> result = new List<InventoryItem>();
        AddFilteredItems(result, inventory.GetInventoryList());
        AddFilteredItems(result, inventory.GetStashList());

        if (includeEquippedItems)
            AddFilteredItems(result, inventory.GetEquipmentList());

        return result;
    }

    private void AddFilteredItems(List<InventoryItem> result, List<InventoryItem> source)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            InventoryItem item = source[i];

            if (item == null || item.data == null)
                continue;

            if (!PassesFilter(item.data))
                continue;

            result.Add(item);
        }
    }

    private bool PassesFilter(ItemData item)
    {
        if (item == null)
            return false;

        switch (currentFilter)
        {
            case PackageFilter.Equipment:
                return item.itemType == ItemType.Equipment;
            case PackageFilter.Material:
                return item.itemType == ItemType.Material;
            case PackageFilter.Item:
                return item.itemType == ItemType.Item;
            default:
                return true;
        }
    }

    private bool TryBindDesignedItemSlots()
    {
        RectTransform slotRoot = FindDesignedSlotRoot();

        if (slotRoot == null)
            return false;

        List<RectTransform> slotTransforms = new List<RectTransform>();
        CollectDesignedSlots(slotRoot, slotTransforms);

        if (slotTransforms.Count == 0)
            return false;

        slotTransforms.Sort(CompareSlotsByVisualOrder);
        cells.Clear();

        for (int i = 0; i < slotTransforms.Count; i++)
        {
            UI_PackageItemCell cell = slotTransforms[i].GetComponent<UI_PackageItemCell>();

            if (cell == null)
                cell = slotTransforms[i].gameObject.AddComponent<UI_PackageItemCell>();

            cells.Add(cell);
        }

        contentRoot = slotRoot;
        return true;
    }

    private RectTransform FindDesignedSlotRoot()
    {
        Transform border = FindDeepChild(transform, "边框");
        return border as RectTransform;
    }

    private void CollectDesignedSlots(Transform root, List<RectTransform> result)
    {
        if (root == null || result == null)
            return;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == null)
                continue;

            if (!child.name.Contains("物品框"))
                continue;

            RectTransform rectTransform = child as RectTransform;

            if (rectTransform != null)
                result.Add(rectTransform);
        }
    }

    private int CompareSlotsByVisualOrder(RectTransform left, RectTransform right)
    {
        if (left == null || right == null)
            return 0;

        Vector2 leftPosition = left.anchoredPosition;
        Vector2 rightPosition = right.anchoredPosition;
        float yDifference = rightPosition.y - leftPosition.y;

        if (Mathf.Abs(yDifference) > 8f)
            return yDifference > 0f ? 1 : -1;

        return leftPosition.x.CompareTo(rightPosition.x);
    }

    private void EnsureScrollView()
    {
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (scrollRect == null)
            scrollRect = CreateScrollView();

        if (contentRoot == null)
            contentRoot = scrollRect.content;

        if (contentRoot == null)
            contentRoot = CreateContent(scrollRect);

        EnsureContentLayout();
    }

    private ScrollRect CreateScrollView()
    {
        GameObject scrollObject = new GameObject("Package Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(transform, false);

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = scrollAnchorMin;
        scrollRectTransform.anchorMax = scrollAnchorMax;
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image background = scrollObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.18f);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);

        RectTransform viewportTransform = viewportObject.GetComponent<RectTransform>();
        viewportTransform.anchorMin = Vector2.zero;
        viewportTransform.anchorMax = Vector2.one;
        viewportTransform.offsetMin = Vector2.zero;
        viewportTransform.offsetMax = Vector2.zero;

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportImage.raycastTarget = true;

        ScrollRect created = scrollObject.GetComponent<ScrollRect>();
        created.viewport = viewportTransform;
        created.horizontal = false;
        created.vertical = true;
        created.movementType = ScrollRect.MovementType.Clamped;
        created.scrollSensitivity = 35f;
        created.content = CreateContent(created);

        return created;
    }

    private RectTransform CreateContent(ScrollRect owner)
    {
        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(owner.viewport != null ? owner.viewport : owner.transform, false);

        RectTransform contentTransform = contentObject.GetComponent<RectTransform>();
        contentTransform.anchorMin = new Vector2(0f, 1f);
        contentTransform.anchorMax = new Vector2(1f, 1f);
        contentTransform.pivot = new Vector2(0.5f, 1f);
        contentTransform.offsetMin = Vector2.zero;
        contentTransform.offsetMax = Vector2.zero;
        contentTransform.anchoredPosition = Vector2.zero;

        owner.content = contentTransform;
        return contentTransform;
    }

    private void EnsureContentLayout()
    {
        GridLayoutGroup grid = contentRoot.GetComponent<GridLayoutGroup>();

        if (grid == null)
            grid = contentRoot.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = cellSize;
        grid.spacing = cellSpacing;
        grid.padding = new RectOffset(16, 16, 16, 16);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = fixedColumnCount > 0 ? GridLayoutGroup.Constraint.FixedColumnCount : GridLayoutGroup.Constraint.Flexible;
        grid.constraintCount = Mathf.Max(1, fixedColumnCount);

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();

        if (fitter == null)
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void BindFilterButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string text = label != null ? label.text : button.gameObject.name;
            PackageFilter? filter = ResolveFilter(text);

            if (!filter.HasValue)
                continue;

            PackageFilter capturedFilter = filter.Value;
            button.onClick.RemoveListener(Refresh);
            button.onClick.AddListener(() => SetFilter(capturedFilter));
        }
    }

    private PackageFilter? ResolveFilter(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (text.Contains("全部") || text.Contains("All"))
            return PackageFilter.All;

        if (text.Contains("装备") || text.Contains("Equipment"))
            return PackageFilter.Equipment;

        if (text.Contains("材料") || text.Contains("Material"))
            return PackageFilter.Material;

        if (text.Contains("道具") || text.Contains("Item"))
            return PackageFilter.Item;

        return null;
    }

    private void SetFilter(PackageFilter filter)
    {
        currentFilter = filter;
        Refresh();
    }

    private void EnsureCellCount(int count)
    {
        while (cells.Count < count)
            cells.Add(CreateCell());
    }

    private UI_PackageItemCell CreateCell()
    {
        GameObject cellObject = new GameObject("Package Item Cell", typeof(RectTransform), typeof(Image), typeof(UI_PackageItemCell));
        cellObject.transform.SetParent(contentRoot, false);

        RectTransform rectTransform = cellObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = cellSize;

        Image background = cellObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.02f, 0.02f, 0.55f);
        background.raycastTarget = true;

        CreateIcon(cellObject.transform);
        CreateAmountText(cellObject.transform);
        CreateNameText(cellObject.transform);

        return cellObject.GetComponent<UI_PackageItemCell>();
    }

    private void CreateIcon(Transform parent)
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(parent, false);

        RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -8f);
        rectTransform.sizeDelta = new Vector2(78f, 78f);

        Image image = iconObject.GetComponent<Image>();
        image.raycastTarget = false;
    }

    private void CreateAmountText(Transform parent)
    {
        TextMeshProUGUI text = CreateText(parent, "Amount", 24, TextAlignmentOptions.BottomRight);
        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(1f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(1f, 0f);
        rectTransform.anchoredPosition = new Vector2(-8f, 32f);
        rectTransform.sizeDelta = new Vector2(52f, 28f);
    }

    private void CreateNameText(Transform parent)
    {
        TextMeshProUGUI text = CreateText(parent, "Name", 20, TextAlignmentOptions.Center);
        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 4f);
        rectTransform.sizeDelta = new Vector2(-8f, 30f);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, int size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font != null ? font : text.font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        return text;
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
                continue;

            if (child.name == childName)
                return child;

            Transform nested = FindDeepChild(child, childName);

            if (nested != null)
                return nested;
        }

        return null;
    }
}
