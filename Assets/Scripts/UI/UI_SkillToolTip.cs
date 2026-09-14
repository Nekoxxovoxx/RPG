using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillToolTip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI skillText;
    [SerializeField] private TextMeshProUGUI skillName;
    [SerializeField] private TextMeshProUGUI unlockConditionText;
    [SerializeField] private TextMeshProUGUI unlockCostText;
    [SerializeField] private Vector2 mouseOffset = new Vector2(150, 150);
    [SerializeField] private Vector2 screenPadding = new Vector2(12, 12);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ResolveOptionalTextReferences();
        MakeTooltipIgnoreRaycasts();
    }

    private void Update()
    {
        UpdateToolTipPosition();
    }

    public void ShowToolTip(string _skillDescprtion,string _skillName)
    {
        ShowToolTip(_skillDescprtion, _skillName, "", "");
    }

    public void ShowToolTip(string _skillDescprtion, string _skillName, string _unlockCondition, string _unlockCost)
    {
        ResolveOptionalTextReferences();

        skillName.text = _skillName;
        skillText.text = BuildSkillDescription(_skillDescprtion, _unlockCondition, _unlockCost);

        SetOptionalText(unlockConditionText, "解锁条件：", _unlockCondition);
        SetOptionalText(unlockCostText, "解锁消耗：", _unlockCost);

        MakeTooltipIgnoreRaycasts();
        gameObject.SetActive(true);

        if (rectTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        UpdateToolTipPosition();
    }

    public void UpdateToolTipPosition()
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

    public void HideToolTip() => gameObject.SetActive(false);

    private string BuildSkillDescription(string skillDescription, string unlockCondition, string unlockCost)
    {
        bool hasDedicatedConditionText = unlockConditionText != null;
        bool hasDedicatedCostText = unlockCostText != null;

        if (hasDedicatedConditionText && hasDedicatedCostText)
            return skillDescription;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (!string.IsNullOrWhiteSpace(skillDescription))
            sb.Append(skillDescription.Trim());

        if (!hasDedicatedConditionText && !string.IsNullOrWhiteSpace(unlockCondition))
        {
            AppendSectionSpacing(sb);
            sb.Append("解锁条件：").Append(unlockCondition.Trim());
        }

        if (!hasDedicatedCostText && !string.IsNullOrWhiteSpace(unlockCost))
        {
            AppendSectionSpacing(sb);
            sb.Append("解锁消耗：").Append(unlockCost.Trim());
        }

        return sb.ToString();
    }

    private void AppendSectionSpacing(System.Text.StringBuilder sb)
    {
        if (sb.Length > 0)
            sb.AppendLine().AppendLine();
    }

    private void SetOptionalText(TextMeshProUGUI targetText, string title, string value)
    {
        if (targetText == null)
            return;

        bool hasValue = !string.IsNullOrWhiteSpace(value);
        targetText.gameObject.SetActive(hasValue);
        targetText.text = hasValue ? title + value.Trim() : "";
    }

    private void ResolveOptionalTextReferences()
    {
        if (unlockConditionText != null && unlockCostText != null)
            return;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];

            if (text == null)
                continue;

            string objectName = text.gameObject.name;

            if (unlockConditionText == null && IsUnlockConditionTextName(objectName))
                unlockConditionText = text;

            if (unlockCostText == null && IsUnlockCostTextName(objectName))
                unlockCostText = text;
        }
    }

    private bool IsUnlockConditionTextName(string objectName)
    {
        return objectName.Contains("解锁条件") ||
               objectName.Contains("UnlockCondition") ||
               objectName.Contains("Condition");
    }

    private bool IsUnlockCostTextName(string objectName)
    {
        return objectName.Contains("解锁消耗") ||
               objectName.Contains("UnlockCost") ||
               objectName.Contains("Cost");
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
    
}
