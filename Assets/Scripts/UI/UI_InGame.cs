using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_InGame : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text emberText;

    [Header("Skill cooldowns")]
    [SerializeField] private Image dashImage;
    [SerializeField] private Image swordImage;
    [SerializeField] private Image preciseDodgeImage;
    [SerializeField] private Image sunImage;
    [SerializeField] private Image awakeningImage;
    [SerializeField] private Image invisibilityImage;
    [SerializeField] private Image parryImage;
    [SerializeField] private GameObject dashSkillRoot;
    [SerializeField] private GameObject swordSkillRoot;
    [SerializeField] private GameObject preciseDodgeSkillRoot;
    [SerializeField] private GameObject sunSkillRoot;
    [SerializeField] private GameObject awakeningSkillRoot;
    [SerializeField] private GameObject invisibilitySkillRoot;
    [SerializeField] private GameObject parrySkillRoot;

    [Header("Flask UI")]
    [SerializeField] private GameObject dewFlaskRoot;
    [SerializeField] private TMP_Text dewFlaskAmountText;
    [SerializeField] private Image flaskImage;
    [SerializeField] private GameObject[] subFlaskRoots = new GameObject[4];
    [SerializeField] private Image[] subFlaskIconImages = new Image[4];
    [SerializeField] private Image[] subFlaskCooldownImages = new Image[4];
    [SerializeField] private TMP_Text[] subFlaskAmountTexts = new TMP_Text[4];
    [SerializeField] private Vector2 generatedSubFlaskSpacing = new Vector2(72f, 0f);

    [Header("Buff UI")]
    [SerializeField] private GameObject buffRoot;
    [SerializeField] private GameObject awakeningBuffIcon;
    [SerializeField] private GameObject invisibilityBuffIcon;
    [SerializeField] private GameObject normalPortraitRoot;
    [SerializeField] private GameObject awakeningPortraitRoot;

    private SkillManager skills;
    private SkillManager subscribedSkillManager;
    private PlayerEmberWallet emberWallet;
    private PlayerFlaskSystem flaskSystem;

    private void Start()
    {
        ResolvePlayerStats();
        ConfigureReadOnlySlider(slider);

        if (playerStats != null)
            playerStats.onHealthChanged += UpdateHealthUI;

        skills = SkillManager.instance;
        BindSkillManagerEvents();
        BindCooldownImages();
        BindSkillRoots();
        BindEmberText();
        BindEmberWallet();
        BindFlaskUI();
        BindFlaskSystem();
        BindBuffUI();
        BindPortraitUI();
        InitializeCooldownImages();
        UpdateHealthUI();
        RefreshFlaskUI();
        RefreshAwakeningPortrait();
        RefreshSkillUnlockVisibility();
    }

    private void Update()
    {
        if (skills == null)
            skills = SkillManager.instance;

        BindSkillManagerEvents();

        if (skills != null)
        {
            CheckSkillCooldown(dashImage, skills.dash);
            CheckSkillCooldown(swordImage, skills.sword);
            CheckSkillCooldown(preciseDodgeImage, skills.preciseDodge);
            CheckSkillCooldown(sunImage, skills.sun);
            CheckSkillCooldown(awakeningImage, skills.awakening);
            CheckSkillCooldown(invisibilityImage, skills.invisibility);
            CheckSkillCooldown(parryImage, skills.parry);
        }

        if (flaskSystem == null)
            BindFlaskSystem();

        RefreshFlaskCooldownUI();
        RefreshBuffUI();
        RefreshAwakeningPortrait();
        RefreshSkillUnlockVisibility();
        UpdateHealthUI();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.onHealthChanged -= UpdateHealthUI;

        if (emberWallet != null)
            emberWallet.OnEmbersChanged -= HandleEmbersChanged;

        if (flaskSystem != null)
        {
            flaskSystem.OnMainFlaskChanged -= HandleFlasksChanged;
            flaskSystem.OnSubFlasksChanged -= HandleFlasksChanged;
        }

        if (subscribedSkillManager != null)
            subscribedSkillManager.OnSkillUnlocksChanged -= RefreshSkillUnlockVisibility;
    }

    private void UpdateHealthUI()
    {
        if (playerStats == null || slider == null)
            return;

        int maxHealth = Mathf.Max(1, playerStats.GetMaxHealthValue());
        slider.maxValue = maxHealth;
        slider.SetValueWithoutNotify(Mathf.Clamp(playerStats.currentHealth, 0, maxHealth));
    }

    private void ResolvePlayerStats()
    {
        if (playerStats != null)
            return;

        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            playerStats = PlayerManager.instance.player.stats as PlayerStats;

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();
    }

    private void ConfigureReadOnlySlider(Slider targetSlider)
    {
        if (targetSlider == null)
            return;

        targetSlider.interactable = false;
        targetSlider.navigation = new Navigation { mode = Navigation.Mode.None };

        Graphic[] graphics = targetSlider.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }

    private void BindCooldownImages()
    {
        dashImage = FindCooldownImage(dashImage,
            new[] { "DashCooldown", "Dash Cooldown", "Dash_UI", "\u51b2\u523a\u51b7\u5374" },
            new[] { "dash", "\u51b2\u523a" });

        swordImage = FindCooldownImage(swordImage,
            new[] { "SwordCooldown", "Sword Cooldown", "ThrowSwordCooldown", "Sword_UI", "\u63b7\u5251\u51b7\u5374", "\u98de\u5251\u51b7\u5374" },
            new[] { "sword", "throwsword", "\u63b7\u5251", "\u98de\u5251" });

        preciseDodgeImage = FindCooldownImage(preciseDodgeImage,
            new[] { "PreciseDodgeCooldown", "Precise Dodge Cooldown", "Dodge_UI", "\u7cbe\u51c6\u95ea\u907f\u51b7\u5374", "\u7cbe\u95ea\u51b7\u5374" },
            new[] { "precisedodge", "dodge", "\u7cbe\u51c6\u95ea\u907f", "\u7cbe\u95ea" });

        sunImage = FindCooldownImage(sunImage,
            new[] { "SunCooldown", "SunSkillCooldown", "Sun Skill Cooldown", "Sun_UI", "\u65e5\u5195\u51b7\u5374", "\u592a\u9633\u51b7\u5374", "\u707c\u65e5\u51cc\u7a7a\u51b7\u5374" },
            new[] { "sun", "sunskill", "\u65e5\u5195", "\u592a\u9633", "\u707c\u65e5" });

        awakeningImage = FindCooldownImage(awakeningImage,
            new[] { "AwakeningCooldown", "Awakening Cooldown", "AwakenCooldown", "Awaken_UI", "\u89c9\u9192\u51b7\u5374" },
            new[] { "awakening", "awaken", "\u89c9\u9192" });

        invisibilityImage = FindCooldownImage(invisibilityImage,
            new[] { "InvisibilityCooldown", "InvisibleCooldown", "StealthCooldown", "Hide_UI", "\u9690\u8eab\u51b7\u5374" },
            new[] { "invisibility", "invisible", "stealth", "hide", "\u9690\u8eab" });

        parryImage = FindCooldownImage(parryImage,
            new[] { "ParryCooldown", "Parry Cooldown", "Parry_UI", "\u5f39\u5200\u51b7\u5374", "\u683c\u6321\u51b7\u5374", "\u53cd\u51fb\u51b7\u5374" },
            new[] { "parry", "counter", "\u5f39\u5200", "\u683c\u6321", "\u53cd\u51fb" });
    }

    private void BindSkillRoots()
    {
        dashSkillRoot = FindSkillRoot(dashSkillRoot, dashImage, "Dash_UI", "\u51B2\u523A_UI");
        swordSkillRoot = FindSkillRoot(swordSkillRoot, swordImage, "Sword_UI", "ThrowSword_UI", "\u85AA\u5251_UI", "\u98DE\u5251_UI");
        preciseDodgeSkillRoot = FindSkillRoot(preciseDodgeSkillRoot, preciseDodgeImage, "PreciseDodge_UI", "Dodge_UI", "\u9699\u5F71_UI", "\u7CBE\u51C6\u95EA\u907F_UI");
        sunSkillRoot = FindSkillRoot(sunSkillRoot, sunImage, "Sun_UI", "SunSkill_UI", "\u65E5\u5195_UI");
        awakeningSkillRoot = FindSkillRoot(awakeningSkillRoot, awakeningImage, "Awaken_UI", "Awakening_UI", "\u89C9\u9192_UI");
        invisibilitySkillRoot = FindSkillRoot(invisibilitySkillRoot, invisibilityImage, "Hide_UI", "Invisibility_UI", "\u7070\u9690_UI", "\u9690\u8EAB_UI");
        parrySkillRoot = FindSkillRoot(parrySkillRoot, parryImage, "Parry_UI", "\u8FD4\u5203_UI", "\u5F39\u5200_UI");
    }

    private void BindSkillManagerEvents()
    {
        SkillManager currentSkillManager = SkillManager.instance;

        if (subscribedSkillManager == currentSkillManager)
        {
            if (skills == null)
                skills = currentSkillManager;

            return;
        }

        if (subscribedSkillManager != null)
            subscribedSkillManager.OnSkillUnlocksChanged -= RefreshSkillUnlockVisibility;

        subscribedSkillManager = currentSkillManager;
        skills = currentSkillManager;

        if (subscribedSkillManager != null)
            subscribedSkillManager.OnSkillUnlocksChanged += RefreshSkillUnlockVisibility;
    }

    private GameObject FindSkillRoot(GameObject assignedRoot, Image cooldownImage, params string[] rootNames)
    {
        if (assignedRoot != null)
            return assignedRoot;

        for (int i = 0; i < rootNames.Length; i++)
        {
            Transform root = FindTransformByName(transform, rootNames[i]);

            if (root != null)
                return root.gameObject;
        }

        if (cooldownImage == null)
            return null;

        if (IsCooldownImage(cooldownImage) && cooldownImage.transform.parent != null)
            return cooldownImage.transform.parent.gameObject;

        return cooldownImage.gameObject;
    }

    private void RefreshSkillUnlockVisibility()
    {
        if (skills == null)
            skills = SkillManager.instance;

        SetSkillRootVisible(dashSkillRoot, IsSkillUnlocked(SkillManager.SkillIds.DashEmberStep));
        SetSkillRootVisible(swordSkillRoot, IsSkillUnlocked(SkillManager.SkillIds.Sword));
        SetSkillRootVisible(preciseDodgeSkillRoot, IsSkillUnlocked(SkillManager.SkillIds.PreciseDodge));
        SetSkillRootVisible(sunSkillRoot, IsSkillUnlocked(SkillManager.SkillIds.Sun));
        SetSkillRootVisible(awakeningSkillRoot, IsSkillUnlocked(SkillManager.SkillIds.Awakening));
        SetSkillRootVisible(invisibilitySkillRoot, IsSkillUnlocked(SkillManager.SkillIds.Invisibility));
        SetSkillRootVisible(parrySkillRoot, IsSkillUnlocked(SkillManager.SkillIds.Parry));
    }

    private bool IsSkillUnlocked(string skillId)
    {
        return skills != null && skills.IsSkillUnlocked(skillId);
    }

    private void SetSkillRootVisible(GameObject root, bool visible)
    {
        if (root != null && root.activeSelf != visible)
            root.SetActive(visible);
    }

    private void BindEmberText()
    {
        if (emberText != null)
            return;

        Transform emberRoot = FindTransformByName(transform, "yujin_UI");

        if (emberRoot == null)
            emberRoot = FindTransformByName(transform, "\u4f59\u70ec_UI");

        if (emberRoot != null)
            emberText = emberRoot.GetComponentInChildren<TMP_Text>(true);
    }

    private void BindEmberWallet()
    {
        PlayerEmberWallet wallet = PlayerEmberWallet.GetOrCreate();

        if (emberWallet == wallet)
        {
            RefreshEmberText();
            return;
        }

        if (emberWallet != null)
            emberWallet.OnEmbersChanged -= HandleEmbersChanged;

        emberWallet = wallet;

        if (emberWallet != null)
        {
            emberWallet.OnEmbersChanged += HandleEmbersChanged;
            emberWallet.Load();
        }

        RefreshEmberText();
    }

    private void HandleEmbersChanged(int currentEmbers)
    {
        if (emberText == null)
            BindEmberText();

        if (emberText != null)
            emberText.text = currentEmbers.ToString();
    }

    private void RefreshEmberText()
    {
        if (emberText != null)
            emberText.text = emberWallet != null ? emberWallet.CurrentEmbers.ToString() : "0";
    }

    private void BindFlaskUI()
    {
        EnsureFlaskArrays();

        if (dewFlaskRoot == null)
        {
            Transform root = FindTransformByName(transform, "\u6b8b\u606f\u9732\u6ef4_UI");

            if (root == null)
                root = FindTransformByName(transform, "\u6062\u590d\u836f_UI");

            if (root == null)
                root = FindTransformByName(transform, "\u9732\u6ef4_UI");

            if (root == null)
                root = FindTransformByName(transform, "DewFlask_UI");

            if (root == null)
                root = FindTransformByName(transform, "Flask_UI");

            if (root != null)
                dewFlaskRoot = root.gameObject;
        }

        if (dewFlaskRoot != null)
        {
            if (dewFlaskAmountText == null)
                dewFlaskAmountText = FindAmountText(dewFlaskRoot.transform);

            flaskImage = FindOrCreateCooldownImage(dewFlaskRoot.transform, flaskImage);
        }

        BindSubFlaskUI();
    }

    private void BindSubFlaskUI()
    {
        EnsureFlaskArrays();

        for (int i = 0; i < 4; i++)
        {
            if (subFlaskRoots[i] == null)
            {
                Transform matchedRoot = FindSubFlaskRoot(i);

                if (matchedRoot != null)
                    subFlaskRoots[i] = matchedRoot.gameObject;
            }

            if (subFlaskRoots[i] == null && dewFlaskRoot != null)
                subFlaskRoots[i] = CreateRuntimeSubFlaskRoot(i);

            if (subFlaskRoots[i] == null)
                continue;

            if (subFlaskIconImages[i] == null)
                subFlaskIconImages[i] = FindIconImage(subFlaskRoots[i].transform);

            if (subFlaskAmountTexts[i] == null)
                subFlaskAmountTexts[i] = FindAmountText(subFlaskRoots[i].transform);

            subFlaskCooldownImages[i] = FindOrCreateCooldownImage(subFlaskRoots[i].transform, subFlaskCooldownImages[i]);
            subFlaskRoots[i].SetActive(false);
        }
    }

    private void BindFlaskSystem()
    {
        PlayerFlaskSystem system = PlayerFlaskSystem.GetOrCreate();

        if (flaskSystem == system)
        {
            RefreshFlaskUI();
            return;
        }

        if (flaskSystem != null)
        {
            flaskSystem.OnMainFlaskChanged -= HandleFlasksChanged;
            flaskSystem.OnSubFlasksChanged -= HandleFlasksChanged;
        }

        flaskSystem = system;

        if (flaskSystem != null)
        {
            flaskSystem.OnMainFlaskChanged += HandleFlasksChanged;
            flaskSystem.OnSubFlasksChanged += HandleFlasksChanged;
        }

        RefreshFlaskUI();
    }

    private void HandleFlasksChanged()
    {
        RefreshFlaskUI();
    }

    private void RefreshFlaskUI()
    {
        if (flaskSystem == null)
            flaskSystem = PlayerFlaskSystem.GetOrCreate();

        RefreshMainFlaskUI();
        RefreshSubFlaskUI();
        RefreshFlaskCooldownUI();
    }

    private void RefreshMainFlaskUI()
    {
        bool hasMainFlask = flaskSystem != null && flaskSystem.HasMainFlask;

        if (dewFlaskRoot != null)
            dewFlaskRoot.SetActive(hasMainFlask);

        if (dewFlaskRoot == null)
            return;

        if (!hasMainFlask)
        {
            if (dewFlaskAmountText != null)
                dewFlaskAmountText.text = "0";

            return;
        }

        Image icon = FindIconImage(dewFlaskRoot.transform);

        if (icon != null && flaskSystem.MainFlaskItemData != null && flaskSystem.MainFlaskItemData.itemicon != null)
            icon.sprite = flaskSystem.MainFlaskItemData.itemicon;

        if (flaskImage != null && flaskSystem.MainFlaskItemData != null && flaskSystem.MainFlaskItemData.itemicon != null)
            flaskImage.sprite = flaskSystem.MainFlaskItemData.itemicon;

        if (dewFlaskAmountText != null)
            dewFlaskAmountText.text = flaskSystem.MainCurrentCharges.ToString();
    }

    private void RefreshSubFlaskUI()
    {
        EnsureFlaskArrays();

        for (int i = 0; i < 4; i++)
        {
            GameObject root = subFlaskRoots[i];

            if (root == null)
                continue;

            ItemData_Equipment item = flaskSystem != null ? flaskSystem.GetSubFlaskItem(i) : null;
            int count = flaskSystem != null ? flaskSystem.GetSubFlaskCount(i) : 0;
            bool show = item != null && count > 0;

            root.SetActive(show);

            if (!show)
                continue;

            if (subFlaskIconImages[i] != null)
            {
                subFlaskIconImages[i].sprite = item.itemicon;
                subFlaskIconImages[i].color = Color.white;
            }

            if (subFlaskCooldownImages[i] != null)
                subFlaskCooldownImages[i].sprite = item.itemicon;

            if (subFlaskAmountTexts[i] != null)
                subFlaskAmountTexts[i].text = count.ToString();
        }
    }

    private void RefreshFlaskCooldownUI()
    {
        SetCooldownProgress(flaskImage, flaskSystem != null ? flaskSystem.GetMainCooldownProgress() : 0f);

        EnsureFlaskArrays();

        for (int i = 0; i < subFlaskCooldownImages.Length; i++)
        {
            float progress = flaskSystem != null ? flaskSystem.GetSubCooldownProgress(i) : 0f;
            SetCooldownProgress(subFlaskCooldownImages[i], progress);
        }
    }

    private void BindBuffUI()
    {
        if (buffRoot == null)
        {
            Transform root = FindTransformByName(transform, "Buff_UI");

            if (root != null)
                buffRoot = root.gameObject;
        }

        if (buffRoot == null)
            return;

        if (awakeningBuffIcon == null)
            awakeningBuffIcon = FindBuffIcon("Awaken_UI", "Awakening_UI", "\u89c9\u9192_UI", "\u89c9\u9192");

        if (invisibilityBuffIcon == null)
            invisibilityBuffIcon = FindBuffIcon("Hide_UI", "Invisibility_UI", "\u9690\u8eab_UI", "\u9690\u8eab");

        RefreshBuffUI();
    }

    private void BindPortraitUI()
    {
        if (normalPortraitRoot == null)
            normalPortraitRoot = FindPortraitRoot("\u89d2\u8272_UI", "CharacterPortrait_UI", "PlayerPortrait_UI", "Character_UI");

        if (awakeningPortraitRoot == null)
            awakeningPortraitRoot = FindPortraitRoot("\u89d2\u8272\u71c3\u85aa\u72b6\u6001_UI", "AwakeningPortrait_UI", "AwakenedPortrait_UI", "BurningPortrait_UI");
    }

    public void SetAwakeningPortrait(bool isAwakened)
    {
        BindPortraitUI();

        if (normalPortraitRoot != null)
            normalPortraitRoot.SetActive(!isAwakened);

        if (awakeningPortraitRoot != null)
            awakeningPortraitRoot.SetActive(isAwakened);
    }

    private GameObject FindPortraitRoot(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform matchedRoot = FindTransformByName(transform, names[i]);

            if (matchedRoot != null)
                return matchedRoot.gameObject;
        }

        return null;
    }

    private GameObject FindBuffIcon(params string[] names)
    {
        if (buffRoot == null)
            return null;

        for (int i = 0; i < names.Length; i++)
        {
            Transform exact = FindTransformByName(buffRoot.transform, names[i]);

            if (exact != null)
                return exact.gameObject;
        }

        Image[] images = buffRoot.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            string normalized = NormalizeName(images[i].gameObject.name);

            for (int j = 0; j < names.Length; j++)
            {
                if (normalized.Contains(NormalizeName(names[j])))
                    return images[i].gameObject;
            }
        }

        return null;
    }

    private void RefreshBuffUI()
    {
        if (buffRoot == null)
            return;

        bool awakeningActive = skills != null && skills.awakening != null && skills.awakening.IsAwakened;
        bool invisibilityActive = skills != null && skills.invisibility != null && skills.invisibility.IsInvisible;

        if (awakeningBuffIcon != null)
            awakeningBuffIcon.SetActive(awakeningActive);

        if (invisibilityBuffIcon != null)
            invisibilityBuffIcon.SetActive(invisibilityActive);

        buffRoot.SetActive(awakeningActive || invisibilityActive);
    }

    private void RefreshAwakeningPortrait()
    {
        bool awakeningActive = skills != null && skills.awakening != null && skills.awakening.IsAwakened;
        SetAwakeningPortrait(awakeningActive);
    }

    private void InitializeCooldownImages()
    {
        PrepareCooldownImage(dashImage);
        PrepareCooldownImage(swordImage);
        PrepareCooldownImage(preciseDodgeImage);
        PrepareCooldownImage(sunImage);
        PrepareCooldownImage(awakeningImage);
        PrepareCooldownImage(invisibilityImage);
        PrepareCooldownImage(parryImage);
        PrepareCooldownImage(flaskImage);

        for (int i = 0; i < subFlaskCooldownImages.Length; i++)
            PrepareCooldownImage(subFlaskCooldownImages[i]);
    }

    private Image FindCooldownImage(Image assignedImage, string[] exactNames, string[] keywords)
    {
        if (assignedImage != null)
            return assignedImage;

        Image[] childImages = GetComponentsInChildren<Image>(true);

        foreach (string exactName in exactNames)
        {
            Transform matchedRoot = FindTransformByName(transform, exactName);

            if (matchedRoot == null)
                continue;

            Image cooldownImage = FindImageInRoot(matchedRoot, "cooldown", "\u51b7\u5374");

            if (cooldownImage != null)
                return cooldownImage;

            Image rootImage = matchedRoot.GetComponent<Image>();

            if (rootImage != null)
                return rootImage;

            Image anyChildImage = matchedRoot.GetComponentInChildren<Image>(true);

            if (anyChildImage != null)
                return anyChildImage;
        }

        foreach (Image image in childImages)
        {
            if (ImageNameContains(image, keywords) && ImageNameContains(image, "cooldown", "\u51b7\u5374"))
                return image;
        }

        foreach (Image image in childImages)
        {
            if (ImageNameContains(image, keywords))
                return image;
        }

        return null;
    }

    private Transform FindTransformByName(Transform root, string expectedName)
    {
        string normalizedExpectedName = NormalizeName(expectedName);

        if (NormalizeName(root.name) == normalizedExpectedName)
            return root;

        foreach (Transform child in root)
        {
            Transform matchedChild = FindTransformByName(child, expectedName);

            if (matchedChild != null)
                return matchedChild;
        }

        return null;
    }

    private Transform FindSubFlaskRoot(int index)
    {
        string[] names =
        {
            "SubFlask" + (index + 1) + "_UI",
            "SubFlask_" + (index + 1) + "_UI",
            "\u526fFlask" + (index + 1) + "_UI",
            "\u526f\u836f\u74f6" + (index + 1) + "_UI",
            "\u836f\u74f6" + (index + 2) + "_UI"
        };

        for (int i = 0; i < names.Length; i++)
        {
            Transform result = FindTransformByName(transform, names[i]);

            if (result != null)
                return result;
        }

        return null;
    }

    private GameObject CreateRuntimeSubFlaskRoot(int index)
    {
        GameObject root = Instantiate(dewFlaskRoot, dewFlaskRoot.transform.parent);
        root.name = "SubFlask_UI_" + (index + 1);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        RectTransform mainRect = dewFlaskRoot.GetComponent<RectTransform>();

        if (rootRect != null && mainRect != null)
            rootRect.anchoredPosition = mainRect.anchoredPosition + generatedSubFlaskSpacing * (index + 1);

        root.SetActive(false);
        return root;
    }

    private TMP_Text FindAmountText(Transform root)
    {
        Transform amountRoot = FindTransformByName(root, "Amount");

        if (amountRoot == null)
            amountRoot = FindTransformByName(root, "\u6570\u91cf");

        if (amountRoot != null)
            return amountRoot.GetComponentInChildren<TMP_Text>(true);

        return root.GetComponentInChildren<TMP_Text>(true);
    }

    private Image FindIconImage(Transform root)
    {
        Image rootImage = root.GetComponent<Image>();

        if (rootImage != null && !IsCooldownImage(rootImage))
            return rootImage;

        Image[] images = root.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && !IsCooldownImage(images[i]))
                return images[i];
        }

        return rootImage;
    }

    private Image FindOrCreateCooldownImage(Transform root, Image assignedImage)
    {
        if (root == null)
            return assignedImage;

        if (assignedImage != null && assignedImage.transform != root && assignedImage.transform.IsChildOf(root))
            return assignedImage;

        Image cooldownImage = FindImageInRoot(root, "cooldown", "\u51b7\u5374");

        if (cooldownImage != null && cooldownImage.transform != root)
            return cooldownImage;

        Image iconImage = FindIconImage(root);

        if (iconImage == null)
            return assignedImage;

        GameObject overlay = new GameObject("Cooldown");
        overlay.transform.SetParent(root, false);

        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image image = overlay.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = iconImage.sprite;
        image.preserveAspect = iconImage.preserveAspect;
        image.color = new Color(0f, 0f, 0f, 0.6f);
        PrepareCooldownImage(image);
        return image;
    }

    private Image FindImageInRoot(Transform root, params string[] keywords)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (ImageNameContains(image, keywords))
                return image;
        }

        return null;
    }

    private bool ImageNameContains(Image image, params string[] keywords)
    {
        for (Transform current = image.transform; current != null; current = current.parent)
        {
            string normalizedName = NormalizeName(current.name);

            foreach (string keyword in keywords)
            {
                if (normalizedName.Contains(NormalizeName(keyword)))
                    return true;
            }

            if (current == transform)
                break;
        }

        return false;
    }

    private bool IsCooldownImage(Image image)
    {
        return image != null && NormalizeName(image.gameObject.name).Contains("cooldown") ||
               image != null && NormalizeName(image.gameObject.name).Contains("\u51b7\u5374");
    }

    private string NormalizeName(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "")
                .ToLowerInvariant();
    }

    private void PrepareCooldownImage(Image image)
    {
        if (image == null)
            return;

        SyncCooldownSpriteWithIcon(image);
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = false;
        image.fillAmount = 0f;
    }

    private void SyncCooldownSpriteWithIcon(Image cooldownImage)
    {
        Image iconImage = FindIconImageForCooldown(cooldownImage.transform);

        if (iconImage == null || iconImage == cooldownImage || iconImage.sprite == null)
            return;

        cooldownImage.sprite = iconImage.sprite;
        cooldownImage.preserveAspect = iconImage.preserveAspect;
    }

    private Image FindIconImageForCooldown(Transform cooldownTransform)
    {
        for (Transform current = cooldownTransform.parent; current != null; current = current.parent)
        {
            if (current == transform)
                break;

            Image image = current.GetComponent<Image>();

            if (image != null && image.sprite != null && image.transform != cooldownTransform)
                return image;
        }

        return null;
    }

    private void CheckSkillCooldown(Image image, Skill skill)
    {
        if (image == null || skill == null || skill.cooldown <= 0)
        {
            SetCooldownProgress(image, 0f);
            return;
        }

        SetCooldownProgress(image, skill.CooldownDisplayProgress);
    }

    private void SetCooldownProgress(Image image, float progress)
    {
        if (image == null)
            return;

        image.fillAmount = Mathf.Clamp01(progress);
        image.enabled = progress > 0.001f;
    }

    private void EnsureFlaskArrays()
    {
        EnsureArrayLength(ref subFlaskRoots, 4);
        EnsureArrayLength(ref subFlaskIconImages, 4);
        EnsureArrayLength(ref subFlaskCooldownImages, 4);
        EnsureArrayLength(ref subFlaskAmountTexts, 4);
    }

    private void EnsureArrayLength<T>(ref T[] array, int length)
    {
        if (array == null || array.Length != length)
            System.Array.Resize(ref array, length);
    }
}
