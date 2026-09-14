using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Awakening_Skill : Skill
{
    [Header("Awakening")]
    [SerializeField, Min(0.1f)] private float duration = 20f;
    [SerializeField, Range(0f, 1f)] private float statBonusPercent = 0.2f;
    [SerializeField] private bool skillUnlocked;
    [SerializeField, Range(0f, 1f)] private float upgradedStatBonusPercent = 0.4f;
    [SerializeField, Min(0f)] private float durationUpgradeSeconds = 5f;

    [Header("Aura")]
    [SerializeField] private AwakeningAuraEffect auraPrefab;
    [SerializeField, Min(1)] private int auraPixelWidth = 1;
    [SerializeField] private Color auraInnerGold = new Color(1f, 0.9f, 0.25f, 1f);
    [SerializeField] private Color auraOuterGold = new Color(1f, 0.55f, 0.06f, 0.9f);

    [Header("Rune Burst")]
    [SerializeField] private Image runeImage;
    [SerializeField] private Sprite runeSprite;
    [SerializeField, Min(0.05f)] private float runeBurstDuration = 0.55f;
    [SerializeField, Min(0.01f)] private float runeStartScale = 0.15f;
    [SerializeField, Min(0.01f)] private float runeEndScale = 1.35f;
    [SerializeField, Range(0f, 1f)] private float runeMaxAlpha = 0.55f;
    [SerializeField] private Color runeColor = new Color(1f, 0.18f, 0.08f, 0.55f);
    [SerializeField, Min(1f)] private float runeScreenSize = 520f;

    [Header("Awakened Finisher")]
    [SerializeField, Min(0f)] private float finisherMotionStartTime = 0.16f;
    [SerializeField, Min(0.05f)] private float finisherMotionDuration = 0.68f;
    [SerializeField] private float finisherForwardDistance = 2.2f;

    private readonly List<AppliedStatModifier> appliedModifiers = new List<AppliedStatModifier>();
    private Coroutine awakeningRoutine;
    private Coroutine runeBurstRoutine;
    private AwakeningAuraEffect activeAura;
    private UI_InGame inGameUI;
    private bool defaultsCached;
    private float baseDuration;
    private float baseStatBonusPercent;

    public bool IsAwakened { get; private set; }
    public float Duration => duration;
    public float StatBonusPercent => statBonusPercent;
    public float FinisherMotionStartTime => finisherMotionStartTime;
    public float FinisherMotionDuration => finisherMotionDuration;
    public float FinisherForwardDistance => finisherForwardDistance;

    private struct AppliedStatModifier
    {
        public Stat stat;
        public int modifier;
    }

    private void Reset()
    {
        cooldown = 45f;
        duration = 20f;
    }

    protected override void Start()
    {
        base.Start();

        if (cooldown <= 0)
            cooldown = 45f;

        if (duration <= 0)
            duration = 20f;

        ResolveRuneSprite();
        ResolveRuneImage();
        HideRuneImage();
    }

    public bool TryActivate()
    {
        EnsurePlayerReference();

        if (!skillUnlocked || !HasLivingPlayer() || IsAwakened)
            return false;

        if (!IsReady())
        {
            Debug.Log("Awakening skill is cooling down.");
            return false;
        }

        StartCooldown();
        awakeningRoutine = StartCoroutine(AwakeningRoutine());
        return true;
    }

    public void CancelAwakening()
    {
        if (awakeningRoutine != null)
            StopCoroutine(awakeningRoutine);

        awakeningRoutine = null;
        EndAwakening();
    }

    public void ApplySkillTreeUnlocks(bool awakeningUnlocked, bool statUpgradeUnlocked, bool durationUpgradeUnlocked)
    {
        CacheBaseValues();
        skillUnlocked = awakeningUnlocked;
        statBonusPercent = statUpgradeUnlocked ? upgradedStatBonusPercent : baseStatBonusPercent;
        duration = baseDuration + (durationUpgradeUnlocked ? durationUpgradeSeconds : 0f);

        if (!skillUnlocked)
            CancelAwakening();
    }

    private IEnumerator AwakeningRoutine()
    {
        BeginAwakening();

        yield return new WaitForSeconds(duration);

        awakeningRoutine = null;
        EndAwakening();
    }

    private void BeginAwakening()
    {
        IsAwakened = true;

        player.primaryAttack?.ResetCombo();

        ApplyStatBonuses();
        SpawnAura();
        PlayRuneBurst();
        SetAwakeningPortrait(true);
    }

    private void CacheBaseValues()
    {
        if (defaultsCached)
            return;

        defaultsCached = true;
        baseDuration = duration;
        baseStatBonusPercent = statBonusPercent;
    }

    private void EndAwakening()
    {
        if (!IsAwakened)
            return;

        IsAwakened = false;
        player.primaryAttack?.ResetCombo();

        RemoveStatBonuses();
        ClearAura();
        SetAwakeningPortrait(false);
    }

    private void ApplyStatBonuses()
    {
        appliedModifiers.Clear();

        if (player == null || player.stats == null)
            return;

        int previousMaxHealth = player.stats.GetMaxHealthValue();

        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
        {
            Stat stat = player.stats.GetStat(statType);

            if (stat == null)
                continue;

            int currentValue = stat.GetValue();

            if (currentValue <= 0)
                continue;

            int modifier = Mathf.Max(1, Mathf.RoundToInt(currentValue * statBonusPercent));
            stat.AddModifier(modifier);
            appliedModifiers.Add(new AppliedStatModifier { stat = stat, modifier = modifier });
        }

        player.stats.IncreaseCurrentHealthByMaxHealthDelta(previousMaxHealth);
    }

    private void RemoveStatBonuses()
    {
        if (player == null || player.stats == null)
            return;

        foreach (AppliedStatModifier appliedModifier in appliedModifiers)
        {
            if (appliedModifier.stat != null)
                appliedModifier.stat.RemoveModifier(appliedModifier.modifier);
        }

        appliedModifiers.Clear();

        player.stats.currentHealth = Mathf.Min(player.stats.currentHealth, player.stats.GetMaxHealthValue());
        player.stats.onHealthChanged?.Invoke();
    }

    private void SpawnAura()
    {
        ClearAura();

        if (player == null)
            return;

        SpriteRenderer playerRenderer = GetPlayerRenderer();

        activeAura = auraPrefab != null
            ? Instantiate(auraPrefab)
            : new GameObject("Awakening Pixel Aura").AddComponent<AwakeningAuraEffect>();

        if (playerRenderer != null)
        {
            activeAura.AttachTo(playerRenderer);
        }
        else
        {
            activeAura.transform.SetParent(player.transform, false);
            activeAura.transform.localPosition = Vector3.zero;
            activeAura.transform.localRotation = Quaternion.identity;
            activeAura.transform.localScale = Vector3.one;
        }

        activeAura.FollowSortingOf(playerRenderer, -1);
        activeAura.Configure(auraPixelWidth, auraInnerGold, auraOuterGold);
    }

    private void ClearAura()
    {
        if (activeAura == null)
            return;

        Destroy(activeAura.gameObject);
        activeAura = null;
    }

    private void PlayRuneBurst()
    {
        ResolveRuneSprite();
        ResolveRuneImage();

        if (runeImage == null || runeSprite == null)
            return;

        if (runeBurstRoutine != null)
            StopCoroutine(runeBurstRoutine);

        runeBurstRoutine = StartCoroutine(RuneBurstRoutine());
    }

    private IEnumerator RuneBurstRoutine()
    {
        RectTransform rectTransform = runeImage.rectTransform;

        runeImage.gameObject.SetActive(true);
        runeImage.transform.SetAsLastSibling();
        runeImage.sprite = runeSprite;
        runeImage.preserveAspect = true;
        runeImage.raycastTarget = false;
        runeImage.color = WithAlpha(runeColor, 0f);

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.one * runeScreenSize;
            rectTransform.localScale = Vector3.one * runeStartScale;
        }

        yield return AnimateRuneBurst(
            scale => rectTransform.localScale = Vector3.one * scale,
            alpha => runeImage.color = WithAlpha(runeColor, alpha));

        runeImage.color = WithAlpha(runeColor, 0f);
        HideRuneImage();
        runeBurstRoutine = null;
    }

    private IEnumerator AnimateRuneBurst(System.Action<float> applyScale, System.Action<float> applyAlpha)
    {
        float elapsed = 0f;
        float durationValue = Mathf.Max(0.05f, runeBurstDuration);

        while (elapsed < durationValue)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / durationValue);
            float easedScale = EaseOutBack(normalizedTime);
            float alpha = Mathf.Sin(normalizedTime * Mathf.PI) * runeMaxAlpha;

            applyScale?.Invoke(Mathf.Lerp(runeStartScale, runeEndScale, easedScale));
            applyAlpha?.Invoke(alpha);

            yield return null;
        }

        applyAlpha?.Invoke(0f);
    }

    private void EnsurePlayerReference()
    {
        if (player != null)
            return;

        if (PlayerManager.instance != null)
            player = PlayerManager.instance.player;

        if (player == null)
            player = FindObjectOfType<Player>();
    }

    private SpriteRenderer GetPlayerRenderer()
    {
        if (player == null)
            return null;

        if (player.sr != null)
            return player.sr;

        return player.GetComponentInChildren<SpriteRenderer>();
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.70158f;
        float t = value - 1f;
        return 1f + (overshoot + 1f) * t * t * t + overshoot * t * t;
    }

    private void ResolveRuneSprite()
    {
        if (runeSprite != null)
            return;

#if UNITY_EDITOR
        runeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphics/VFX/fuwen.png");
#endif
    }

    private void ResolveRuneImage()
    {
        if (runeImage != null)
            return;

        Image[] images = FindObjectsOfType<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (image == null || image.gameObject == null)
                continue;

            string normalizedName = image.gameObject.name.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();

            if (normalizedName.Contains("awakeningrune") || normalizedName.Contains("fuwen") || normalizedName.Contains("\u89c9\u9192\u7b26\u6587"))
            {
                runeImage = image;
                break;
            }
        }

        if (runeImage == null)
            return;

        HideRuneImage();
    }

    private void HideRuneImage()
    {
        if (runeImage == null)
            return;

        runeImage.color = WithAlpha(runeColor, 0f);
        runeImage.gameObject.SetActive(false);
    }

    private void SetAwakeningPortrait(bool isAwakened)
    {
        UI_InGame currentInGameUI = ResolveInGameUI();

        if (currentInGameUI != null)
            currentInGameUI.SetAwakeningPortrait(isAwakened);
    }

    private UI_InGame ResolveInGameUI()
    {
        if (inGameUI != null)
            return inGameUI;

        inGameUI = FindObjectOfType<UI_InGame>(true);
        return inGameUI;
    }

    private void OnValidate()
    {
        runeBurstDuration = Mathf.Max(0.05f, runeBurstDuration);
        runeStartScale = Mathf.Max(0.01f, runeStartScale);
        runeEndScale = Mathf.Max(runeStartScale, runeEndScale);
        runeColor.a = runeMaxAlpha;
        ResolveRuneSprite();
    }
}
