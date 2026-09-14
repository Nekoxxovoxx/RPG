using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UI_LevelUpPanel : MonoBehaviour
{
    [Header("升级消耗")]
    [SerializeField, Min(0)] private int baseUpgradeCost = 10;
    [FormerlySerializedAs("costGrowthPerLevel")]
    [SerializeField, Min(0)] private int linearGrowth = 5;
    [SerializeField, Min(0)] private int curveGrowth = 1;

    [Header("界面文本")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text currentEmbersText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text messageText;

    [Header("界面按钮")]
    [SerializeField] private Button confirmButton;
    [FormerlySerializedAs("resetButton")]
    [SerializeField] private Button reforgeButton;

    [Header("弹窗")]
    [SerializeField] private UI_UpgradeConfirmPopup confirmPopup;

    private readonly UpgradePreviewData preview = new UpgradePreviewData();
    private readonly List<UI_StatSlot> primarySlots = new List<UI_StatSlot>();
    private readonly List<UI_StatSlot> derivedSlots = new List<UI_StatSlot>();
    private readonly Dictionary<StatType, Button> plusButtons = new Dictionary<StatType, Button>();
    private readonly Dictionary<StatType, Button> minusButtons = new Dictionary<StatType, Button>();

    private PlayerStats playerStats;
    private PlayerEmberWallet emberWallet;
    private TMP_FontAsset resolvedFont;
    private float hideMessageAt;

    public void Open()
    {
        ResolveReferences();
        preview.Clear();
        RefreshAll();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeWallet();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (emberWallet != null)
            emberWallet.OnEmbersChanged -= HandleEmbersChanged;
    }

    private void Update()
    {
        if (messageText != null && messageText.gameObject.activeSelf && hideMessageAt > 0f && Time.unscaledTime >= hideMessageAt)
            messageText.gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (playerStats == null && PlayerManager.instance != null && PlayerManager.instance.player != null)
            playerStats = PlayerManager.instance.player.GetComponent<PlayerStats>();

        if (emberWallet == null)
            emberWallet = PlayerEmberWallet.GetOrCreate();

        ResolveTexts();
        ResolveStatSlots();
        ResolveButtons();
        NormalizeRaycastTargets();
        SubscribeWallet();
    }

    private void ResolveTexts()
    {
        if (currentEmbersText == null)
            currentEmbersText = FindTextByObjectName("当前持有余烬");

        if (upgradeCostText == null)
            upgradeCostText = FindTextByObjectName("升级所需余烬");

        if (levelText == null)
            levelText = EnsureLevelText();

        if (messageText == null)
            messageText = EnsureMessageText();

        if (messageText != null)
            messageText.gameObject.SetActive(false);

        if (confirmPopup == null)
            confirmPopup = UI_UpgradeConfirmPopup.GetOrCreate(transform, ResolveFont());

        if (confirmPopup != null)
            confirmPopup.Hide();
    }

    private void ResolveStatSlots()
    {
        primarySlots.Clear();
        derivedSlots.Clear();
        plusButtons.Clear();
        minusButtons.Clear();

        UI_StatSlot[] statSlots = GetComponentsInChildren<UI_StatSlot>(true);

        for (int i = 0; i < statSlots.Length; i++)
        {
            UI_StatSlot slot = statSlots[i];

            if (slot == null)
                continue;

            RestoreSlotTexts(slot);

            if (UpgradeCalculator.IsPrimaryStat(slot.StatType))
            {
                primarySlots.Add(slot);
                BindPrimaryAdjustButtons(slot);
            }
            else
            {
                derivedSlots.Add(slot);
                DisableGeneratedRow(slot.transform.Find("UpgradeDerivedRow"));
            }
        }
    }

    private void ResolveButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            string searchText = GetButtonSearchText(button);

            if (ContainsAny(searchText, "祭礼", "绁ぜ"))
                confirmButton = button;
            else if (ContainsAny(searchText, "重铸", "重鑄", "閲嶉摳"))
                reforgeButton = button;
        }

        BindActionButton(confirmButton, ConfirmUpgrade);
        BindActionButton(reforgeButton, ConfirmReforge);
    }

    private void BindActionButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.enabled = true;
        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        ApplyMenuHeaderButtonFeedback(button);
    }

    private void SubscribeWallet()
    {
        if (emberWallet == null)
            return;

        emberWallet.OnEmbersChanged -= HandleEmbersChanged;
        emberWallet.OnEmbersChanged += HandleEmbersChanged;
    }

    private void HandleEmbersChanged(int _)
    {
        RefreshEmbers();
    }

    private void AddPendingPoint(StatType statType)
    {
        preview.AddPending(statType);
        RefreshAll();
    }

    private void RemovePendingPoint(StatType statType)
    {
        if (preview.RemovePending(statType))
            RefreshAll();
    }

    private void ConfirmUpgrade()
    {
        ResolveReferences();

        if (playerStats == null)
        {
            ShowMessage("未找到玩家属性数据");
            return;
        }

        if (preview.TotalPendingPoints <= 0)
        {
            ShowMessage("请先选择要进行洗礼的属性");
            return;
        }

        int cost = GetPendingUpgradeCost();

        if (emberWallet == null || !emberWallet.TrySpendEmbers(cost))
        {
            ShowMessage("当前升级所需余烬不足");
            return;
        }

        playerStats.ApplyPermanentUpgrade(preview, cost);
        preview.Clear();
        RefreshAll();
        ShowMessage("祭礼完成");
    }

    private void ConfirmReforge()
    {
        ResolveReferences();

        if (playerStats == null)
        {
            ShowMessage("未找到玩家属性数据");
            return;
        }

        int refund = playerStats.ReforgeLevelProgress();

        if (refund > 0)
            emberWallet?.AddEmbers(refund);

        preview.Clear();
        RefreshAll();
        ShowMessage(refund > 0 ? $"重铸完成，返还余烬：{refund}" : "重铸完成");
    }

    private void ApplyMenuHeaderButtonFeedback(Button button)
    {
        if (button == null)
            return;

        Graphic targetGraphic = ResolveButtonGraphic(button);

        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        colors.pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);
        colors.selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        colors.disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        if (targetGraphic != null)
            targetGraphic.raycastTarget = true;

        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < labels.Length; i++)
            labels[i].raycastTarget = false;
    }

    private void RefreshAll()
    {
        ResolveReferencesIfNeeded();
        RefreshPrimaryStats();
        RefreshDerivedStats();
        RefreshLevel();
        RefreshEmbers();
    }

    private void ResolveReferencesIfNeeded()
    {
        if (playerStats == null || emberWallet == null || levelText == null || currentEmbersText == null || upgradeCostText == null)
            ResolveReferences();
    }

    private void RefreshPrimaryStats()
    {
        for (int i = 0; i < primarySlots.Count; i++)
        {
            UI_StatSlot slot = primarySlots[i];

            if (slot == null)
                continue;

            int current = UpgradeCalculator.GetCurrentStatValue(playerStats, slot.StatType);
            int previewValue = UpgradeCalculator.GetPreviewStatValue(playerStats, preview, slot.StatType);
            int pending = preview.GetPending(slot.StatType);

            string suffix = slot.IsPercent ? "%" : "";
            SetSlotTexts(slot, $"{previewValue}{suffix}");

            if (minusButtons.TryGetValue(slot.StatType, out Button minusButton) && minusButton != null)
                minusButton.interactable = pending > 0;
        }
    }

    private void RefreshDerivedStats()
    {
        for (int i = 0; i < derivedSlots.Count; i++)
        {
            UI_StatSlot slot = derivedSlots[i];

            if (slot == null)
                continue;

            int previewValue = UpgradeCalculator.GetPreviewStatValue(playerStats, preview, slot.StatType);
            string suffix = slot.IsPercent ? "%" : "";
            SetSlotTexts(slot, $"{previewValue}{suffix}");
        }
    }

    private void RefreshLevel()
    {
        if (levelText == null)
            return;

        int currentLevel = playerStats != null ? playerStats.CurrentLevel : 0;
        levelText.text = $"Level {currentLevel + preview.TotalPendingPoints}";
    }

    private void RefreshEmbers()
    {
        if (emberWallet != null)
            emberWallet.Load();

        if (currentEmbersText != null)
            currentEmbersText.text = $"当前持有余烬：{(emberWallet != null ? emberWallet.CurrentEmbers : 0)}";

        if (upgradeCostText != null)
            upgradeCostText.text = $"升级所需余烬：{GetPendingUpgradeCost()}";
    }

    private int GetPendingUpgradeCost()
    {
        int currentLevel = playerStats != null ? playerStats.CurrentLevel : 0;
        return UpgradeCalculator.CalculateTotalUpgradeCost(currentLevel, preview.TotalPendingPoints, baseUpgradeCost, linearGrowth, curveGrowth);
    }

    private void ShowMessage(string message)
    {
        if (messageText == null)
        {
            Debug.Log(message);
            return;
        }

        messageText.text = message;
        messageText.transform.SetAsLastSibling();
        messageText.gameObject.SetActive(true);
        hideMessageAt = Time.unscaledTime + 2f;
    }

    private void RestoreSlotTexts(UI_StatSlot slot)
    {
        if (slot == null)
            return;

        TMP_Text nameText = slot.StatNameText;

        if (nameText != null)
        {
            nameText.enabled = true;
            nameText.text = GetStatDisplayName(slot);
            nameText.raycastTarget = false;
        }

        TMP_Text valueText = slot.StatValueText;

        if (valueText != null)
        {
            valueText.enabled = true;
            valueText.raycastTarget = false;
        }

        DisableGeneratedRowTexts(slot.transform.Find("UpgradePrimaryRow"));
        DisableGeneratedRow(slot.transform.Find("UpgradeDerivedRow"));
    }

    private void SetSlotTexts(UI_StatSlot slot, string value)
    {
        if (slot == null)
            return;

        if (slot.StatNameText != null)
            slot.StatNameText.text = GetStatDisplayName(slot);

        slot.SetStatValueText(value);
    }

    private void BindPrimaryAdjustButtons(UI_StatSlot slot)
    {
        if (slot == null)
            return;

        Transform rowRoot = slot.transform.Find("UpgradePrimaryRow");

        if (rowRoot == null)
        {
            GameObject rowObject = new GameObject("UpgradePrimaryRow", typeof(RectTransform));
            rowObject.transform.SetParent(slot.transform, false);
            rowRoot = rowObject.transform;

            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero;
            rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;
        }

        DisableGeneratedRowTexts(rowRoot);

        Button minusButton = FindButton(rowRoot, "MinusButton", "-");
        Button plusButton = FindButton(rowRoot, "PlusButton", "+");

        if (minusButton == null)
            minusButton = CreateSmallButton(rowRoot, "MinusButton", "-", new Vector2(82f, 0f));

        if (plusButton == null)
            plusButton = CreateSmallButton(rowRoot, "PlusButton", "+", new Vector2(118f, 0f));

        StatType statType = slot.StatType;

        BindSmallButton(minusButton, () => RemovePendingPoint(statType));
        BindSmallButton(plusButton, () => AddPendingPoint(statType));

        minusButtons[statType] = minusButton;
        plusButtons[statType] = plusButton;
    }

    private void BindSmallButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.enabled = true;
        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);

        Graphic graphic = ResolveButtonGraphic(button);

        if (graphic != null)
            graphic.raycastTarget = true;

        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < labels.Length; i++)
            labels[i].raycastTarget = false;
    }

    private Button FindButton(Transform root, string name, string label)
    {
        if (root == null)
            return null;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            if (button.name == name)
                return button;

            if (GetButtonSearchText(button).Trim() == label)
                return button;
        }

        return null;
    }

    private Button CreateSmallButton(Transform parent, string objectName, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(30f, 30f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.36f, 0.05f, 0.05f, 0.92f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.36f, 0.05f, 0.05f, 0.92f);
        colors.highlightedColor = new Color(0.58f, 0.08f, 0.07f, 1f);
        colors.pressedColor = new Color(0.22f, 0.02f, 0.02f, 1f);
        colors.disabledColor = new Color(0.18f, 0.05f, 0.05f, 0.45f);
        button.colors = colors;

        EnsureButtonLabel(button.transform, label);
        return button;
    }

    private TMP_Text EnsureButtonLabel(Transform parent, string label)
    {
        Transform existing = parent.Find("Text");
        TextMeshProUGUI text;

        if (existing != null)
        {
            text = existing.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        text.text = label;
        text.font = ResolveFont();
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        return text;
    }

    private void DisableGeneratedRow(Transform rowRoot)
    {
        if (rowRoot != null)
            rowRoot.gameObject.SetActive(false);
    }

    private void DisableGeneratedRowTexts(Transform rowRoot)
    {
        if (rowRoot == null)
            return;

        rowRoot.gameObject.SetActive(true);
        TMP_Text[] texts = rowRoot.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null || text.GetComponentInParent<Button>() != null)
                continue;

            text.enabled = false;
            text.text = string.Empty;
            text.raycastTarget = false;
        }
    }

    private void NormalizeRaycastTargets()
    {
        HashSet<Graphic> buttonGraphics = new HashSet<Graphic>();
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Graphic graphic = ResolveButtonGraphic(buttons[i]);

            if (graphic == null)
                continue;

            graphic.raycastTarget = true;
            buttonGraphics.Add(graphic);
        }

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];

            if (graphic == null || buttonGraphics.Contains(graphic))
                continue;

            if (graphic.GetComponentInParent<Button>() != null)
            {
                graphic.raycastTarget = false;
                continue;
            }

            graphic.raycastTarget = false;
        }
    }

    private Graphic ResolveButtonGraphic(Button button)
    {
        if (button == null)
            return null;

        Graphic graphic = button.targetGraphic;

        if (graphic == null)
            graphic = button.GetComponent<Graphic>();

        if (graphic == null)
            graphic = button.GetComponentInChildren<Graphic>(true);

        if (graphic != null)
            button.targetGraphic = graphic;

        return graphic;
    }


    private bool ContainsAny(string text, params string[] patterns)
    {
        if (string.IsNullOrEmpty(text) || patterns == null)
            return false;

        for (int i = 0; i < patterns.Length; i++)
        {
            if (!string.IsNullOrEmpty(patterns[i]) && text.Contains(patterns[i]))
                return true;
        }

        return false;
    }
    private string GetButtonSearchText(Button button)
    {
        if (button == null)
            return string.Empty;

        string searchText = button.gameObject.name;
        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                searchText += texts[i].text;
        }

        return searchText;
    }

    private TMP_Text EnsureLevelText()
    {
        Transform character = transform.Find("Character");
        Transform parent = character != null ? character : transform;
        Transform existing = parent.Find("LevelPreviewText");

        if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
            return existingText;

        TMP_Text[] existingTexts = parent.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < existingTexts.Length; i++)
        {
            TMP_Text candidateText = existingTexts[i];

            if (candidateText == null)
                continue;

            string text = candidateText.text ?? string.Empty;

            if (candidateText.gameObject.name.Contains("Level") || text.Contains("Level"))
                return candidateText;
        }

        GameObject textObject = new GameObject("LevelPreviewText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 26f);
        rectTransform.sizeDelta = new Vector2(300f, 42f);

        TextMeshProUGUI levelPreviewText = textObject.GetComponent<TextMeshProUGUI>();
        levelPreviewText.font = ResolveFont();
        levelPreviewText.fontSize = 24f;
        levelPreviewText.alignment = TextAlignmentOptions.Center;
        levelPreviewText.color = Color.white;
        levelPreviewText.raycastTarget = false;

        return levelPreviewText;
    }

    private TMP_Text EnsureMessageText()
    {
        Transform character = transform.Find("Character");
        Transform parent = character != null ? character : transform;
        TMP_Text existingText = null;
        Transform existing = parent.Find("LevelUpMessageText");

        if (existing != null && existing.TryGetComponent(out TMP_Text foundText))
        {
            ConfigureMessageText(foundText, parent);
            return foundText;
        }

        TMP_Text[] existingTexts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < existingTexts.Length; i++)
        {
            TMP_Text candidate = existingTexts[i];

            if (candidate != null && candidate.gameObject.name == "LevelUpMessageText")
            {
                existingText = candidate;
                break;
            }
        }

        if (existingText != null)
        {
            ConfigureMessageText(existingText, parent);
            return existingText;
        }

        GameObject textObject = new GameObject("LevelUpMessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        ConfigureMessageText(text, parent);

        return text;
    }

    private void ConfigureMessageText(TMP_Text text, Transform parent)
    {
        if (text == null)
            return;

        if (parent != null && text.transform.parent != parent)
            text.transform.SetParent(parent, false);

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, 150f);
        rectTransform.sizeDelta = new Vector2(480f, 48f);

        text.font = ResolveFont();
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.94f, 0.9f, 0.78f, 1f);
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    private TMP_Text FindTextByObjectName(string objectNamePart)
    {
        if (string.IsNullOrWhiteSpace(objectNamePart))
            return null;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
                continue;

            string objectName = text.gameObject != null ? text.gameObject.name : string.Empty;
            string textContent = text.text ?? string.Empty;

            if (objectName.Contains(objectNamePart) || textContent.Contains(objectNamePart))
                return text;
        }

        return null;
    }

    private string GetStatDisplayName(UI_StatSlot slot)
    {
        if (slot != null && !string.IsNullOrWhiteSpace(slot.StatName))
            return slot.StatName;

        return slot != null ? slot.StatType.ToString() : string.Empty;
    }

    private TMP_FontAsset ResolveFont()
    {
        if (resolvedFont != null)
            return resolvedFont;

        TMP_Text existingText = GetComponentInChildren<TMP_Text>(true);

        if (existingText != null)
            resolvedFont = existingText.font;

        if (resolvedFont == null)
            resolvedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/xiangsuziti SDF");

        return resolvedFont;
    }
}



