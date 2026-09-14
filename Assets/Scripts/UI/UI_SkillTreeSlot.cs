using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class UI_SkillTreeSlot : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    private static readonly string[] DeprecatedSkillKeywords = { "\u5E7B\u8C61", "\u6C34\u6676", "\u9ED1\u6D1E" };

    private UI ui;
    private Image skillImage;

    [SerializeField] private string skillName;
    [TextArea]
    [SerializeField] private string skillDescription;
    [TextArea]
    [SerializeField] private string unlockCondition;
    [TextArea]
    [SerializeField] private string unlockCost;
    [SerializeField] private string skillIdOverride;
    [SerializeField] private Color lockedSkillColor;


    public bool unlocked;

    [SerializeField] private UI_SkillTreeSlot[] shouldBeUnlocked;
    [SerializeField] private UI_SkillTreeSlot[] shouldBeLocked;

    public string SkillName => skillName;
    public string SkillDescription => skillDescription;
    public string SkillIdOverride => skillIdOverride;
    public string SkillId => SkillManager.ResolveSkillIdForSlot(this);
    public UI_SkillTreeSlot[] PrerequisiteSlots => shouldBeUnlocked;
    public UI_SkillTreeSlot[] LockedOutSlots => shouldBeLocked;
    public bool IsDeprecated => ShouldHideDeprecatedSkill();
    public int RequiredLevel => ExtractRequiredLevel(unlockCondition);
    public int UnlockCostAmount => ExtractFirstInt(unlockCost);
    public string RequiredScrollName => ExtractRequiredScrollName(unlockCondition);

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(skillIdOverride))
        {
            string resolvedSkillId = SkillManager.ResolveSkillIdForSlot(this);

            if (!string.IsNullOrWhiteSpace(resolvedSkillId))
                skillIdOverride = resolvedSkillId;
        }

        gameObject.name = "SkillTreeSlot_UI -" + skillName;
    }
    private void Start()
    {
        if (ShouldHideDeprecatedSkill())
        {
            gameObject.SetActive(false);
            return;
        }

        skillImage = GetComponent<Image>();
        ui= GetComponentInParent<UI>();

        SkillManager.instance?.RegisterSkillTreeSlot(this);
        RefreshUnlockVisual();

        GetComponent<Button>().onClick.AddListener(() => UnlockSkillSlot());
    }

    public void UnlockSkillSlot()
    {
        SkillManager skillManager = SkillManager.instance;

        if (skillManager == null)
        {
            Debug.LogWarning("SkillManager was not found. Cannot unlock skill.");
            return;
        }

        if (!skillManager.TryUnlockFromSlot(this, out string failureReason))
        {
            Debug.Log("Cannot unlock skill: " + failureReason);
            return;
        }

        RefreshUnlockVisual();
    }

    private bool ShouldHideDeprecatedSkill()
    {
        for (int i = 0; i < DeprecatedSkillKeywords.Length; i++)
        {
            if (!string.IsNullOrEmpty(skillName) && skillName.Contains(DeprecatedSkillKeywords[i]))
                return true;
        }

        return false;
    }

    public void RefreshUnlockVisual()
    {
        if (skillImage == null)
            skillImage = GetComponent<Image>();

        unlocked = SkillManager.instance != null && SkillManager.instance.IsSkillUnlocked(SkillId);

        if (skillImage != null)
            skillImage.color = unlocked ? Color.white : lockedSkillColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ui == null || ui.skillToolTip == null)
            return;

        ui.skillToolTip.ShowToolTip(skillDescription, skillName, unlockCondition, unlockCost);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        ui?.skillToolTip?.UpdateToolTipPosition();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ui?.skillToolTip?.HideToolTip();
    }

    private static int ExtractRequiredLevel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        string marker = "\u7B49\u7EA7\u8FBE\u5230";
        int markerIndex = value.IndexOf(marker, System.StringComparison.Ordinal);

        if (markerIndex < 0)
            return 0;

        return ExtractFirstInt(value.Substring(markerIndex + marker.Length));
    }

    private static int ExtractFirstInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        int result = 0;
        bool foundDigit = false;

        for (int i = 0; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
            {
                if (foundDigit)
                    break;

                continue;
            }

            foundDigit = true;
            result = result * 10 + (value[i] - '0');
        }

        return foundDigit ? result : 0;
    }

    private static string ExtractRequiredScrollName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string[] scrollNames =
        {
            "\u9010\u7070\u5377\u8F74",
            "\u65E7\u5251\u5377\u8F74",
            "\u9006\u950B\u5377\u8F74",
            "\u533F\u5F71\u5377\u8F74",
            "\u71C3\u85AA\u5377\u8F74",
            "\u5760\u65E5\u5377\u8F74"
        };

        for (int i = 0; i < scrollNames.Length; i++)
        {
            if (value.Contains(scrollNames[i]))
                return scrollNames[i];
        }

        return string.Empty;
    }
}
