using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Items/Main Flask Upgrade Material System")]
public class MainFlaskUpgradeMaterialSystem : MonoBehaviour
{
    [Serializable]
    public class MaterialSlot
    {
        public GameObject root;
        public Image icon;
        public TMP_Text amountText;
    }

    [Serializable]
    public class FlaskUpgradeCost
    {
        public ItemData material;
        [Min(1)] public int amount = 1;
    }

    [Serializable]
    public class HealingUpgradeStep
    {
        [Range(0.01f, 1f)] public float healPercentIncrease = 0.1f;
        public List<FlaskUpgradeCost> requiredMaterials = new List<FlaskUpgradeCost>();
    }

    [Serializable]
    public class CapacityUpgradeStep
    {
        [Min(1)] public int capacityIncrease = 1;
        public List<FlaskUpgradeCost> requiredMaterials = new List<FlaskUpgradeCost>();
    }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text healingPreviewText;
    [SerializeField] private TMP_Text capacityPreviewText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button healingUpgradeButton;
    [SerializeField] private Button capacityUpgradeButton;

    [Header("Material Requirement UI")]
    [SerializeField] private Transform healingMaterialsRoot;
    [SerializeField] private Transform capacityMaterialsRoot;
    [SerializeField] private List<MaterialSlot> healingMaterialSlots = new List<MaterialSlot>();
    [SerializeField] private List<MaterialSlot> capacityMaterialSlots = new List<MaterialSlot>();
    [SerializeField] private Color enoughMaterialColor = Color.white;
    [SerializeField] private Color missingMaterialColor = new Color(1f, 0.35f, 0.25f, 1f);

    [Header("Healing Upgrade")]
    [SerializeField] private List<HealingUpgradeStep> healingUpgradeSteps = new List<HealingUpgradeStep>
    {
        new HealingUpgradeStep { healPercentIncrease = 0.1f }
    };

    [Header("Capacity Upgrade")]
    [SerializeField] private List<CapacityUpgradeStep> capacityUpgradeSteps = new List<CapacityUpgradeStep>
    {
        new CapacityUpgradeStep { capacityIncrease = 1 }
    };

    [Header("Message")]
    [SerializeField, Min(0f)] private float messageDuration = 2f;

    private PlayerFlaskSystem flaskSystem;
    private float hideMessageAt;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
        Refresh();
    }

    private void OnValidate()
    {
        if (healingUpgradeSteps == null)
            healingUpgradeSteps = new List<HealingUpgradeStep>();

        if (capacityUpgradeSteps == null)
            capacityUpgradeSteps = new List<CapacityUpgradeStep>();

        if (healingMaterialSlots == null)
            healingMaterialSlots = new List<MaterialSlot>();

        if (capacityMaterialSlots == null)
            capacityMaterialSlots = new List<MaterialSlot>();
    }

    private void Update()
    {
        if (messageText != null && messageText.gameObject.activeSelf && hideMessageAt > 0f && Time.unscaledTime >= hideMessageAt)
            messageText.gameObject.SetActive(false);
    }

    public void Open()
    {
        ResolveReferences();

        UI_NpcInteractionMenu.Instance?.Hide();
        UI_DialogueConversationSystem.Instance?.Hide();

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        UI_NpcPanelUtility.ActivatePanel(root);
        root.gameObject.SetActive(true);

        BindButtons();
        Refresh();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void UpgradeHealing()
    {
        ResolveReferences();

        if (flaskSystem == null)
        {
            ShowMessage("\u672a\u627e\u5230\u6b8b\u606f\u9732\u6ef4\u7cfb\u7edf");
            return;
        }

        int level = flaskSystem.MainHealingUpgradeLevel;

        if (level >= healingUpgradeSteps.Count)
        {
            ShowMessage("\u6062\u590d\u91cf\u5df2\u8fbe\u5230\u4e0a\u9650");
            return;
        }

        HealingUpgradeStep step = healingUpgradeSteps[level];

        if (!HasRequiredMaterials(step != null ? step.requiredMaterials : null))
        {
            ShowMessage("\u7f3a\u5c11\u6240\u9700\u6750\u6599");
            return;
        }

        ConsumeMaterials(step != null ? step.requiredMaterials : null);
        flaskSystem.UpgradeMainFlaskHealing(step != null ? step.healPercentIncrease : 0.1f);
        Refresh();
        ShowMessage("\u51dd\u7ed3\u5b8c\u6210");
    }

    public void UpgradeCapacity()
    {
        ResolveReferences();

        if (flaskSystem == null)
        {
            ShowMessage("\u672a\u627e\u5230\u6b8b\u606f\u9732\u6ef4\u7cfb\u7edf");
            return;
        }

        int level = flaskSystem.MainCapacityUpgradeLevel;

        if (level >= capacityUpgradeSteps.Count)
        {
            ShowMessage("\u6570\u91cf\u5df2\u8fbe\u5230\u4e0a\u9650");
            return;
        }

        CapacityUpgradeStep step = capacityUpgradeSteps[level];

        if (!HasRequiredMaterials(step != null ? step.requiredMaterials : null))
        {
            ShowMessage("\u7f3a\u5c11\u6240\u9700\u6750\u6599");
            return;
        }

        ConsumeMaterials(step != null ? step.requiredMaterials : null);
        flaskSystem.UpgradeMainFlaskCapacity(step != null ? step.capacityIncrease : 1);
        Refresh();
        ShowMessage("\u51dd\u7ed3\u5b8c\u6210");
    }

    public void Refresh()
    {
        ResolveReferences();

        if (flaskSystem == null)
            return;

        int healingLevel = flaskSystem.MainHealingUpgradeLevel;
        int capacityLevel = flaskSystem.MainCapacityUpgradeLevel;

        int currentHeal = Mathf.RoundToInt(flaskSystem.MainHealPercent * 100f);
        int nextHeal = currentHeal;

        if (healingLevel < healingUpgradeSteps.Count && healingUpgradeSteps[healingLevel] != null)
        {
            float increase = healingUpgradeSteps[healingLevel].healPercentIncrease;
            nextHeal = Mathf.RoundToInt(Mathf.Clamp01(flaskSystem.MainHealPercent + increase) * 100f);
        }

        int currentCapacity = flaskSystem.MainMaxCharges;
        int nextCapacity = currentCapacity;

        if (capacityLevel < capacityUpgradeSteps.Count && capacityUpgradeSteps[capacityLevel] != null)
            nextCapacity = currentCapacity + capacityUpgradeSteps[capacityLevel].capacityIncrease;

        if (healingPreviewText != null)
            healingPreviewText.text = $"{currentHeal}% \u2192 {nextHeal}%";

        if (capacityPreviewText != null)
            capacityPreviewText.text = $"{currentCapacity} \u2192 {nextCapacity}";

        if (healingUpgradeButton != null)
            healingUpgradeButton.interactable = healingLevel < healingUpgradeSteps.Count;

        if (capacityUpgradeButton != null)
            capacityUpgradeButton.interactable = capacityLevel < capacityUpgradeSteps.Count;

        RefreshMaterialSlots(healingMaterialSlots, GetHealingCosts(healingLevel));
        RefreshMaterialSlots(capacityMaterialSlots, GetCapacityCosts(capacityLevel));
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (flaskSystem == null)
            flaskSystem = PlayerFlaskSystem.GetOrCreate();

        if (healingPreviewText == null)
            healingPreviewText = FindPreviewText("\u6062\u590d\u91cf", "\u56de\u590d\u91cf", "%", "heal", "Healing");

        if (capacityPreviewText == null)
            capacityPreviewText = FindPreviewText("\u6570\u91cf", "\u6b21\u6570", "\u74f6", "capacity", "Capacity");

        if (messageText == null)
            messageText = FindText("\u63d0\u793a", "\u7f3a\u5c11", "Message", "message");

        if (messageText != null && hideMessageAt <= 0f)
            messageText.gameObject.SetActive(false);

        if (healingUpgradeButton == null || capacityUpgradeButton == null)
            AutoBindUpgradeButtons();

        AutoBindMaterialRequirementSlots();
    }

    private void BindButtons()
    {
        if (healingUpgradeButton != null)
        {
            healingUpgradeButton.onClick.RemoveListener(UpgradeHealing);
            healingUpgradeButton.onClick.AddListener(UpgradeHealing);
        }

        if (capacityUpgradeButton != null)
        {
            capacityUpgradeButton.onClick.RemoveListener(UpgradeCapacity);
            capacityUpgradeButton.onClick.AddListener(UpgradeCapacity);
        }
    }

    private void AutoBindUpgradeButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> upgradeButtons = new List<Button>();

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            string searchText = GetSearchText(button.transform);

            if (searchText.Contains("\u51dd\u7ed3") || searchText.Contains("Upgrade") || searchText.Contains("upgrade"))
                upgradeButtons.Add(button);
        }

        upgradeButtons.Sort((a, b) =>
        {
            float ay = a != null ? a.transform.position.y : 0f;
            float by = b != null ? b.transform.position.y : 0f;
            return by.CompareTo(ay);
        });

        if (healingUpgradeButton == null && upgradeButtons.Count > 0)
            healingUpgradeButton = upgradeButtons[0];

        if (capacityUpgradeButton == null && upgradeButtons.Count > 1)
            capacityUpgradeButton = upgradeButtons[1];
    }

    private void AutoBindMaterialRequirementSlots()
    {
        Transform root = panelRoot != null ? panelRoot.transform : transform;

        if (healingMaterialsRoot == null || capacityMaterialsRoot == null)
            AutoBindMaterialRoots(root);

        if (!HasUsableMaterialSlots(healingMaterialSlots) && healingMaterialsRoot != null)
            healingMaterialSlots = BuildMaterialSlots(healingMaterialsRoot);

        if (!HasUsableMaterialSlots(capacityMaterialSlots) && capacityMaterialsRoot != null)
            capacityMaterialSlots = BuildMaterialSlots(capacityMaterialsRoot);
    }

    private void AutoBindMaterialRoots(Transform root)
    {
        if (root == null)
            return;

        GridLayoutGroup[] grids = root.GetComponentsInChildren<GridLayoutGroup>(true);
        List<Transform> materialRows = new List<Transform>();

        for (int i = 0; i < grids.Length; i++)
        {
            GridLayoutGroup grid = grids[i];

            if (grid == null)
                continue;

            string rowName = grid.gameObject.name ?? string.Empty;

            if (!rowName.Contains("\u6750\u6599\u680f") && !rowName.Contains("\u6750\u6599"))
                continue;

            if (GetMaterialSlotChildren(grid.transform).Count > 0)
                materialRows.Add(grid.transform);
        }

        materialRows.Sort((a, b) =>
        {
            float ay = a != null ? a.position.y : 0f;
            float by = b != null ? b.position.y : 0f;
            return by.CompareTo(ay);
        });

        if (healingMaterialsRoot == null && materialRows.Count > 0)
            healingMaterialsRoot = materialRows[0];

        if (capacityMaterialsRoot == null && materialRows.Count > 1)
            capacityMaterialsRoot = materialRows[1];
    }

    private List<MaterialSlot> BuildMaterialSlots(Transform root)
    {
        List<MaterialSlot> slots = new List<MaterialSlot>();
        List<Transform> slotChildren = GetMaterialSlotChildren(root);

        slotChildren.Sort((a, b) =>
        {
            RectTransform ar = a as RectTransform;
            RectTransform br = b as RectTransform;

            if (ar != null && br != null)
            {
                int xCompare = ar.anchoredPosition.x.CompareTo(br.anchoredPosition.x);

                if (xCompare != 0)
                    return xCompare;
            }

            return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
        });

        for (int i = 0; i < slotChildren.Count; i++)
        {
            Transform child = slotChildren[i];
            Image icon = child.GetComponent<Image>();

            if (icon == null)
                continue;

            MaterialSlot slot = new MaterialSlot
            {
                root = child.gameObject,
                icon = icon,
                amountText = child.GetComponentInChildren<TMP_Text>(true)
            };

            slots.Add(slot);
        }

        return slots;
    }

    private List<Transform> GetMaterialSlotChildren(Transform root)
    {
        List<Transform> children = new List<Transform>();

        if (root == null)
            return children;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == null)
                continue;

            Image image = child.GetComponent<Image>();
            string childName = child.gameObject.name ?? string.Empty;

            if (image != null && childName.Contains("\u6750\u6599"))
                children.Add(child);
        }

        return children;
    }

    private bool HasUsableMaterialSlots(List<MaterialSlot> slots)
    {
        if (slots == null || slots.Count <= 0)
            return false;

        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null && slots[i].icon != null)
                return true;

        return false;
    }

    private List<FlaskUpgradeCost> GetHealingCosts(int healingLevel)
    {
        if (healingLevel < 0 || healingLevel >= healingUpgradeSteps.Count || healingUpgradeSteps[healingLevel] == null)
            return null;

        return healingUpgradeSteps[healingLevel].requiredMaterials;
    }

    private List<FlaskUpgradeCost> GetCapacityCosts(int capacityLevel)
    {
        if (capacityLevel < 0 || capacityLevel >= capacityUpgradeSteps.Count || capacityUpgradeSteps[capacityLevel] == null)
            return null;

        return capacityUpgradeSteps[capacityLevel].requiredMaterials;
    }

    private void RefreshMaterialSlots(List<MaterialSlot> slots, List<FlaskUpgradeCost> costs)
    {
        if (slots == null || slots.Count <= 0)
            return;

        int visibleCostIndex = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            MaterialSlot slot = slots[i];

            if (slot == null)
                continue;

            FlaskUpgradeCost cost = GetVisibleCost(costs, ref visibleCostIndex);

            if (cost == null || cost.material == null)
            {
                SetMaterialSlotVisible(slot, false);
                continue;
            }

            SetMaterialSlotVisible(slot, true);

            if (slot.icon != null)
            {
                slot.icon.sprite = cost.material.itemicon;
                slot.icon.color = cost.material.itemicon != null ? Color.white : Color.clear;
                slot.icon.preserveAspect = true;
            }

            if (slot.amountText != null)
            {
                int requiredAmount = Mathf.Max(1, cost.amount);
                bool enough = CountMaterial(cost.material) >= requiredAmount;
                slot.amountText.text = requiredAmount.ToString();
                slot.amountText.color = enough ? enoughMaterialColor : missingMaterialColor;
            }
        }
    }

    private FlaskUpgradeCost GetVisibleCost(List<FlaskUpgradeCost> costs, ref int index)
    {
        if (costs == null)
            return null;

        while (index < costs.Count)
        {
            FlaskUpgradeCost cost = costs[index];
            index++;

            if (cost != null && cost.material != null)
                return cost;
        }

        return null;
    }

    private void SetMaterialSlotVisible(MaterialSlot slot, bool visible)
    {
        if (slot == null)
            return;

        if (slot.root != null)
        {
            slot.root.SetActive(visible);
            return;
        }

        if (slot.icon != null)
            slot.icon.gameObject.SetActive(visible);
    }

    private TMP_Text FindPreviewText(params string[] hints)
    {
        TMP_Text direct = FindValueText(hints);

        if (direct != null)
            return direct;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text != null && (text.text.Contains("\u2192") || text.text.Contains("->")))
                return text;
        }

        return null;
    }

    private TMP_Text FindValueText(params string[] hints)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null || text.GetComponentInParent<Button>() != null)
                continue;

            string searchText = GetSearchText(text.transform);

            if (!LooksLikeValueText(text, searchText))
                continue;

            for (int h = 0; h < hints.Length; h++)
                if (!string.IsNullOrEmpty(hints[h]) && searchText.Contains(hints[h]))
                    return text;
        }

        return null;
    }

    private bool LooksLikeValueText(TMP_Text text, string searchText)
    {
        string value = text != null ? text.text ?? string.Empty : string.Empty;
        string objectName = text != null ? text.gameObject.name ?? string.Empty : string.Empty;

        return ContainsDigit(value) ||
               value.Contains("%") ||
               value.Contains("\u2192") ||
               value.Contains("->") ||
               objectName.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Value", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.Contains("\u6570\u503c") ||
               searchText.Contains("%");
    }

    private bool ContainsDigit(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
            if (char.IsDigit(value[i]))
                return true;

        return false;
    }

    private TMP_Text FindText(params string[] hints)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
                continue;

            string searchText = GetSearchText(text.transform);

            for (int h = 0; h < hints.Length; h++)
                if (!string.IsNullOrEmpty(hints[h]) && searchText.Contains(hints[h]))
                    return text;
        }

        return null;
    }

    private string GetSearchText(Transform root)
    {
        if (root == null)
            return string.Empty;

        string searchText = root.name;
        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < labels.Length; i++)
            if (labels[i] != null)
                searchText += labels[i].text;

        return searchText;
    }

    private bool HasRequiredMaterials(List<FlaskUpgradeCost> costs)
    {
        if (costs == null || costs.Count <= 0)
            return true;

        for (int i = 0; i < costs.Count; i++)
        {
            FlaskUpgradeCost cost = costs[i];

            if (cost == null || cost.material == null)
                continue;

            if (CountMaterial(cost.material) < Mathf.Max(1, cost.amount))
                return false;
        }

        return true;
    }

    private void ConsumeMaterials(List<FlaskUpgradeCost> costs)
    {
        if (Inventory.instance == null || costs == null)
            return;

        for (int i = 0; i < costs.Count; i++)
        {
            FlaskUpgradeCost cost = costs[i];

            if (cost == null || cost.material == null)
                continue;

            int amount = Mathf.Max(1, cost.amount);

            for (int c = 0; c < amount; c++)
                Inventory.instance.RemoveItem(cost.material);
        }
    }

    private int CountMaterial(ItemData material)
    {
        if (material == null || Inventory.instance == null)
            return 0;

        return CountInList(Inventory.instance.GetStashList(), material) +
               CountInList(Inventory.instance.GetInventoryList(), material);
    }

    private int CountInList(List<InventoryItem> items, ItemData material)
    {
        if (items == null)
            return 0;

        int count = 0;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];

            if (item != null && item.data == material)
                count += Mathf.Max(0, item.stackSize);
        }

        return count;
    }

    private void ShowMessage(string message)
    {
        if (messageText == null)
        {
            Debug.Log(message, this);
            return;
        }

        messageText.text = message;
        messageText.gameObject.SetActive(true);
        messageText.transform.SetAsLastSibling();
        hideMessageAt = Time.unscaledTime + messageDuration;
    }
}
