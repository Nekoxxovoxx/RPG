using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager instance;

    public static class SkillIds
    {
        public const string DashEmberStep = "Dash.EmberStep";
        public const string DashDoubleJump = "Dash.DoubleJump";
        public const string DashDoubleDash = "Dash.DoubleDash";

        public const string PreciseDodge = "PreciseDodge.Base";
        public const string PreciseDodgeFollowUp = "PreciseDodge.FollowUp";

        public const string Sword = "Sword.Base";
        public const string SwordBounce = "Sword.Bounce";
        public const string SwordPierce = "Sword.Pierce";
        public const string SwordSpin = "Sword.Spin";
        public const string SwordSoulRift = "Sword.SoulRift";
        public const string SwordTimeStop = "Sword.TimeStop";

        public const string Parry = "Parry.Base";
        public const string ParryRestore = "Parry.Restore";
        public const string ParryDamage = "Parry.Damage";

        public const string Invisibility = "Invisibility.Base";
        public const string InvisibilityUpgrade = "Invisibility.Upgrade";

        public const string Awakening = "Awakening.Base";
        public const string AwakeningStatUpgrade = "Awakening.StatUpgrade";
        public const string AwakeningDurationUpgrade = "Awakening.DurationUpgrade";

        public const string Sun = "Sun.Base";
        public const string SunDamageUpgrade = "Sun.DamageUpgrade";
        public const string SunDurationUpgrade = "Sun.DurationUpgrade";
        public const string SunMaxTargets = "Sun.MaxTargets";
        public const string SunWideDamage = "Sun.WideDamage";
    }

    private static readonly string[] AllSkillIds =
    {
        SkillIds.DashEmberStep,
        SkillIds.DashDoubleJump,
        SkillIds.DashDoubleDash,
        SkillIds.PreciseDodge,
        SkillIds.PreciseDodgeFollowUp,
        SkillIds.Sword,
        SkillIds.SwordBounce,
        SkillIds.SwordPierce,
        SkillIds.SwordSpin,
        SkillIds.SwordSoulRift,
        SkillIds.SwordTimeStop,
        SkillIds.Parry,
        SkillIds.ParryRestore,
        SkillIds.ParryDamage,
        SkillIds.Invisibility,
        SkillIds.InvisibilityUpgrade,
        SkillIds.Awakening,
        SkillIds.AwakeningStatUpgrade,
        SkillIds.AwakeningDurationUpgrade,
        SkillIds.Sun,
        SkillIds.SunDamageUpgrade,
        SkillIds.SunDurationUpgrade,
        SkillIds.SunMaxTargets,
        SkillIds.SunWideDamage
    };

    private const string SavePrefix = "SkillTree.";

    public Dash_Skill dash { get; private set; }
    public Sword_Skill sword { get; private set; }
    public Parry_Skill parry { get; private set; }  
    public PreciseDodge_Skill preciseDodge { get; private set; }
    public Sun_Skill sun { get; private set; }
    public Awakening_Skill awakening { get; private set; }
    public Invisibility_Skill invisibility { get; private set; }

    private readonly HashSet<string> unlockedSkills = new HashSet<string>();
    private readonly List<UI_SkillTreeSlot> registeredSlots = new List<UI_SkillTreeSlot>();
    private bool unlocksLoaded;

    public event System.Action OnSkillUnlocksChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveSkillComponents();
        LoadUnlockedSkills();
        EnsureDefaultUnlocks();
    }

    private void Start()
    {
        ResolveSkillComponents();
        ApplyUnlockedSkillEffects();
        RefreshRegisteredSlots();
    }

    public void CancelAllActiveSkills()
    {
        if (sun != null)
            sun.CancelActiveSuns();
        awakening?.CancelAwakening();
        invisibility?.CancelInvisibility();
        preciseDodge?.CancelPreciseDodge();
    }

    public void ResetAllCooldowns()
    {
        dash?.ResetCooldown();
        sword?.ResetCooldown();
        parry?.ResetCooldown();
        preciseDodge?.ResetCooldown();
        sun?.ResetCooldown();
        awakening?.ResetCooldown();
        invisibility?.ResetCooldown();
    }

    public static void ResetSavedUnlocksToDefaults()
    {
        for (int i = 0; i < AllSkillIds.Length; i++)
            PlayerPrefs.DeleteKey(GetSaveKey(AllSkillIds[i]));

        PlayerPrefs.SetInt(GetSaveKey(SkillIds.DashEmberStep), 1);
        PlayerPrefs.Save();

        if (instance == null)
            return;

        instance.unlockedSkills.Clear();
        instance.unlockedSkills.Add(SkillIds.DashEmberStep);
        instance.unlocksLoaded = true;
        instance.CancelAllActiveSkills();
        instance.ResetAllCooldowns();
        instance.ApplyUnlockedSkillEffects();
        instance.RefreshRegisteredSlots();
        instance.OnSkillUnlocksChanged?.Invoke();
    }

    public void RegisterSkillTreeSlot(UI_SkillTreeSlot slot)
    {
        if (slot == null || registeredSlots.Contains(slot))
            return;

        registeredSlots.Add(slot);
        slot.RefreshUnlockVisual();
    }

    public bool IsSkillUnlocked(string skillId)
    {
        LoadUnlockedSkills();
        skillId = NormalizeSkillId(skillId);
        return !string.IsNullOrEmpty(skillId) && unlockedSkills.Contains(skillId);
    }

    public bool UnlockSkill(string skillId)
    {
        LoadUnlockedSkills();
        skillId = NormalizeSkillId(skillId);

        if (string.IsNullOrEmpty(skillId))
            return false;

        if (!unlockedSkills.Add(skillId))
            return false;

        PlayerPrefs.SetInt(GetSaveKey(skillId), 1);
        PlayerPrefs.Save();
        ApplyUnlockedSkillEffects();
        RefreshRegisteredSlots();
        OnSkillUnlocksChanged?.Invoke();
        return true;
    }

    public bool TryUnlockFromSlot(UI_SkillTreeSlot slot, out string failureReason)
    {
        failureReason = string.Empty;

        if (slot == null)
        {
            failureReason = "Invalid skill slot.";
            return false;
        }

        string skillId = NormalizeSkillId(slot.SkillId);

        if (string.IsNullOrEmpty(skillId))
        {
            failureReason = "Skill id was not resolved.";
            return false;
        }

        if (IsSkillUnlocked(skillId))
        {
            failureReason = "Skill is already unlocked.";
            return false;
        }

        if (!ArePrerequisitesUnlocked(slot, skillId))
        {
            failureReason = "Skill prerequisites are not met.";
            return false;
        }

        if (IsBlockedByMutualExclusion(slot))
        {
            failureReason = "A mutually exclusive skill is already unlocked.";
            return false;
        }

        int requiredLevel = slot.RequiredLevel;

        if (requiredLevel > 0 && GetPlayerLevel() < requiredLevel)
        {
            failureReason = "Player level is too low.";
            return false;
        }

        string requiredScrollName = slot.RequiredScrollName;

        if (!string.IsNullOrEmpty(requiredScrollName) && !HasItemNamed(requiredScrollName))
        {
            failureReason = "Required scroll is missing.";
            return false;
        }

        int emberCost = slot.UnlockCostAmount;
        PlayerEmberWallet wallet = PlayerEmberWallet.GetOrCreate();

        if (wallet != null && !wallet.TrySpendEmbers(emberCost))
        {
            failureReason = "Not enough embers.";
            return false;
        }

        UnlockSkill(skillId);
        return true;
    }

    public void ApplyUnlockedSkillEffects()
    {
        ResolveSkillComponents();

        dash?.SetMaxCharges(IsSkillUnlocked(SkillIds.DashDoubleDash) ? 2 : 1);

        if (sword != null)
        {
            SwordType swordType = SwordType.Regular;

            if (IsSkillUnlocked(SkillIds.SwordBounce))
                swordType = SwordType.Bounce;
            else if (IsSkillUnlocked(SkillIds.SwordPierce))
                swordType = SwordType.Pierce;
            else if (IsSkillUnlocked(SkillIds.SwordSpin))
                swordType = SwordType.Spin;

            sword.ApplySkillTreeUnlocks(
                IsSkillUnlocked(SkillIds.Sword),
                swordType,
                IsSkillUnlocked(SkillIds.SwordSoulRift),
                IsSkillUnlocked(SkillIds.SwordTimeStop));
        }

        preciseDodge?.ApplySkillTreeUnlocks(
            IsSkillUnlocked(SkillIds.PreciseDodge),
            IsSkillUnlocked(SkillIds.PreciseDodgeFollowUp));

        parry?.ApplySkillTreeUnlocks(
            IsSkillUnlocked(SkillIds.Parry),
            IsSkillUnlocked(SkillIds.ParryRestore),
            IsSkillUnlocked(SkillIds.ParryDamage));

        invisibility?.ApplySkillTreeUnlocks(
            IsSkillUnlocked(SkillIds.Invisibility),
            IsSkillUnlocked(SkillIds.InvisibilityUpgrade));

        awakening?.ApplySkillTreeUnlocks(
            IsSkillUnlocked(SkillIds.Awakening),
            IsSkillUnlocked(SkillIds.AwakeningStatUpgrade),
            IsSkillUnlocked(SkillIds.AwakeningDurationUpgrade));

        sun?.ApplySkillTreeUnlocks(
            IsSkillUnlocked(SkillIds.Sun),
            IsSkillUnlocked(SkillIds.SunDamageUpgrade),
            IsSkillUnlocked(SkillIds.SunDurationUpgrade),
            IsSkillUnlocked(SkillIds.SunMaxTargets),
            IsSkillUnlocked(SkillIds.SunWideDamage));
    }

    public static string NormalizeSkillId(string skillId)
    {
        return string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
    }

    public static string ResolveSkillIdForSlot(UI_SkillTreeSlot slot)
    {
        if (slot == null)
            return string.Empty;

        string overrideId = NormalizeSkillId(slot.SkillIdOverride);

        if (!string.IsNullOrEmpty(overrideId))
            return overrideId;

        return ResolveSkillId(slot.SkillName, slot.SkillDescription, slot.PrerequisiteSlots);
    }

    public static bool IsBaseSkillId(string skillId)
    {
        skillId = NormalizeSkillId(skillId);
        return skillId == SkillIds.DashEmberStep ||
               skillId == SkillIds.PreciseDodge ||
               skillId == SkillIds.Sword ||
               skillId == SkillIds.Parry ||
               skillId == SkillIds.Invisibility ||
               skillId == SkillIds.Awakening ||
               skillId == SkillIds.Sun;
    }

    private void ResolveSkillComponents()
    {
        dash = GetComponent<Dash_Skill>();
        sword = GetComponent<Sword_Skill>();
        preciseDodge = GetComponent<PreciseDodge_Skill>();
        parry = GetComponent<Parry_Skill>();
        sun = GetComponent<Sun_Skill>();
        awakening = GetComponent<Awakening_Skill>();
        invisibility = GetComponent<Invisibility_Skill>();

        if (preciseDodge == null)
            preciseDodge = gameObject.AddComponent<PreciseDodge_Skill>();

        if (parry == null)
            parry = gameObject.AddComponent<Parry_Skill>();

        if (awakening == null)
            awakening = gameObject.AddComponent<Awakening_Skill>();

        if (invisibility == null)
            invisibility = gameObject.AddComponent<Invisibility_Skill>();
    }

    private void LoadUnlockedSkills()
    {
        if (unlocksLoaded)
            return;

        unlocksLoaded = true;
        unlockedSkills.Clear();

        for (int i = 0; i < AllSkillIds.Length; i++)
        {
            string skillId = AllSkillIds[i];

            if (PlayerPrefs.GetInt(GetSaveKey(skillId), 0) == 1)
                unlockedSkills.Add(skillId);
        }
    }

    private void EnsureDefaultUnlocks()
    {
        if (unlockedSkills.Contains(SkillIds.DashEmberStep))
            return;

        unlockedSkills.Add(SkillIds.DashEmberStep);
        PlayerPrefs.SetInt(GetSaveKey(SkillIds.DashEmberStep), 1);
        PlayerPrefs.Save();
    }

    private bool ArePrerequisitesUnlocked(UI_SkillTreeSlot slot, string skillId)
    {
        UI_SkillTreeSlot[] prerequisites = slot.PrerequisiteSlots;

        for (int i = 0; prerequisites != null && i < prerequisites.Length; i++)
        {
            UI_SkillTreeSlot prerequisite = prerequisites[i];

            if (prerequisite != null && !prerequisite.IsDeprecated && !IsSkillUnlocked(prerequisite.SkillId))
                return false;
        }

        if (skillId == SkillIds.SwordSoulRift)
        {
            return IsSkillUnlocked(SkillIds.SwordBounce) ||
                   IsSkillUnlocked(SkillIds.SwordPierce) ||
                   IsSkillUnlocked(SkillIds.SwordSpin);
        }

        return true;
    }

    private bool IsBlockedByMutualExclusion(UI_SkillTreeSlot slot)
    {
        UI_SkillTreeSlot[] lockedSlots = slot.LockedOutSlots;

        for (int i = 0; lockedSlots != null && i < lockedSlots.Length; i++)
        {
            UI_SkillTreeSlot lockedSlot = lockedSlots[i];

            if (lockedSlot != null && !lockedSlot.IsDeprecated && IsSkillUnlocked(lockedSlot.SkillId))
                return true;
        }

        return false;
    }

    private int GetPlayerLevel()
    {
        Player player = PlayerManager.instance != null ? PlayerManager.instance.player : null;
        PlayerStats playerStats = player != null ? player.stats as PlayerStats : null;

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        return playerStats != null ? playerStats.CurrentLevel : 0;
    }

    private bool HasItemNamed(string itemName)
    {
        return Inventory.instance != null && Inventory.instance.HasItemNamed(itemName, true);
    }

    private void RefreshRegisteredSlots()
    {
        for (int i = registeredSlots.Count - 1; i >= 0; i--)
        {
            if (registeredSlots[i] == null)
            {
                registeredSlots.RemoveAt(i);
                continue;
            }

            registeredSlots[i].RefreshUnlockVisual();
        }
    }

    private static string GetSaveKey(string skillId)
    {
        return SavePrefix + NormalizeSkillId(skillId);
    }

    private static string ResolveSkillId(string skillName, string skillDescription, UI_SkillTreeSlot[] prerequisites)
    {
        if (string.IsNullOrWhiteSpace(skillName) && string.IsNullOrWhiteSpace(skillDescription))
            return string.Empty;

        string combinedText = (skillName ?? string.Empty) + "\n" + (skillDescription ?? string.Empty);

        if (Contains(skillName, "\u75BE\u70EC\u5347\u534E"))
            return SkillIds.DashDoubleDash;
        if (Contains(skillName, "\u70EC\u8DC3"))
            return SkillIds.DashDoubleJump;
        if (Contains(skillName, "\u8E0F\u70EC") || Contains(skillName, "\u70EC\u8E0F"))
            return SkillIds.DashEmberStep;

        if (Contains(skillName, "\u5F71\u8FD4"))
            return SkillIds.PreciseDodgeFollowUp;
        if (Contains(skillName, "\u9699\u5F71"))
            return SkillIds.PreciseDodge;

        if (Contains(skillName, "\u6298\u5203\u8A93\u5251"))
            return SkillIds.SwordBounce;
        if (Contains(skillName, "\u6D41\u706B\u5251"))
            return SkillIds.SwordPierce;
        if (Contains(skillName, "\u952F\u85AA\u5251"))
            return SkillIds.SwordSpin;
        if (Contains(skillName, "\u88C2\u9B42"))
            return SkillIds.SwordSoulRift;
        if (Contains(skillName, "\u505C\u6677"))
            return SkillIds.SwordTimeStop;
        if (Contains(skillName, "\u85AA\u5251"))
            return SkillIds.Sword;

        if (Contains(skillName, "\u8FD4\u5203\u56DE\u706B"))
            return SkillIds.ParryRestore;
        if (Contains(skillName, "\u8FD4\u5203\u88C2\u85AA"))
            return SkillIds.ParryDamage;
        if (Contains(skillName, "\u8FD4\u5203"))
            return SkillIds.Parry;

        if (Contains(skillName, "\u65E0\u5F62\u7070\u5F71"))
            return SkillIds.InvisibilityUpgrade;
        if (Contains(skillName, "\u7070\u9690"))
            return SkillIds.Invisibility;

        if (Contains(skillName, "\u5203\u706B\u5347\u534E"))
            return SkillIds.AwakeningStatUpgrade;
        if (Contains(skillName, "\u85AA\u706B\u6C38\u5B58") ||
            Contains(combinedText, "\u89C9\u9192") && Contains(combinedText, "\u6301\u7EED"))
        {
            return SkillIds.AwakeningDurationUpgrade;
        }
        if (Contains(skillName, "\u71C3\u85AA"))
            return SkillIds.Awakening;

        if (Contains(skillName, "\u65E5\u5195\u5347\u534E"))
            return SkillIds.SunDamageUpgrade;
        if (Contains(skillName, "\u707C\u65E5\u51CC\u7A7A"))
            return SkillIds.SunMaxTargets;
        if (Contains(skillName, "\u70C8\u9633\u7B3C\u7F69"))
            return SkillIds.SunWideDamage;
        if (Contains(skillName, "\u4F59\u71C3\u5347\u534E"))
            return IsSunDurationUpgrade(prerequisites) ? SkillIds.SunDurationUpgrade : SkillIds.AwakeningDurationUpgrade;
        if (Contains(combinedText, "\u592A\u9633") && Contains(combinedText, "\u6301\u7EED"))
            return SkillIds.SunDurationUpgrade;
        if (Contains(skillName, "\u65E5\u5195"))
            return SkillIds.Sun;

        return string.Empty;
    }

    private static bool IsSunDurationUpgrade(UI_SkillTreeSlot[] prerequisites)
    {
        for (int i = 0; prerequisites != null && i < prerequisites.Length; i++)
        {
            UI_SkillTreeSlot prerequisite = prerequisites[i];

            if (prerequisite == null)
                continue;

            string prerequisiteId = ResolveSkillIdForSlot(prerequisite);

            if (prerequisiteId == SkillIds.Sun || prerequisiteId == SkillIds.SunDamageUpgrade)
                return true;
        }

        return false;
    }

    private static bool Contains(string value, string expected)
    {
        return !string.IsNullOrEmpty(value) && value.Contains(expected);
    }
}
