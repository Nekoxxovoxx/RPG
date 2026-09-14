using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossIntroSequenceDirector : MonoBehaviour
{
    private const string DefaultBossUiRootName = "Boss_UI";
    private const string DefaultBossUiGroupName = "\u6df7\u6c8c\u72fc\u4e3b";
    private const string DefaultHealthSliderName = "\u8840\u6761_UI";
    private const string DefaultIntroTextName = "\u6f14\u51fa\u663e\u793a";
    private const string DefaultBossPortalToken = "\u72fc\u4e3b";
    private const string DefaultEntranceToken = "\u5165\u53e3";
    private const string DefaultExitToken = "\u51fa\u53e3";

    [Header("Trigger")]
    [SerializeField] private PortalInteractable entrancePortal;
    [SerializeField] private bool autoResolveEntrancePortal = true;
    [SerializeField] private bool playOnlyOncePerScene = true;
    [SerializeField] private bool lockReturnPortalUntilBossDefeated = true;
    [SerializeField] private string bossPortalNameToken = DefaultBossPortalToken;
    [SerializeField] private string entranceNameToken = DefaultEntranceToken;
    [SerializeField] private string exitNameToken = DefaultExitToken;
    [SerializeField, Min(1f)] private float autoResolveMaxTargetDistanceFromBoss = 38f;

    [Header("Boss")]
    [SerializeField] private MonoBehaviour bossPresentationTarget;
    [SerializeField] private Transform cameraFocusTarget;
    [SerializeField] private CharacterStats bossStats;

    [Header("Boss UI")]
    [SerializeField] private GameObject bossUiRoot;
    [SerializeField] private GameObject bossCombatUiGroup;
    [SerializeField] private string bossUiRootName = DefaultBossUiRootName;
    [SerializeField] private string bossUiGroupName = DefaultBossUiGroupName;
    [SerializeField] private string healthSliderName = DefaultHealthSliderName;
    [SerializeField] private string introTextName = DefaultIntroTextName;
    [SerializeField] private UI_BossHealthBar bossHealthBar;
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private TextMeshProUGUI introText;
    [SerializeField, TextArea] private string introDisplayText;
    [SerializeField, Min(0.1f)] private float typewriterCharactersPerSecond = 4f;

    [Header("Camera")]
    [SerializeField] private BossPhaseTransitionDirector cameraDirector;
    [SerializeField, Min(0.5f)] private float focusOrthographicSize = 4.2f;
    [SerializeField, Min(0f)] private float inputBufferDuration = 1f;
    [SerializeField, Min(0f)] private float cameraFocusMoveDuration = 0.7f;
    [SerializeField, Min(0f)] private float holdAfterTextDuration = 2f;
    [SerializeField, Min(0f)] private float cameraRestoreDuration = 0.55f;

    [Header("Letterbox")]
    [SerializeField] private RectTransform topLetterbox;
    [SerializeField] private RectTransform bottomLetterbox;
    [SerializeField, Min(1f)] private float letterboxHeight = 120f;
    [SerializeField, Min(0f)] private float letterboxFadeDuration = 0.25f;
    [SerializeField] private Color letterboxColor = Color.black;

    [Header("Player")]
    [SerializeField] private bool lockPlayerDuringIntro = true;

    [Header("Intro Audio")]
    [SerializeField] private AudioEventPlayer introAudioEvents;
    [SerializeField] private string introTypeCueName = "BossIntroType";
    [SerializeField] private AudioClip introTypeClip;
    [SerializeField, Min(1)] private int introTypeSoundEveryCharacters = 2;
    [SerializeField, Min(0f)] private float introTypeSoundCooldown = 0.04f;
    [SerializeField, Range(0f, 1f)] private float introTypeSoundVolume = 0.65f;

    private PortalInteractable subscribedPortal;
    private CanvasGroup topLetterboxGroup;
    private CanvasGroup bottomLetterboxGroup;
    private Coroutine introRoutine;
    private Player lockedPlayer;
    private bool hasPlayedIntro;
    private bool referencesResolved;
    private bool combatUiVisible;
    private bool defeatCleanupCompleted;
    private float nextIntroTypeSoundTime;

    private IBossIntroPresentationTarget BossPresentationTarget =>
        bossPresentationTarget as IBossIntroPresentationTarget;

    private IBossCombatActivationTarget BossCombatActivationTarget =>
        bossPresentationTarget as IBossCombatActivationTarget ?? GetComponent<IBossCombatActivationTarget>();

    protected virtual void Awake()
    {
        ResolveReferences();
        PrepareInitialUiState();
    }

    protected virtual void OnEnable()
    {
        ResolveReferences();
        SubscribeToPortal();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeFromPortal();

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        SetLetterboxesActive(false);
        BossPresentationTarget?.SetIntroPresentationLocked(false);
        UnlockPlayer();
    }

    private void LateUpdate()
    {
        if (!combatUiVisible || defeatCleanupCompleted)
            return;

        if (IsPlayerDefeated())
        {
            HandlePlayerDefeated();
            return;
        }

        if (!IsBossUnavailable())
            return;

        HandleBossDefeated();
    }

    public void ConfigureAutoResolveNames(
        string uiGroupName,
        string portalNameToken,
        string uiRootName = DefaultBossUiRootName,
        string healthName = DefaultHealthSliderName,
        string introName = DefaultIntroTextName)
    {
        if (!string.IsNullOrWhiteSpace(uiGroupName))
            bossUiGroupName = uiGroupName;

        if (!string.IsNullOrWhiteSpace(portalNameToken))
            bossPortalNameToken = portalNameToken;

        if (!string.IsNullOrWhiteSpace(uiRootName))
            bossUiRootName = uiRootName;

        if (!string.IsNullOrWhiteSpace(healthName))
            healthSliderName = healthName;

        if (!string.IsNullOrWhiteSpace(introName))
            introTextName = introName;

        referencesResolved = false;
    }

    public virtual void AutoConfigure()
    {
        referencesResolved = false;
        ResolveReferences();
        PrepareInitialUiState();
        SubscribeToPortal();
    }

    public void SetEntrancePortal(PortalInteractable portal)
    {
        entrancePortal = portal;
        ConfigureReturnPortalBossLock();
        SubscribeToPortal();
    }

    public void SetBossPresentationTarget(MonoBehaviour target)
    {
        bossPresentationTarget = target;
        referencesResolved = false;
    }

    public void SetBossStats(CharacterStats stats)
    {
        bossStats = stats;
        ConfigureReturnPortalBossLock();
        referencesResolved = false;
    }

    public void ForceHideCombatUi(bool keepMonitoring = false)
    {
        combatUiVisible = keepMonitoring;
        HideCombatUi();
    }

    public void ForceBossDefeatedCleanup()
    {
        if (defeatCleanupCompleted)
        {
            HideCombatUi();
            return;
        }

        HandleBossDefeated();
    }

    private void ResolveReferences()
    {
        if (referencesResolved)
            return;

        if (bossPresentationTarget == null)
            bossPresentationTarget = GetIntroTargetComponent();

        ResolveBossStats();

        if (introAudioEvents == null)
            introAudioEvents = GetComponent<AudioEventPlayer>();

        if (cameraFocusTarget == null)
            cameraFocusTarget = transform;

        if (cameraDirector == null)
        {
            cameraDirector = GetComponent<BossPhaseTransitionDirector>();

            if (cameraDirector == null)
                cameraDirector = gameObject.AddComponent<BossPhaseTransitionDirector>();
        }

        ResolveBossUiReferences();

        if (autoResolveEntrancePortal && entrancePortal == null)
            entrancePortal = FindBestEntrancePortal();

        ConfigureReturnPortalBossLock();
        referencesResolved = true;
    }

    private void ConfigureReturnPortalBossLock()
    {
        if (!lockReturnPortalUntilBossDefeated || entrancePortal == null)
            return;

        PortalInteractable returnPortal = entrancePortal.TargetPortal;

        if (returnPortal == null || returnPortal == entrancePortal)
            return;

        returnPortal.SetBossDefeatLock(bossStats, true);
    }

    private MonoBehaviour GetIntroTargetComponent()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IBossIntroPresentationTarget)
                return behaviours[i];
        }

        return null;
    }

    private void ResolveBossUiReferences()
    {
        if (bossUiRoot == null)
        {
            GameObject foundRoot = FindSceneObjectByName(bossUiRootName);

            if (foundRoot != null)
                bossUiRoot = foundRoot;
        }

        if (bossUiRoot == null)
            return;

        if (bossCombatUiGroup != null && !BossUiGroupMatchesCurrentBoss(bossCombatUiGroup.transform))
        {
            bossCombatUiGroup = null;
            bossHealthSlider = null;
            bossHealthBar = null;
            introText = null;
        }

        if (bossCombatUiGroup == null)
        {
            Transform group = FindChildRecursive(bossUiRoot.transform, bossUiGroupName);

            if (group == null)
                group = FindChildRecursiveByNormalizedToken(bossUiRoot.transform, bossUiGroupName);

            if (group != null)
                bossCombatUiGroup = group.gameObject;
        }

        Transform searchRoot = bossCombatUiGroup != null ? bossCombatUiGroup.transform : bossUiRoot.transform;

        if (bossHealthSlider != null && !IsAncestorOrSelf(searchRoot, bossHealthSlider.transform))
        {
            bossHealthSlider = null;
            bossHealthBar = null;
        }

        if (bossHealthBar != null && !IsAncestorOrSelf(searchRoot, bossHealthBar.transform))
            bossHealthBar = null;

        if (introText != null && !IsAncestorOrSelf(searchRoot, introText.transform))
            introText = null;

        if (bossHealthSlider == null)
        {
            Transform healthTransform = FindChildRecursive(searchRoot, healthSliderName);

            if (healthTransform != null)
                bossHealthSlider = healthTransform.GetComponent<Slider>();

            if (bossHealthSlider == null)
                bossHealthSlider = searchRoot.GetComponentInChildren<Slider>(true);
        }

        if (bossHealthBar == null && bossHealthSlider != null)
        {
            bossHealthBar = bossHealthSlider.GetComponent<UI_BossHealthBar>();

            if (bossHealthBar == null)
                bossHealthBar = bossHealthSlider.gameObject.AddComponent<UI_BossHealthBar>();
        }

        if (bossHealthBar != null)
        {
            GameObject healthRoot = bossHealthSlider != null ? bossHealthSlider.gameObject : bossHealthBar.gameObject;
            bossHealthBar.Configure(bossHealthSlider, healthRoot);
            bossHealthBar.SetHideWhenTargetDies(false);
            bossHealthBar.Bind(bossStats);
        }

        if (introText == null)
        {
            Transform introTransform = FindChildRecursive(searchRoot, introTextName);

            if (introTransform != null)
                introText = introTransform.GetComponent<TextMeshProUGUI>();

            if (introText == null)
            {
                introTransform = FindChildRecursiveContaining(searchRoot, introTextName);

                if (introTransform != null)
                    introText = introTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (introText == null && bossCombatUiGroup != null)
            introText = CreateFallbackIntroText(bossCombatUiGroup.transform);

        if (introText != null && string.IsNullOrWhiteSpace(introDisplayText))
            introDisplayText = introText.text;
    }

    private TextMeshProUGUI CreateFallbackIntroText(Transform parent)
    {
        if (parent == null)
            return null;

        GameObject textObject = new GameObject(introTextName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(520f, 120f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = bossUiGroupName;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 42f;
        text.raycastTarget = false;
        textObject.SetActive(false);
        return text;
    }

    private void PrepareInitialUiState()
    {
        combatUiVisible = false;

        if (bossHealthBar != null)
            bossHealthBar.Hide();

        if (introText != null)
        {
            introText.text = string.Empty;
            introText.gameObject.SetActive(false);
            EnsureParentsActive(introText.transform, bossCombatUiGroup != null ? bossCombatUiGroup.transform : bossUiRoot != null ? bossUiRoot.transform : null);
        }

        SetLetterboxesActive(false);

        if (bossUiRoot != null)
            bossUiRoot.SetActive(false);
    }

    private void SubscribeToPortal()
    {
        if (subscribedPortal == entrancePortal)
            return;

        UnsubscribeFromPortal();

        if (entrancePortal == null)
            return;

        entrancePortal.OnPlayerTeleported += HandlePlayerTeleported;
        subscribedPortal = entrancePortal;
    }

    private void UnsubscribeFromPortal()
    {
        if (subscribedPortal == null)
            return;

        subscribedPortal.OnPlayerTeleported -= HandlePlayerTeleported;
        subscribedPortal = null;
    }

    private void HandlePlayerTeleported(Player player, PortalInteractable portal, Vector3 destination)
    {
        _ = destination;

        if (introRoutine != null)
            return;

        ResolveReferences();

        if (entrancePortal != null && portal != entrancePortal)
            return;

        if (!IsPortalAllowedForThisBoss(portal))
            return;

        if (IsBossUnavailable())
            return;

        if (playOnlyOncePerScene && hasPlayedIntro)
            return;

        introRoutine = StartCoroutine(IntroRoutine(player));
    }

    private bool IsPortalAllowedForThisBoss(PortalInteractable portal)
    {
        if (portal == null || string.IsNullOrWhiteSpace(bossPortalNameToken))
            return true;

        PortalInteractable[] portals = FindObjectsOfType<PortalInteractable>(true);

        if (!HasAnyBossTokenPortal(portals))
            return entrancePortal != null && portal == entrancePortal;

        return PortalPairMatchesBossToken(portal, portal.TargetPortal);
    }

    private IEnumerator IntroRoutine(Player player)
    {
        hasPlayedIntro = true;
        LockPlayer(player);
        BossPresentationTarget?.SetIntroPresentationLocked(true);
        BossPresentationTarget?.ForceIntroIdle();

        if (inputBufferDuration > 0f)
            yield return new WaitForSecondsRealtime(inputBufferDuration);

        if (IsBossUnavailable())
        {
            FinishIntroCleanup(false);
            yield break;
        }

        PrepareUiForIntroText();
        EnsureLetterboxes();
        SetLetterboxAlpha(0f);
        SetLetterboxesActive(true);

        if (cameraDirector != null)
            cameraDirector.BeginCameraSequence();

        Coroutine letterboxInRoutine = StartCoroutine(FadeLetterboxes(1f, letterboxFadeDuration));

        if (cameraDirector != null)
        {
            yield return cameraDirector.FocusOnPositionCentered(
                GetCameraFocusPosition(),
                focusOrthographicSize,
                cameraFocusMoveDuration);
        }
        else if (cameraFocusMoveDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(cameraFocusMoveDuration);
        }

        if (letterboxInRoutine != null)
            yield return letterboxInRoutine;

        BossPresentationTarget?.ForceIntroIdle();

        if (introText != null)
        {
            introText.gameObject.SetActive(true);
            yield return TypeIntroText();
        }

        if (holdAfterTextDuration > 0f)
            yield return new WaitForSecondsRealtime(holdAfterTextDuration);

        if (introText != null)
            introText.gameObject.SetActive(false);

        Coroutine letterboxOutRoutine = StartCoroutine(FadeLetterboxes(0f, letterboxFadeDuration));

        if (cameraDirector != null)
            yield return cameraDirector.RestoreCamera(cameraRestoreDuration);
        else if (cameraRestoreDuration > 0f)
            yield return new WaitForSecondsRealtime(cameraRestoreDuration);

        if (letterboxOutRoutine != null)
            yield return letterboxOutRoutine;

        SetLetterboxesActive(false);
        FinishIntroCleanup(!IsBossUnavailable());
    }

    private void FinishIntroCleanup(bool showCombatUi)
    {
        if (showCombatUi)
            ShowCombatUi();
        else if (bossUiRoot != null)
        {
            bossUiRoot.SetActive(false);
            combatUiVisible = false;
        }

        BossPresentationTarget?.SetIntroPresentationLocked(false);

        if (showCombatUi)
            BossCombatActivationTarget?.BeginBossCombat(lockedPlayer != null ? lockedPlayer : ResolvePlayer());

        UnlockPlayer();
        introRoutine = null;
    }

    private void PrepareUiForIntroText()
    {
        if (bossUiRoot != null)
        {
            bossUiRoot.SetActive(true);
            bossUiRoot.transform.SetAsLastSibling();
        }

        ActivateOnlyCurrentBossGroup();
        SetCombatVisualsActive(false);

        if (bossHealthBar != null)
            bossHealthBar.Hide();

        if (introText != null)
        {
            introText.text = string.Empty;
            introText.gameObject.SetActive(false);
        }
    }

    private void ShowCombatUi()
    {
        if (IsBossUnavailable())
        {
            HandleBossDefeated();
            return;
        }

        combatUiVisible = true;

        if (bossUiRoot != null)
        {
            bossUiRoot.SetActive(true);
            bossUiRoot.transform.SetAsLastSibling();
        }

        ActivateOnlyCurrentBossGroup();
        SetCombatVisualsActive(true);

        if (introText != null)
            introText.gameObject.SetActive(false);

        if (bossHealthBar != null)
        {
            bossHealthBar.Bind(bossStats);
            bossHealthBar.Show();
        }
    }

    private void HandleBossDefeated()
    {
        defeatCleanupCompleted = true;
        combatUiVisible = false;
        HideCombatUi();
        ReleaseReturnPortalBossLock();
        BGMPlayer.PlayLastRegularBGM();
    }

    private void HandlePlayerDefeated()
    {
        combatUiVisible = false;
        HideCombatUi();
    }

    private void HideCombatUi()
    {
        if (bossHealthBar != null)
            bossHealthBar.Hide();

        if (introText != null)
        {
            introText.text = string.Empty;
            introText.gameObject.SetActive(false);
        }

        if (bossCombatUiGroup != null)
            bossCombatUiGroup.SetActive(false);

        if (bossUiRoot != null)
            bossUiRoot.SetActive(false);
    }

    private void ActivateOnlyCurrentBossGroup()
    {
        if (bossCombatUiGroup == null)
            return;

        if (bossUiRoot != null)
        {
            Transform activeBranch = GetDirectChildUnderRoot(bossCombatUiGroup.transform, bossUiRoot.transform);

            if (activeBranch != null)
            {
                for (int i = 0; i < bossUiRoot.transform.childCount; i++)
                {
                    Transform child = bossUiRoot.transform.GetChild(i);

                    if (child == null)
                        continue;

                    bool isActiveBranch = child == activeBranch;
                    child.gameObject.SetActive(isActiveBranch);

                    if (!isActiveBranch)
                        HideBossHealthBarsInBranch(child);
                }

                EnsureParentsActive(bossCombatUiGroup.transform, bossUiRoot.transform);
                bossCombatUiGroup.SetActive(true);
                return;
            }
        }

        Transform parent = bossCombatUiGroup.transform.parent;

        if (parent == null)
        {
            bossCombatUiGroup.SetActive(true);
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child != null)
                child.gameObject.SetActive(child == bossCombatUiGroup.transform);
        }
    }

    private Transform GetDirectChildUnderRoot(Transform child, Transform root)
    {
        if (child == null || root == null)
            return null;

        Transform current = child;
        Transform directChild = child;

        while (current != null && current != root)
        {
            directChild = current;
            current = current.parent;
        }

        return current == root ? directChild : null;
    }

    private bool BossUiGroupMatchesCurrentBoss(Transform group)
    {
        if (group == null || string.IsNullOrWhiteSpace(bossUiGroupName))
            return true;

        string expectedName = UI_NpcPanelUtility.NormalizeName(bossUiGroupName);

        for (Transform current = group; current != null; current = current.parent)
        {
            if (current == bossUiRoot?.transform)
                break;

            string currentName = UI_NpcPanelUtility.NormalizeName(current.name);

            if (currentName == expectedName || currentName.Contains(expectedName))
            {
                return true;
            }
        }

        return false;
    }

    private void HideBossHealthBarsInBranch(Transform branch)
    {
        if (branch == null)
            return;

        UI_BossHealthBar[] healthBars = branch.GetComponentsInChildren<UI_BossHealthBar>(true);

        for (int i = 0; i < healthBars.Length; i++)
        {
            if (healthBars[i] != null)
                healthBars[i].Hide();
        }

        Slider[] sliders = branch.GetComponentsInChildren<Slider>(true);

        for (int i = 0; i < sliders.Length; i++)
        {
            if (sliders[i] != null)
                sliders[i].gameObject.SetActive(false);
        }
    }

    private void SetCombatVisualsActive(bool active)
    {
        if (bossCombatUiGroup == null)
            return;

        Transform introTransform = introText != null ? introText.transform : null;
        Transform groupTransform = bossCombatUiGroup.transform;

        if (active)
        {
            SetDescendantsActive(groupTransform, true);
            HideIntroText();
        }
        else
        {
            SetOnlyIntroPathActive(groupTransform, introTransform);
        }
    }

    private void HideIntroText()
    {
        if (introText == null)
        {
            HideNamedIntroTextObjects();
            return;
        }

        introText.text = string.Empty;
        introText.gameObject.SetActive(false);
        HideNamedIntroTextObjects();
    }

    private void HideNamedIntroTextObjects()
    {
        Transform searchRoot = bossCombatUiGroup != null
            ? bossCombatUiGroup.transform
            : bossUiRoot != null ? bossUiRoot.transform : null;

        if (searchRoot == null || string.IsNullOrWhiteSpace(introTextName))
            return;

        TMP_Text[] texts = searchRoot.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null || text == introText)
                continue;

            if (!text.gameObject.name.Contains(introTextName))
                continue;

            text.text = string.Empty;
            text.gameObject.SetActive(false);
        }
    }

    private void SetDescendantsActive(Transform root, bool active)
    {
        if (root == null)
            return;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == null)
                continue;

            child.gameObject.SetActive(active);
            SetDescendantsActive(child, active);
        }
    }

    private void SetOnlyIntroPathActive(Transform root, Transform introTransform)
    {
        if (root == null)
            return;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == null)
                continue;

            bool containsIntroText = introTransform != null && IsAncestorOrSelf(child, introTransform);
            child.gameObject.SetActive(containsIntroText);

            if (containsIntroText)
                SetOnlyIntroPathActive(child, introTransform);
        }
    }

    private bool IsAncestorOrSelf(Transform possibleAncestor, Transform child)
    {
        Transform current = child;

        while (current != null)
        {
            if (current == possibleAncestor)
                return true;

            current = current.parent;
        }

        return false;
    }

    private void EnsureParentsActive(Transform child, Transform stopAt)
    {
        Transform current = child != null ? child.parent : null;

        while (current != null)
        {
            current.gameObject.SetActive(true);

            if (current == stopAt)
                break;

            current = current.parent;
        }
    }

    private IEnumerator TypeIntroText()
    {
        if (introText == null)
            yield break;

        string fullText = introDisplayText ?? string.Empty;
        introText.text = string.Empty;

        if (fullText.Length <= 0)
            yield break;

        float secondsPerCharacter = 1f / Mathf.Max(0.1f, typewriterCharactersPerSecond);
        float timer = 0f;
        int visibleCharacters = 0;

        while (visibleCharacters < fullText.Length)
        {
            timer += Time.unscaledDeltaTime;

            while (timer >= secondsPerCharacter && visibleCharacters < fullText.Length)
            {
                timer -= secondsPerCharacter;
                visibleCharacters++;
                introText.text = fullText.Substring(0, visibleCharacters);
                PlayIntroTypeSoundIfNeeded(visibleCharacters);
            }

            yield return null;
        }

        introText.text = fullText;
    }

    private void ResolveBossStats()
    {
        ChaosWolfLordStats wolfStats = GetComponent<ChaosWolfLordStats>();

        if (wolfStats != null)
        {
            wolfStats.EnsureInitializedStats();
            bossStats = wolfStats;
            return;
        }

        if (bossStats == null)
            bossStats = GetComponent<CharacterStats>();
    }

    private void PlayIntroTypeSoundIfNeeded(int visibleCharacters)
    {
        if (visibleCharacters <= 0 || introTypeSoundEveryCharacters <= 0)
            return;

        if (visibleCharacters % introTypeSoundEveryCharacters != 0)
            return;

        if (introTypeSoundCooldown > 0f && Time.unscaledTime < nextIntroTypeSoundTime)
            return;

        nextIntroTypeSoundTime = Time.unscaledTime + introTypeSoundCooldown;
        AudioPlaybackUtility.PlayCueOrClip(
            introAudioEvents,
            introTypeCueName,
            introTypeClip,
            transform,
            introTypeSoundVolume);
    }

    private bool IsBossUnavailable()
    {
        IBossIntroPresentationTarget target = BossPresentationTarget;

        if (target != null)
            return target.IsIntroUnavailable;

        return bossStats != null && bossStats.currentHealth <= 0;
    }

    private bool IsPlayerDefeated()
    {
        Player player = lockedPlayer != null ? lockedPlayer : ResolvePlayer();

        if (player == null)
            return false;

        CharacterStats playerStats = player.stats != null
            ? player.stats
            : player.GetComponent<CharacterStats>();

        return playerStats != null && playerStats.isDead;
    }

    private Vector3 GetCameraFocusPosition()
    {
        if (cameraFocusTarget != null && cameraFocusTarget != transform)
            return cameraFocusTarget.position;

        IBossIntroPresentationTarget target = BossPresentationTarget;

        if (target != null)
            return target.GetIntroFocusPosition();

        Collider2D bodyCollider = GetComponent<Collider2D>();

        if (bodyCollider != null && bodyCollider.enabled)
            return bodyCollider.bounds.center;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null)
            return spriteRenderer.bounds.center;

        return cameraFocusTarget != null ? cameraFocusTarget.position : transform.position;
    }

    private void EnsureLetterboxes()
    {
        if (topLetterbox == null)
            topLetterbox = CreateLetterbox("BossIntro_TopLetterbox", true);

        if (bottomLetterbox == null)
            bottomLetterbox = CreateLetterbox("BossIntro_BottomLetterbox", false);

        ConfigureLetterbox(topLetterbox, true);
        ConfigureLetterbox(bottomLetterbox, false);

        topLetterboxGroup = EnsureCanvasGroup(topLetterbox);
        bottomLetterboxGroup = EnsureCanvasGroup(bottomLetterbox);
    }

    private RectTransform CreateLetterbox(string objectName, bool top)
    {
        Transform parent = ResolveLetterboxParent();
        GameObject letterboxObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        RectTransform rectTransform = letterboxObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        ConfigureLetterbox(rectTransform, top);
        return rectTransform;
    }

    private void ConfigureLetterbox(RectTransform rectTransform, bool top)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = top ? new Vector2(0f, 1f) : Vector2.zero;
        rectTransform.anchorMax = top ? Vector2.one : new Vector2(1f, 0f);
        rectTransform.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(0f, letterboxHeight);
        rectTransform.SetAsLastSibling();

        Image image = rectTransform.GetComponent<Image>();

        if (image != null)
        {
            image.color = letterboxColor;
            image.raycastTarget = false;
        }
    }

    private CanvasGroup EnsureCanvasGroup(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return null;

        CanvasGroup group = rectTransform.GetComponent<CanvasGroup>();

        if (group == null)
            group = rectTransform.gameObject.AddComponent<CanvasGroup>();

        group.blocksRaycasts = false;
        group.interactable = false;
        return group;
    }

    private Transform ResolveLetterboxParent()
    {
        Canvas canvas = FindParentCanvas(bossUiRoot != null ? bossUiRoot.transform : transform);

        if (canvas != null)
            return canvas.transform;

        if (bossUiRoot != null)
            return bossUiRoot.transform;

        return transform;
    }

    private Canvas FindParentCanvas(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            Canvas canvas = current.GetComponent<Canvas>();

            if (canvas != null)
                return canvas;

            current = current.parent;
        }

        return null;
    }

    private IEnumerator FadeLetterboxes(float targetAlpha, float duration)
    {
        EnsureLetterboxes();

        float startTop = topLetterboxGroup != null ? topLetterboxGroup.alpha : targetAlpha;
        float startBottom = bottomLetterboxGroup != null ? bottomLetterboxGroup.alpha : targetAlpha;

        if (duration <= 0f)
        {
            SetLetterboxAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetLetterboxAlpha(
                Mathf.Lerp(startTop, targetAlpha, t),
                Mathf.Lerp(startBottom, targetAlpha, t));
            yield return null;
        }

        SetLetterboxAlpha(targetAlpha);
    }

    private void SetLetterboxAlpha(float alpha)
    {
        SetLetterboxAlpha(alpha, alpha);
    }

    private void SetLetterboxAlpha(float topAlpha, float bottomAlpha)
    {
        if (topLetterboxGroup != null)
            topLetterboxGroup.alpha = topAlpha;

        if (bottomLetterboxGroup != null)
            bottomLetterboxGroup.alpha = bottomAlpha;
    }

    private void SetLetterboxesActive(bool active)
    {
        if (topLetterbox != null)
            topLetterbox.gameObject.SetActive(active);

        if (bottomLetterbox != null)
            bottomLetterbox.gameObject.SetActive(active);
    }

    private void LockPlayer(Player player)
    {
        if (!lockPlayerDuringIntro)
            return;

        lockedPlayer = player != null ? player : ResolvePlayer();
        lockedPlayer?.BeginCutsceneControlLock();
    }

    private void UnlockPlayer()
    {
        if (lockedPlayer == null)
            return;

        lockedPlayer.EndCutsceneControlLock();
        lockedPlayer = null;
    }

    private Player ResolvePlayer()
    {
        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            return PlayerManager.instance.player;

        return FindObjectOfType<Player>();
    }

    private PortalInteractable FindBestEntrancePortal()
    {
        PortalInteractable[] portals = FindObjectsOfType<PortalInteractable>(true);

        if (portals == null || portals.Length == 0)
            return null;

        Vector3 bossPosition = GetCameraFocusPosition();
        PortalInteractable bestPortal = null;
        float bestScore = float.NegativeInfinity;
        bool hasBossTokenPortal = HasAnyBossTokenPortal(portals);

        for (int i = 0; i < portals.Length; i++)
        {
            PortalInteractable portal = portals[i];

            if (portal == null || portal.TargetPortal == null || portal.TargetPortal == portal)
                continue;

            if (hasBossTokenPortal && !PortalPairMatchesBossToken(portal, portal.TargetPortal))
                continue;

            float sourceDistance = Vector2.Distance(portal.transform.position, bossPosition);
            float targetDistance = Vector2.Distance(portal.TargetPortal.transform.position, bossPosition);

            if (targetDistance >= sourceDistance - 0.1f)
                continue;

            if (!hasBossTokenPortal &&
                !string.IsNullOrWhiteSpace(bossPortalNameToken) &&
                targetDistance > autoResolveMaxTargetDistanceFromBoss)
            {
                continue;
            }

            float score = sourceDistance - targetDistance + GetPortalNameScore(portal, portal.TargetPortal);

            if (score > bestScore)
            {
                bestScore = score;
                bestPortal = portal;
            }
        }

        if (bestPortal != null)
            return bestPortal;

        return string.IsNullOrWhiteSpace(bossPortalNameToken) ? portals[0] : null;
    }

    private bool HasAnyBossTokenPortal(PortalInteractable[] portals)
    {
        if (portals == null || portals.Length <= 0 || string.IsNullOrWhiteSpace(bossPortalNameToken))
            return false;

        for (int i = 0; i < portals.Length; i++)
        {
            PortalInteractable portal = portals[i];

            if (portal == null)
                continue;

            if (PortalNameContains(portal, bossPortalNameToken) ||
                PortalNameContains(portal.TargetPortal, bossPortalNameToken))
                return true;
        }

        return false;
    }

    private bool PortalPairMatchesBossToken(PortalInteractable sourcePortal, PortalInteractable targetPortal)
    {
        if (string.IsNullOrWhiteSpace(bossPortalNameToken))
            return true;

        return PortalNameContains(sourcePortal, bossPortalNameToken) ||
               PortalNameContains(targetPortal, bossPortalNameToken);
    }

    private void ReleaseReturnPortalBossLock()
    {
        if (!lockReturnPortalUntilBossDefeated || entrancePortal == null)
            return;

        PortalInteractable returnPortal = entrancePortal.TargetPortal;

        if (returnPortal == null || returnPortal == entrancePortal)
            return;

        returnPortal.ClearBossDefeatLock(true);
    }

    private bool PortalNameContains(PortalInteractable portal, string token)
    {
        return portal != null &&
               !string.IsNullOrWhiteSpace(token) &&
               portal.name.Contains(token);
    }

    private float GetPortalNameScore(PortalInteractable sourcePortal, PortalInteractable targetPortal)
    {
        float score = 0f;
        string sourceName = sourcePortal != null ? sourcePortal.name : string.Empty;
        string targetName = targetPortal != null ? targetPortal.name : string.Empty;

        if (!string.IsNullOrWhiteSpace(bossPortalNameToken))
        {
            if (sourceName.Contains(bossPortalNameToken))
                score += 1000f;

            if (targetName.Contains(bossPortalNameToken))
                score += 250f;
        }

        if (!string.IsNullOrWhiteSpace(entranceNameToken))
        {
            if (sourceName.Contains(entranceNameToken))
                score += 400f;

            if (targetName.Contains(entranceNameToken))
                score -= 150f;
        }

        if (!string.IsNullOrWhiteSpace(exitNameToken))
        {
            if (sourceName.Contains(exitNameToken))
                score -= 800f;

            if (targetName.Contains(exitNameToken))
                score += 150f;
        }

        return score;
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Scene scene = gameObject.scene.IsValid() ? gameObject.scene : SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform found = FindChildRecursive(rootObjects[i].transform, objectName);

            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        if (parent.name == objectName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);

            if (found != null)
                return found;
        }

        return null;
    }

    private Transform FindChildRecursiveContaining(Transform parent, string nameToken)
    {
        if (parent == null || string.IsNullOrWhiteSpace(nameToken))
            return null;

        if (parent.name.Contains(nameToken))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursiveContaining(parent.GetChild(i), nameToken);

            if (found != null)
                return found;
        }

        return null;
    }

    private Transform FindChildRecursiveByNormalizedToken(Transform parent, string nameToken)
    {
        if (parent == null || string.IsNullOrWhiteSpace(nameToken))
            return null;

        string expectedName = UI_NpcPanelUtility.NormalizeName(nameToken);
        string currentName = UI_NpcPanelUtility.NormalizeName(parent.name);

        if (!string.IsNullOrWhiteSpace(expectedName) &&
            (currentName == expectedName || currentName.Contains(expectedName)))
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursiveByNormalizedToken(parent.GetChild(i), nameToken);

            if (found != null)
                return found;
        }

        return null;
    }
}
