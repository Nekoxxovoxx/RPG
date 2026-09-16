using System;
using UnityEngine;

public class PlayerStats : CharacterStats
{
    private const string DefaultLevelSaveKey = "PlayerLevel";
    private const string DefaultStrengthSaveKey = "PlayerLevelStrength";
    private const string DefaultAgilitySaveKey = "PlayerLevelAgility";
    private const string DefaultIntelligenceSaveKey = "PlayerLevelIntelligence";
    private const string DefaultVitalitySaveKey = "PlayerLevelVitality";
    private const string DefaultSpentEmbersSaveKey = "PlayerLevelSpentEmbers";

    [Header("Level")]
    [SerializeField, Min(0)] private int currentLevel;
    [SerializeField] private bool saveLevelProgress = true;
    [SerializeField] private string levelSaveKey = DefaultLevelSaveKey;
    [SerializeField] private string strengthSaveKey = DefaultStrengthSaveKey;
    [SerializeField] private string agilitySaveKey = DefaultAgilitySaveKey;
    [SerializeField] private string intelligenceSaveKey = DefaultIntelligenceSaveKey;
    [SerializeField] private string vitalitySaveKey = DefaultVitalitySaveKey;
    [SerializeField] private string spentEmbersSaveKey = DefaultSpentEmbersSaveKey;
    [SerializeField, Min(0)] private int totalSpentEmbersOnUpgrade;

    private Player player;
    private bool levelProgressLoaded;
    private bool initialPrimaryStatsCached;
    private int initialStrength;
    private int initialAgility;
    private int initialIntelligence;
    private int initialVitality;
    private int savedStrengthBonus;
    private int savedAgilityBonus;
    private int savedIntelligenceBonus;
    private int savedVitalityBonus;

    public int CurrentLevel => currentLevel;
    public int TotalSpentEmbersOnUpgrade => totalSpentEmbersOnUpgrade;
    public event Action OnLevelProgressChanged;
    public event Action OnPlayerDeath;

    private void Awake()
    {
        EnsureCoreStatObjects();
        CacheInitialPrimaryStats();
        LoadLevelProgress();
    }

    protected override void Start()
    {
        CacheInitialPrimaryStats();
        LoadLevelProgress();
        base.Start();

        player = GetComponent<Player>();
    }

    public void ApplyPermanentUpgrade(UpgradePreviewData preview)
    {
        ApplyPermanentUpgrade(preview, 0);
    }

    public void ApplyPermanentUpgrade(UpgradePreviewData preview, int spentEmbers)
    {
        if (preview == null || preview.TotalPendingPoints <= 0)
            return;

        CacheInitialPrimaryStats();
        LoadLevelProgress();

        int previousMaxHealth = GetMaxHealthValue();
        int strengthIncrease = preview.GetPending(StatType.力量);
        int agilityIncrease = preview.GetPending(StatType.敏捷);
        int intelligenceIncrease = preview.GetPending(StatType.智慧);
        int vitalityIncrease = preview.GetPending(StatType.活力);

        strength?.AddBaseValue(strengthIncrease);
        agility?.AddBaseValue(agilityIncrease);
        intelligence?.AddBaseValue(intelligenceIncrease);
        vitality?.AddBaseValue(vitalityIncrease);

        savedStrengthBonus += strengthIncrease;
        savedAgilityBonus += agilityIncrease;
        savedIntelligenceBonus += intelligenceIncrease;
        savedVitalityBonus += vitalityIncrease;
        currentLevel += preview.TotalPendingPoints;
        totalSpentEmbersOnUpgrade += Mathf.Max(0, spentEmbers);

        int healthIncrease = Mathf.Max(0, GetMaxHealthValue() - previousMaxHealth);

        if (healthIncrease > 0)
            IncreaseHealthBy(healthIncrease);

        SaveLevelProgress();
        OnLevelProgressChanged?.Invoke();
    }

    public int ReforgeLevelProgress()
    {
        CacheInitialPrimaryStats();
        LoadLevelProgress();

        int refund = Mathf.Max(0, totalSpentEmbersOnUpgrade);

        strength?.SetDefaultValue(initialStrength);
        agility?.SetDefaultValue(initialAgility);
        intelligence?.SetDefaultValue(initialIntelligence);
        vitality?.SetDefaultValue(initialVitality);

        savedStrengthBonus = 0;
        savedAgilityBonus = 0;
        savedIntelligenceBonus = 0;
        savedVitalityBonus = 0;
        currentLevel = 0;
        totalSpentEmbersOnUpgrade = 0;

        currentHealth = Mathf.Clamp(currentHealth, 0, GetMaxHealthValue());
        onHealthChanged?.Invoke();

        SaveLevelProgress();
        OnLevelProgressChanged?.Invoke();

        return refund;
    }

    private void LoadLevelProgress()
    {
        if (levelProgressLoaded)
            return;

        CacheInitialPrimaryStats();
        levelProgressLoaded = true;

        if (!saveLevelProgress)
            return;

        currentLevel = PlayerPrefs.GetInt(GetKey(levelSaveKey, DefaultLevelSaveKey), currentLevel);
        savedStrengthBonus = PlayerPrefs.GetInt(GetKey(strengthSaveKey, DefaultStrengthSaveKey), 0);
        savedAgilityBonus = PlayerPrefs.GetInt(GetKey(agilitySaveKey, DefaultAgilitySaveKey), 0);
        savedIntelligenceBonus = PlayerPrefs.GetInt(GetKey(intelligenceSaveKey, DefaultIntelligenceSaveKey), 0);
        savedVitalityBonus = PlayerPrefs.GetInt(GetKey(vitalitySaveKey, DefaultVitalitySaveKey), 0);
        totalSpentEmbersOnUpgrade = PlayerPrefs.GetInt(GetKey(spentEmbersSaveKey, DefaultSpentEmbersSaveKey), totalSpentEmbersOnUpgrade);

        strength?.AddBaseValue(savedStrengthBonus);
        agility?.AddBaseValue(savedAgilityBonus);
        intelligence?.AddBaseValue(savedIntelligenceBonus);
        vitality?.AddBaseValue(savedVitalityBonus);
    }

    private void SaveLevelProgress()
    {
        if (!saveLevelProgress)
            return;

        PlayerPrefs.SetInt(GetKey(levelSaveKey, DefaultLevelSaveKey), currentLevel);
        PlayerPrefs.SetInt(GetKey(strengthSaveKey, DefaultStrengthSaveKey), savedStrengthBonus);
        PlayerPrefs.SetInt(GetKey(agilitySaveKey, DefaultAgilitySaveKey), savedAgilityBonus);
        PlayerPrefs.SetInt(GetKey(intelligenceSaveKey, DefaultIntelligenceSaveKey), savedIntelligenceBonus);
        PlayerPrefs.SetInt(GetKey(vitalitySaveKey, DefaultVitalitySaveKey), savedVitalityBonus);
        PlayerPrefs.SetInt(GetKey(spentEmbersSaveKey, DefaultSpentEmbersSaveKey), totalSpentEmbersOnUpgrade);
        PlayerPrefs.Save();
    }

    private void CacheInitialPrimaryStats()
    {
        if (initialPrimaryStatsCached)
            return;

        initialPrimaryStatsCached = true;
        initialStrength = strength != null ? strength.BaseValue : 0;
        initialAgility = agility != null ? agility.BaseValue : 0;
        initialIntelligence = intelligence != null ? intelligence.BaseValue : 0;
        initialVitality = vitality != null ? vitality.BaseValue : 0;
    }

    private string GetKey(string customKey, string defaultKey)
    {
        return string.IsNullOrWhiteSpace(customKey) ? defaultKey : customKey;
    }

    public override void TakeDamage(int _damage)
    {
        if (isDead || _damage <= 0)
            return;
        base.TakeDamage(_damage);

        if (player == null)
            player = GetComponent<Player>();

        if (_damage > 0)
            player?.InterruptPreciseDodgeFollowUp();
    }

    protected override void Die()
    {
        if (isDead)
            return;
        MoonShadowLastHomelandRuntimeEffect moonShadowEffect = GetComponent<MoonShadowLastHomelandRuntimeEffect>();

        if (moonShadowEffect != null && moonShadowEffect.TryPreventDeath())
            return;

        base.Die();

        if (player == null)
            player = GetComponent<Player>();

        player?.Die();
        OnPlayerDeath?.Invoke();
    }

    public void TakeBlazingSunDrainDamage(int damage)
    {
        if (damage <= 0 || isDead)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        onHealthChanged?.Invoke();

        if (currentHealth <= 0 && !isDead)
            Die();
    }

    public void ForceMoonShadowDelayedDeath(MoonShadowLastHomelandRuntimeEffect source)
    {
        if (source == null || source != GetComponent<MoonShadowLastHomelandRuntimeEffect>() || isDead)
            return;

        currentHealth = 0;
        base.Die();
        onHealthChanged?.Invoke();

        if (player == null)
            player = GetComponent<Player>();

        player?.Die();
        OnPlayerDeath?.Invoke();
    }

    protected override void DecreaseHealthBy(int _damage)
    {
        if (isDead || _damage <= 0)
            return;
        base.DecreaseHealthBy(_damage);

        if (Inventory.instance == null)
            return;

        ItemData_Equipment currentArmor = Inventory.instance.GetEquipment(EquipmentType.Armor);

        if (player == null)
            player = GetComponent<Player>();

        if (currentArmor != null && player != null)
            currentArmor.Effect(player.transform);
    }

    public override void IncreaseHealthBy(int amount)
    {
        MoonShadowLastHomelandRuntimeEffect effect = GetComponent<MoonShadowLastHomelandRuntimeEffect>();
        if (effect != null && effect.IsDefyingDeath)
            return;
        base.IncreaseHealthBy(amount);
    }

    public override void ReviveWithHealth(int health)
    {
        GetComponent<MoonShadowLastHomelandRuntimeEffect>()?.CancelDeathWindow();
        base.ReviveWithHealth(health);
    }
}
