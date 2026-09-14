using UnityEngine;

public class UI : MonoBehaviour
{
    [SerializeField] private GameObject characterUI;
    [SerializeField] private GameObject skillTreeUI;
    [SerializeField] private GameObject craftUI;
    [SerializeField] private GameObject optionsUI;
    [SerializeField] private GameObject packageUI;
    [SerializeField] private GameObject levelUpUI;
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private GameObject npcUI;
    [SerializeField] private GameObject deathUI;
    [SerializeField] private GameObject bossUI;
    [SerializeField] private bool pauseGameWhenMenuOpen = true;

    [Header("Audio")]
    [SerializeField] private AudioEventPlayer audioEvents;
    [SerializeField] private string openMenuCueName = "UiOpen";
    [SerializeField] private string closeMenuCueName = "UiClose";
    [SerializeField] private AudioClip openMenuClip;
    [SerializeField] private AudioClip closeMenuClip;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.8f;

    public UI_SkillToolTip skillToolTip;
    public UI_ItemTooltip itemTooltip;
    public UI_StatToolTip statToolTip;
    public UI_InteractionPrompt interactionPrompt;
    public UI_CraftWindow craftWindow;

    private bool uiPauseActive;
    private float timeScaleBeforeUIPause = 1f;
    private UI_DeathScreen deathScreen;
    private Player observedPlayer;
    private bool started;

    public Transform InteractionPromptRoot => inGameUI != null ? inGameUI.transform : transform;

    void Start()
    {
        ResolveMenuReferences();
        ResolveTooltipReferences();
        ResolveDeathScreen();
        SwtichTo(inGameUI);
        deathScreen?.HideImmediate();

        itemTooltip?.gameObject.SetActive(false);
        statToolTip?.gameObject.SetActive(false);
        interactionPrompt = UI_InteractionPrompt.GetOrCreate(InteractionPromptRoot);
        interactionPrompt?.Hide();
        BindPlayerDeathEvent();
        started = true;
    }

    public UI_ItemTooltip GetItemTooltip()
    {
        ResolveTooltipReferences();
        return itemTooltip;
    }

    void Update()
    {
        BindPlayerDeathEvent();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!CloseActiveMenuWithEscape())
                OpenSettings();

            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchWithKeyTo(characterUI);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            SwitchWithKeyTo(skillTreeUI);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            SwitchWithKeyTo(craftUI);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            OpenPackage();
        }

        RefreshGamePauseState();
    }

    public bool CloseActiveMenuWithEscape()
    {
        bool closedAnyMenu = false;

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (child == null || child == inGameUI || child == deathUI || IsPersistentOverlay(child) || !child.activeSelf)
                continue;

            child.SetActive(false);
            closedAnyMenu = true;
        }

        if (closedAnyMenu)
        {
            PlayUiSound(closeMenuCueName, closeMenuClip);
            CheakForInGameUI();
        }

        RefreshGamePauseState();
        return closedAnyMenu;
    }

    public void OpenSettings()
    {
        ResolveMenuReferences();

        if (optionsUI == null)
        {
            Debug.LogWarning("Settings UI was not found under Canvas/UI.", this);
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (child == null || child == inGameUI || child == optionsUI || child == deathUI || IsPersistentOverlay(child))
                continue;

            child.SetActive(false);
        }

        if (inGameUI != null)
            inGameUI.SetActive(true);

        optionsUI.SetActive(true);
        PlayUiSound(openMenuCueName, openMenuClip);

        UI_SettingsPanel settingsPanel = optionsUI.GetComponent<UI_SettingsPanel>();

        if (settingsPanel == null)
            settingsPanel = optionsUI.AddComponent<UI_SettingsPanel>();

        settingsPanel.Open(this);
        RefreshGamePauseState();
    }

    public void SwtichTo(GameObject _menu)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (IsPersistentOverlay(child))
                continue;

            child.SetActive(false);
        }

        if (_menu != null)
        {
            _menu.SetActive(true);
        }

        if (started && _menu != null && _menu != inGameUI && _menu != deathUI && !IsPersistentOverlay(_menu))
            PlayUiSound(openMenuCueName, openMenuClip);

        RefreshGamePauseState();
    }

    public void SwitchWithKeyTo(GameObject _menu)
    {
        if (_menu != null && _menu.activeSelf)
        {
            _menu.SetActive(false);
            PlayUiSound(closeMenuCueName, closeMenuClip);
            CheakForInGameUI();
            RefreshGamePauseState();
            return;
        }
        SwtichTo(_menu);
    }

    public void OpenSkillTree()
    {
        SwtichTo(skillTreeUI);
    }

    public void OpenPackage()
    {
        ResolveMenuReferences();

        if (packageUI == null)
        {
            Debug.LogWarning("Package_UI was not found under Canvas/UI.", this);
            return;
        }

        SwitchWithKeyTo(packageUI);

        if (packageUI.activeInHierarchy)
        {
            UI_PackagePanel packagePanel = packageUI.GetComponent<UI_PackagePanel>();

            if (packagePanel == null)
                packagePanel = packageUI.AddComponent<UI_PackagePanel>();

            packagePanel.Open();
        }
    }

    public void OpenLevelUp()
    {
        ResolveMenuReferences();
        PlayerEmberWallet.GetOrCreate();

        if (levelUpUI == null)
        {
            Debug.LogWarning("LevelUp_UI was not found under Canvas/UI.", this);
            return;
        }

        SwtichTo(levelUpUI);

        UI_LevelUpPanel levelUpPanel = levelUpUI.GetComponent<UI_LevelUpPanel>();

        if (levelUpPanel == null)
            levelUpPanel = levelUpUI.AddComponent<UI_LevelUpPanel>();

        levelUpPanel.Open();
    }

    private void ResolveMenuReferences()
    {
        if (levelUpUI == null)
            levelUpUI = FindChildMenu("LevelUp_UI");

        if (packageUI == null)
            packageUI = FindChildMenu("Package_UI");

        if (packageUI == null && optionsUI != null && optionsUI.name == "Package_UI")
            packageUI = optionsUI;

        if (packageUI == null)
            packageUI = FindChildMenu("Option_UI");

        if (optionsUI == null || optionsUI == packageUI || optionsUI.name == "Package_UI" || optionsUI.name == "Option_UI")
            optionsUI = FindChildMenu("Options", "Options_UI", "Settings_UI", "Setting_UI", "\u8BBE\u7F6E_UI", "\u8BBE\u7F6E");

        if (npcUI == null)
            npcUI = FindChildMenu("NPC_UI");

        if (bossUI == null)
            bossUI = FindChildMenu("Boss_UI");

        if (deathUI == null)
        {
            deathUI = FindChildMenu("\u6B7B\u4EA1_UI");

            if (deathUI == null)
                deathUI = FindChildMenu("Death_UI");
        }

        if (audioEvents == null)
            audioEvents = GetComponent<AudioEventPlayer>();
    }

    private void ResolveTooltipReferences()
    {
        if (itemTooltip == null)
            itemTooltip = GetComponentInChildren<UI_ItemTooltip>(true);

        PrepareItemTooltipForGlobalUse();
    }

    private void PrepareItemTooltipForGlobalUse()
    {
        if (itemTooltip == null)
            return;

        Transform tooltipTransform = itemTooltip.transform;

        if (tooltipTransform.parent != transform)
            tooltipTransform.SetParent(transform, true);

        tooltipTransform.SetAsLastSibling();
    }

    private void ResolveDeathScreen()
    {
        ResolveMenuReferences();

        if (deathUI == null)
            return;

        deathScreen = deathUI.GetComponent<UI_DeathScreen>();

        if (deathScreen == null)
            deathScreen = deathUI.AddComponent<UI_DeathScreen>();

        deathScreen.Initialize();
    }

    private GameObject FindChildMenu(string menuName)
    {
        if (string.IsNullOrWhiteSpace(menuName))
            return null;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (child != null && child.name == menuName)
                return child.gameObject;
        }

        return null;
    }

    private GameObject FindChildMenu(params string[] menuNames)
    {
        if (menuNames == null)
            return null;

        for (int i = 0; i < menuNames.Length; i++)
        {
            GameObject menu = FindChildMenu(menuNames[i]);

            if (menu != null)
                return menu;
        }

        return null;
    }

    private void CheakForInGameUI()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (child != null && child.activeSelf && !IsPersistentOverlay(child))
                return;
        }
        SwtichTo(inGameUI);
    }

    private void RefreshGamePauseState()
    {
        ApplyGamePause(ShouldPauseForActiveMenu());
    }

    private bool ShouldPauseForActiveMenu()
    {
        if (!pauseGameWhenMenuOpen)
            return false;

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (!child.activeInHierarchy || child == inGameUI || child == npcUI || child.name == "NPC_UI" || IsPersistentOverlay(child))
                continue;

            return true;
        }

        return false;
    }

    private bool IsPersistentOverlay(GameObject child)
    {
        if (child == null)
            return false;

        ResolveMenuReferencesIfNeeded();
        return child == bossUI || child.name == "Boss_UI";
    }

    private void ResolveMenuReferencesIfNeeded()
    {
        if (bossUI == null)
            bossUI = FindChildMenu("Boss_UI");
    }

    private void ApplyGamePause(bool shouldPause)
    {
        if (shouldPause == uiPauseActive)
            return;

        if (shouldPause)
        {
            timeScaleBeforeUIPause = Time.timeScale;
            Time.timeScale = 0f;
            uiPauseActive = true;
            return;
        }

        Time.timeScale = timeScaleBeforeUIPause;
        uiPauseActive = false;
    }

    private void PlayUiSound(string cueName, AudioClip clip)
    {
        if (!started)
            return;

        AudioPlaybackUtility.PlayCueOrClip(audioEvents, cueName, clip, transform, audioVolume);
    }

    private void OnDisable()
    {
        UnbindPlayerDeathEvent();
        ApplyGamePause(false);
    }

    private void OnDestroy()
    {
        UnbindPlayerDeathEvent();
        ApplyGamePause(false);
    }

    private void BindPlayerDeathEvent()
    {
        Player currentPlayer = null;

        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            currentPlayer = PlayerManager.instance.player;

        if (currentPlayer == observedPlayer)
            return;

        UnbindPlayerDeathEvent();
        observedPlayer = currentPlayer;

        if (observedPlayer != null)
            observedPlayer.OnDeathAnimationFinished += HandlePlayerDeathAnimationFinished;
    }

    private void UnbindPlayerDeathEvent()
    {
        if (observedPlayer != null)
            observedPlayer.OnDeathAnimationFinished -= HandlePlayerDeathAnimationFinished;

        observedPlayer = null;
    }

    private void HandlePlayerDeathAnimationFinished()
    {
        ResolveDeathScreen();
        deathScreen?.Show();
    }

}
