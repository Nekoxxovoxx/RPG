using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Enemy_DemonBoss : Enemy, IGenericControlImmuneEnemy, IPreciseDodgeTimeStopTarget, IBossIntroPresentationTarget, IBossCombatActivationTarget
{
    private enum BossPhase
    {
        Slime,
        Demon
    }

    private enum BossMode
    {
        Idle,
        Move,
        Attack,
        Hit,
        Transition,
        Dead
    }

    private enum DemonAttack
    {
        Cleave,
        Smash,
        FireBreath,
        CastSpell
    }

    [Header("Boss Health")]
    [SerializeField, Min(1)] private int phaseOneHealth = 80;
    [SerializeField, Min(1)] private int phaseTwoHealth = 260;

    [Header("Phase One - Slime")]
    [SerializeField, Min(0.1f)] private float slimeDetectionRadius = 7f;
    [SerializeField, Min(0.1f)] private float slimeContactRadius = 0.75f;
    [SerializeField, Min(0)] private int slimeContactDamage = 12;
    [SerializeField, Min(0.05f)] private float slimeContactCooldown = 0.85f;
    [SerializeField, Min(0.1f)] private float slimeMoveSpeed = 2.4f;

    [Header("Phase Transition")]
#pragma warning disable 0414
    // Kept serialized so existing inspector tuning is not discarded if the old prompt beat returns later.
    [SerializeField] private string transitionPrompt = "\u6076\u610f\u4f3c\u4e4e\u6d88\u6563\u4e86......";
    [SerializeField] private Color transitionPromptColor = new Color(1f, 0.82f, 0.42f, 1f);
    [SerializeField, Min(0f)] private float fakeVictoryPause = 2f;
    [SerializeField] private Vector3 transitionPromptOffset = new Vector3(0f, 2.4f, 0f);
#pragma warning restore 0414
    [SerializeField, Min(0.5f)] private float transitionFocusSize = 4.4f;
    [SerializeField, Min(0f)] private float transitionFocusDuration = 0.7f;
    [SerializeField] private Vector3 phaseTwoFocusOffset = new Vector3(0f, 4.2f, 0f);
    [SerializeField, Min(0f)] private float transitionCameraRestoreDuration = 0.55f;
    [SerializeField, Min(0f)] private float postBoomHoldSeconds = 10f;
    [SerializeField] private string phaseTwoIntroState = "d_idle";
    [SerializeField, Min(0f)] private float phaseTwoIntroDuration = 1.2f;
    [SerializeField, Min(0.1f)] private float phaseTwoIntroCharactersPerSecond = 4f;
    [SerializeField] private BGMPlayer phaseTwoBgmPlayer;

    [Header("Phase Two - Demon")]
    [SerializeField, Min(0.1f)] private float demonDetectionRadius = 10f;
    [SerializeField, Min(0.1f)] private float demonPreferredDistance = 2.1f;
    [SerializeField, Min(0.1f)] private float demonMeleeAttackDistance = 2.6f;
    [SerializeField, Min(0.1f)] private float demonFireBreathDistance = 5.2f;
    [SerializeField, Min(0.1f)] private float demonCastSpellDistance = 8f;
    [SerializeField, Min(0.1f)] private float demonMeleeAttackStartDistance = 4.2f;
    [SerializeField, Min(0.1f)] private float demonFireBreathStartDistance = 8f;
    [SerializeField, Min(0.1f)] private float demonCastSpellStartDistance = 10f;
    [SerializeField, Min(0.1f)] private float demonMoveSpeed = 4.2f;
    [SerializeField, Min(0.05f)] private float demonAttackCooldown = 1.25f;
    [SerializeField, Min(0f)] private float attackRecovery = 0.25f;

    [Header("Attack Weights")]
    [SerializeField] private bool enableCastSpellAttack = true;
    [SerializeField, Min(0f)] private float cleaveWeight = 35f;
    [SerializeField, Min(0f)] private float smashWeight = 35f;
    [SerializeField, Min(0f)] private float fireBreathWeight = 20f;
    [SerializeField, Min(0f)] private float castSpellWeight = 10f;
    [SerializeField, Range(0f, 1f)] private float fireBreathSelectionWeightMultiplier = 0.45f;
    [SerializeField, Min(0f)] private float cleaveCooldown = 1.2f;
    [SerializeField, Min(0f)] private float smashCooldown = 1.6f;
    [SerializeField, Min(0f)] private float fireBreathCooldown = 4f;
    [SerializeField, Min(0f)] private float castSpellCooldown = 3f;
    [SerializeField, Min(0)] private int maxConsecutiveSameAttack = 1;

    [Header("Attack Damage")]
    [SerializeField, Min(0)] private int cleaveDamage = 18;
    [SerializeField, Min(0)] private int smashDamage = 22;
    [SerializeField, Min(0)] private int fireBreathDamage = 8;
    [SerializeField, Min(0)] private int castSpellDamage = 16;
    [SerializeField, Min(0.05f)] private float fireBreathTickInterval = 0.18f;

    [Header("Attack Areas")]
    [SerializeField] private BoxCollider2D phaseOneContactHitbox;
    [SerializeField] private BoxCollider2D phaseTwoCleaveHitbox;
    [SerializeField] private BoxCollider2D phaseTwoSmashHitbox;
    [SerializeField] private BoxCollider2D phaseTwoFireBreathHitbox;
    [Header("Fire Breath Sprite Hitbox")]
    [SerializeField] private bool useSpriteFireBreathHitbox = true;
    [SerializeField] private TextAsset fireBreathHitboxData;
    [SerializeField] private Transform cleaveCheck;
    [SerializeField] private Vector2 cleaveBoxSize = new Vector2(2.2f, 1.4f);
    [SerializeField] private Transform fireBreathCheck;
    [SerializeField] private Vector2 fireBreathBoxSize = new Vector2(3.8f, 1.25f);
    [SerializeField] private Transform smashCheck;
    [SerializeField, Min(0.1f)] private float smashRadius = 1.65f;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 8.5f;

    [Header("Hit Reaction / Armor")]
    [SerializeField, Min(0)] private int phaseTwoHitReactionsBeforeAdvancedArmor = 3;
    [SerializeField, Min(0f)] private float hitReactionDuration = 0.22f;
    [SerializeField, Min(0f)] private float advancedSuperArmorDuration = 3f;

    [Header("Animation State Names")]
    [SerializeField] private string slimeIdleState = "idle";
    [SerializeField] private string slimeMoveState = "move";
    [SerializeField] private string slimeHitState = "take_hit";
    [SerializeField] private string slimeBoomState = "death-boom";
    [SerializeField] private string demonTransformState = "death-transform";
    [SerializeField] private string demonIdleState = "d_idle";
    [SerializeField] private string demonWalkState = "d_walk";
    [SerializeField] private string demonHitState = "d_take_hit";
    [SerializeField] private string demonCleaveState = "d_cleave";
    [SerializeField] private string demonSmashState = "d_smash";
    [SerializeField] private string demonFireBreathState = "d_fire_breath";
    [SerializeField] private string demonCastSpellState = "d_cast_spell";
    [SerializeField] private string demonDeathState = "death";

    [Header("Animation Timings")]
    [SerializeField] private bool useAnimationEventCombat = true;
    [SerializeField] private bool fireBreathDamageOnStart = true;
    [SerializeField] private bool requireAttackEndEvent;
    [SerializeField, Range(0f, 1f)] private float cleaveHitNormalizedTime = 0.46f;
    [SerializeField, Range(0f, 1f)] private float smashHitNormalizedTime = 0.58f;
    [SerializeField, Range(0f, 1f)] private float fireBreathStartNormalizedTime = 0.25f;
    [SerializeField, Range(0f, 1f)] private float fireBreathEndNormalizedTime = 0.78f;
    [SerializeField, Range(0f, 1f)] private float castSpellNormalizedTime = 0.48f;
    [SerializeField, Min(0.1f)] private float fallbackAnimationLength = 0.75f;
    [SerializeField, Min(0f)] private float destroyDelayAfterDeath = 2f;

    [Header("Boss UI")]
    [SerializeField] private GameObject bossUiRoot;
    [SerializeField] private GameObject redBossUiGroup;
    [SerializeField] private GameObject phaseOneUiGroup;
    [SerializeField] private GameObject phaseTwoUiGroup;
    [SerializeField] private UI_BossHealthBar phaseOneHealthBar;
    [SerializeField] private UI_BossHealthBar phaseTwoHealthBar;
    [SerializeField] private Slider phaseOneHealthSlider;
    [SerializeField] private Slider phaseTwoHealthSlider;
    [SerializeField] private string bossUiRootName = "Boss_UI";
    [SerializeField] private string redBossUiGroupName = "\u8d64\u9ad3boss";
    [SerializeField] private string phaseOneUiGroupName = "\u8d64\u9ad3\u4e4b\u5375";
    [SerializeField] private string phaseTwoUiGroupName = "\u8d64\u9ad3\u5b88\u536b";
    [SerializeField] private string healthSliderName = "\u8840\u6761_UI";
    [SerializeField] private string phaseIntroTextName = "\u6f14\u51fa\u663e\u793a";

    [Header("Ground Stabilization")]
    [SerializeField] private bool snapVisualBottomToGround = true;
    [SerializeField, Min(0.1f)] private float phaseTransitionGroundSnapDistance = 8f;
    [SerializeField, Min(0f)] private float phaseTransitionGroundSkin = 0.03f;

    [Header("Body Alignment")]
    [SerializeField] private bool defaultVisualFacesLeft = true;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool alignRootToVisualOnAwake;
    [SerializeField] private bool autoFitColliderToVisual = true;
    [SerializeField, Range(0.2f, 1.5f)] private float colliderWidthScale = 0.72f;
    [SerializeField, Range(0.2f, 1.5f)] private float colliderHeightScale = 0.9f;
    [SerializeField] private Vector2 colliderSizePadding = new Vector2(0.1f, 0f);

    private BossPhase currentPhase = BossPhase.Slime;
    private BossMode currentMode = BossMode.Idle;
    private Transform player;
    private DemonBossStats bossStats;
    private BossPhaseTransitionDirector transitionDirector;
    private Coroutine currentActionRoutine;
    private float lastContactDamageTime = -999f;
    private float lastDemonAttackTime = -999f;
    private bool preciseDodgeTimeStopped;
    private bool phaseTransitionStarted;
    private bool advancedSuperArmor;
    private int phaseTwoHitReactionCount;
    private ContactFilter2D playerContactFilter;
    private readonly Collider2D[] overlapHits = new Collider2D[12];
    private SpriteRenderer visualSpriteRenderer;
    private CapsuleCollider2D bossCollider;
    private AudioEventPlayer audioEvents;
    private bool rootAlignedToVisual;
    private bool introPresentationLocked;
    private bool bossCombatStarted;
    private Coroutine advancedArmorRoutine;
    private float savedTransitionGravityScale = 1f;
    private RigidbodyConstraints2D savedTransitionConstraints;
    private RigidbodyType2D savedTransitionBodyType;
    private Vector3 savedTransitionRootPosition;
    private bool transitionPhysicsLocked;
    private DemonAttack currentDemonAttack;
    private bool hasActiveDemonAttack;
    private bool attackEndEventReceived;
    private bool fallbackHitTriggered;
    private bool attackHitEventReceived;
    private bool fireBreathStartEventReceived;
    private bool fireBreathEndEventReceived;
    private bool fireBreathActive;
    private float fireBreathTickTimer;
    private Player phaseTransitionLockedPlayer;
    private DemonAttack queuedDemonAttack;
    private bool hasQueuedDemonAttack;
    private DemonFireBreathHitbox spriteFireBreathHitbox;
    private float lastCleaveTime = -999f;
    private float lastSmashTime = -999f;
    private float lastFireBreathTime = -999f;
    private float lastCastSpellTime = -999f;
    private DemonAttack lastSelectedDemonAttack;
    private int consecutiveSameDemonAttackCount;
    private bool hasLastSelectedDemonAttack;
    private TMP_Text phaseTwoIntroText;
    private string phaseTwoIntroDisplayText;
    private bool deathStartedNotified;

    public bool IsImmuneToGenericEnemyControl => true;
    public bool IsInAdvancedSuperArmor => advancedSuperArmor;
    public bool IsIntroUnavailable => currentMode == BossMode.Dead;
    public bool HasBossCombatStarted => bossCombatStarted;
    public bool IsPhaseTransitionInvulnerable => phaseTransitionStarted || currentMode == BossMode.Transition;
    public float DestroyDelayAfterDeath => Mathf.Max(0f, destroyDelayAfterDeath);
    public event System.Action<Enemy_DemonBoss> OnDeathStarted;
    public static event System.Action<Enemy_DemonBoss> AnyDeathStarted;

#if UNITY_EDITOR
    private bool editorBindQueued;
#endif

    protected override void Awake()
    {
        CacheVisualReferences();
        AlignRootToVisualCenter();
        EnsureRequiredComponents();
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        CacheVisualReferences();
        bossCollider = GetComponent<CapsuleCollider2D>();
        bossStats = GetComponent<DemonBossStats>();
        transitionDirector = GetComponent<BossPhaseTransitionDirector>();
        ResolveAudioEvents();

        if (transitionDirector == null)
            transitionDirector = gameObject.AddComponent<BossPhaseTransitionDirector>();

        if (whatIsPlayer.value == 0)
            whatIsPlayer = LayerMask.GetMask("Player");

        if (whatIsGround.value == 0)
            whatIsGround = LayerMask.GetMask("Ground");

        playerContactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = true,
            layerMask = whatIsPlayer
        };

        SyncInitialFacingWithVisual();
        EnsureCheckPoints();
        EnsureAttackHitboxes();
        RefreshBodyColliderFromVisual();
        spriteFireBreathHitbox = new DemonFireBreathHitbox(fireBreathHitboxData);
        if (useSpriteFireBreathHitbox && fireBreathHitboxData == null)
            Debug.LogWarning("Demon fire breath has no baked sprite hitboxes. Fire damage is disabled until the data is assigned.", this);
        ResolveBossUiReferences();

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.gravityScale = Mathf.Max(0f, rb.gravityScale);
        }

        if (moveSpeed <= 0f)
            moveSpeed = demonMoveSpeed;

        if (defaultMoveSpeed <= 0f)
            defaultMoveSpeed = moveSpeed;

        SyncStatsForCurrentPhase();
        PlayState(slimeIdleState);
        SnapVisualBottomToGround();
        RefreshBodyColliderFromVisual();
        RefreshBossPhaseUi();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        QueueEditorAutoBind();
    }

    private void OnValidate()
    {
        QueueEditorAutoBind();
    }

    [ContextMenu("Auto Bind Demon Boss References")]
    private void AutoBindDemonBossReferences()
    {
        EditorAutoBind();
    }

    private void QueueEditorAutoBind()
    {
        if (Application.isPlaying || editorBindQueued || this == null || !gameObject.scene.IsValid())
            return;

        editorBindQueued = true;
        UnityEditor.EditorApplication.delayCall += EditorAutoBind;
    }

    private void EditorAutoBind()
    {
        editorBindQueued = false;

        if (this == null || Application.isPlaying || !gameObject.scene.IsValid())
            return;

        CacheVisualReferences();
        EnsureAttackHitboxes();
        ResolveBossUiReferences();
        EnsureIntroDirectorForEditor();

        UnityEditor.EditorUtility.SetDirty(this);

        if (phaseOneContactHitbox != null)
            UnityEditor.EditorUtility.SetDirty(phaseOneContactHitbox);

        if (phaseTwoCleaveHitbox != null)
            UnityEditor.EditorUtility.SetDirty(phaseTwoCleaveHitbox);

        if (phaseTwoSmashHitbox != null)
            UnityEditor.EditorUtility.SetDirty(phaseTwoSmashHitbox);

        if (phaseTwoFireBreathHitbox != null)
            UnityEditor.EditorUtility.SetDirty(phaseTwoFireBreathHitbox);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private void EnsureIntroDirectorForEditor()
    {
        BossIntroSequenceDirector introDirector = GetComponent<BossIntroSequenceDirector>();

        if (introDirector == null)
            introDirector = gameObject.AddComponent<DemonBossIntroDirector>();

        introDirector.ConfigureAutoResolveNames(redBossUiGroupName, "\u8d64\u9ad3");
        introDirector.SetBossPresentationTarget(this);
        introDirector.SetBossStats(GetComponent<DemonBossStats>());
        UnityEditor.EditorUtility.SetDirty(introDirector);
    }
#endif

    protected override void Update()
    {
        if (currentMode == BossMode.Dead)
            return;

        ResolvePlayer();

        if (introPresentationLocked || !bossCombatStarted)
        {
            StopBodyMotion();
            PlayStateIfNotCurrent(GetIdleStateForCurrentPhase());
            return;
        }

        if (preciseDodgeTimeStopped)
        {
            StopBodyMotion();
            return;
        }

        if (phaseTransitionStarted || currentMode == BossMode.Transition || currentMode == BossMode.Attack || currentMode == BossMode.Hit)
            return;

        if (IsLockedPlayerUnavailable())
        {
            ClearQueuedDemonAttack();
            StopBodyMotion();
            PlayStateIfNotCurrent(GetIdleStateForCurrentPhase());
            return;
        }

        if (currentPhase == BossPhase.Slime)
            UpdateSlimePhase();
        else
            UpdateDemonPhase();
    }

    private void LateUpdate()
    {
        if (currentMode == BossMode.Dead)
            return;

        SnapVisualBottomToGround();
        // Animator has applied the visible sprite before LateUpdate. Sample the
        // current flame, not the previous frame observed by a coroutine.
        if (currentMode == BossMode.Attack && !preciseDodgeTimeStopped && Time.deltaTime > 0f)
            UpdateFireBreathDamageWindow(Time.deltaTime);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnlockPlayerForPhaseTransition();
    }

    public void SyncStatsForCurrentPhase()
    {
        if (bossStats == null)
            bossStats = GetComponent<DemonBossStats>();

        if (bossStats == null)
            return;

        bossStats.SetBossHealth(currentPhase == BossPhase.Slime ? phaseOneHealth : phaseTwoHealth);
        RefreshBossPhaseUi();
    }

    public bool TryHandlePhaseOneDefeat()
    {
        if (currentPhase != BossPhase.Slime || currentMode == BossMode.Dead)
            return false;

        if (phaseTransitionStarted || currentMode == BossMode.Transition)
        {
            if (bossStats != null)
            {
                bossStats.currentHealth = Mathf.Max(1, bossStats.currentHealth);
                bossStats.onHealthChanged?.Invoke();
            }

            return true;
        }

        phaseTransitionStarted = true;

        if (bossStats != null)
        {
            bossStats.currentHealth = 1;
            bossStats.onHealthChanged?.Invoke();
        }

        if (currentActionRoutine != null)
            StopCoroutine(currentActionRoutine);

        currentActionRoutine = StartCoroutine(PhaseTransitionRoutine());
        return true;
    }

    private void UpdateSlimePhase()
    {
        FacePlayer();

        if (TryDamagePlayerByPhaseOneContact())
        {
            StopBodyMotion();
            return;
        }

        Vector2 direction = GetDirectionToPlayer();
        SetVelocity(direction.x * slimeMoveSpeed, rb != null ? rb.velocity.y : 0f);
        PlayStateIfNotCurrent(slimeMoveState);
    }

    private void UpdateDemonPhase()
    {
        FacePlayer();
        float distance = player != null ? GetHorizontalDistanceToPlayer() : 999f;

        if (distance > demonDetectionRadius && !hasQueuedDemonAttack)
        {
            ClearQueuedDemonAttack();
            SetVelocity(GetHorizontalDirectionToPlayer() * GetDemonMoveSpeed(), rb != null ? rb.velocity.y : 0f);
            PlayStateIfNotCurrent(demonWalkState);
            return;
        }

        if (Time.time >= lastDemonAttackTime + demonAttackCooldown)
        {
            if (!hasQueuedDemonAttack)
            {
                if (!TryChooseAttack(out queuedDemonAttack))
                {
                    MoveTowardPreferredDemonDistance(distance);
                    return;
                }

                hasQueuedDemonAttack = true;
            }

            if (CanStartAttackAtDistance(queuedDemonAttack, distance))
            {
                DemonAttack attack = queuedDemonAttack;
                ClearQueuedDemonAttack();
                StartDemonAttack(attack);
                return;
            }

            MoveTowardQueuedAttackRange(distance);
            return;
        }

        if (distance > demonPreferredDistance)
        {
            SetVelocity(GetHorizontalDirectionToPlayer() * GetDemonMoveSpeed(), rb != null ? rb.velocity.y : 0f);
            PlayStateIfNotCurrent(demonWalkState);
            return;
        }

        StopBodyMotion();
        PlayStateIfNotCurrent(demonIdleState);
    }

    private IEnumerator PhaseTransitionRoutine()
    {
        currentMode = BossMode.Transition;
        StopBodyMotion();
        StopFireBreathDamageWindow();
        ClearQueuedDemonAttack();
        ClearActiveDemonAttack();
        HidePhaseOneBossUiForTransition();
        SnapVisualBottomToGround();
        BeginPhaseTransitionPhysicsLock();

        PlayState(slimeBoomState);
        yield return WaitForAnimation(slimeBoomState);
        PauseAnimatorAtFinalFrame(slimeBoomState);
        StopBgmForPhaseTransition();
        RestoreTransitionRootPosition();

        if (postBoomHoldSeconds > 0f)
            yield return WaitWhileNotTimeStopped(postBoomHoldSeconds);

        if (anim != null)
            anim.speed = 1f;

        RestoreTransitionRootPosition();
        LockPlayerForPhaseTransition();

        Coroutine transitionPromptRoutine = null;

        if (transitionDirector != null)
        {
            transitionDirector.BeginCameraSequence();

            if (!string.IsNullOrWhiteSpace(transitionPrompt))
            {
                transitionPromptRoutine = StartCoroutine(transitionDirector.ShowPromptAtPosition(
                    GetVisualFocusPosition() + transitionPromptOffset,
                    transitionPrompt,
                    transitionPromptColor,
                    transitionFocusDuration));
            }

            yield return transitionDirector.FocusOnPositionCentered(
                GetVisualFocusPosition(),
                transitionFocusSize,
                transitionFocusDuration);

            if (transitionPromptRoutine != null)
            {
                StopCoroutine(transitionPromptRoutine);
                transitionDirector.ClearPromptText();
            }
        }

        PlayAudioCue("transform");
        PlayState(demonTransformState);
        yield return WaitForAnimation(demonTransformState);
        RestoreTransitionRootPosition();

        currentPhase = BossPhase.Demon;
        StopAdvancedSuperArmor();
        SyncStatsForCurrentPhase();
        RefreshBodyColliderFromVisual();
        RestoreTransitionRootPosition();

        yield return PhaseTwoIntroRoutine();
        SnapVisualBottomToGround();
        RefreshBodyColliderFromVisual();
        PlayPhaseTwoBgm();

        RestoreTransitionRootPosition();
        EndPhaseTransitionPhysicsLock();
        PlayStateIfNotCurrent(demonIdleState);
        RefreshBodyColliderFromVisual();

        if (transitionDirector != null)
            yield return transitionDirector.RestoreCamera(transitionCameraRestoreDuration);

        UnlockPlayerForPhaseTransition();
        phaseTransitionStarted = false;
        currentMode = BossMode.Idle;
        RefreshBossPhaseUi();
        currentActionRoutine = null;
    }

    private void StartDemonAttack(DemonAttack attack)
    {
        if (attack == DemonAttack.CastSpell && !CanUseCastSpellAttack())
            return;

        if (currentActionRoutine != null)
            StopCoroutine(currentActionRoutine);

        StopFireBreathDamageWindow();
        ClearActiveDemonAttack();
        RecordDemonAttackUse(attack);
        currentActionRoutine = StartCoroutine(DemonAttackRoutine(attack));
    }

    private IEnumerator DemonAttackRoutine(DemonAttack attack)
    {
        currentMode = BossMode.Attack;
        lastDemonAttackTime = Time.time;
        StopBodyMotion();
        FacePlayer();

        switch (attack)
        {
            case DemonAttack.Cleave:
                yield return useAnimationEventCombat
                    ? EventDrivenAttackRoutine(attack, demonCleaveState, cleaveHitNormalizedTime, AnimationEvent_CleaveHit)
                    : TimedAttack(demonCleaveState, cleaveHitNormalizedTime, DealCleaveHit);
                break;
            case DemonAttack.Smash:
                yield return useAnimationEventCombat
                    ? EventDrivenAttackRoutine(attack, demonSmashState, smashHitNormalizedTime, AnimationEvent_SmashHit)
                    : TimedAttack(demonSmashState, smashHitNormalizedTime, DealSmashHit);
                break;
            case DemonAttack.FireBreath:
                yield return useAnimationEventCombat
                    ? EventDrivenFireBreathAttackRoutine()
                    : FireBreathAttackRoutine();
                break;
            case DemonAttack.CastSpell:
                if (!CanUseCastSpellAttack())
                    break;

                yield return useAnimationEventCombat
                    ? EventDrivenAttackRoutine(attack, demonCastSpellState, castSpellNormalizedTime, AnimationEvent_CastSpell)
                    : TimedAttack(demonCastSpellState, castSpellNormalizedTime, DealCastSpellHit);
                break;
        }

        StopFireBreathDamageWindow();
        ClearActiveDemonAttack();

        if (attackRecovery > 0f)
            yield return WaitWhileNotTimeStopped(attackRecovery);

        currentMode = BossMode.Idle;
        currentActionRoutine = null;
    }

    private IEnumerator TimedAttack(string stateName, float hitNormalizedTime, System.Action hitAction)
    {
        float length = PlayStateAndGetLength(stateName);
        float hitTime = Mathf.Clamp01(hitNormalizedTime) * length;

        yield return WaitWhileNotTimeStopped(hitTime);
        hitAction?.Invoke();
        yield return WaitWhileNotTimeStopped(Mathf.Max(0f, length - hitTime));
    }

    private IEnumerator EventDrivenAttackRoutine(DemonAttack attack, string stateName, float fallbackHitNormalizedTime, System.Action fallbackHitAction)
    {
        float length = PlayStateAndGetLength(stateName);
        float fallbackHitTime = Mathf.Clamp01(fallbackHitNormalizedTime) * length;
        float elapsed = 0f;

        BeginActiveDemonAttack(attack);

        while (ShouldContinueEventAttack(elapsed, length))
        {
            if (!preciseDodgeTimeStopped)
            {
                elapsed += Time.deltaTime;

                if (!fallbackHitTriggered && !attackHitEventReceived && elapsed >= fallbackHitTime)
                {
                    fallbackHitTriggered = true;
                    fallbackHitAction?.Invoke();
                }
            }

            yield return null;
        }
    }

    private IEnumerator FireBreathAttackRoutine()
    {
        float length = PlayStateAndGetLength(demonFireBreathState);
        float startTime = Mathf.Clamp01(fireBreathStartNormalizedTime) * length;
        float endTime = Mathf.Clamp01(fireBreathEndNormalizedTime) * length;

        if (endTime < startTime)
            endTime = startTime;

        BeginActiveDemonAttack(DemonAttack.FireBreath);
        yield return WaitWhileNotTimeStopped(startTime);

        StartFireBreathDamageWindow();
        float elapsed = startTime;

        while (elapsed < endTime)
        {
            if (preciseDodgeTimeStopped)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopFireBreathDamageWindow();
        yield return WaitWhileNotTimeStopped(Mathf.Max(0f, length - endTime));
    }

    private IEnumerator EventDrivenFireBreathAttackRoutine()
    {
        float length = PlayStateAndGetLength(demonFireBreathState);
        float fallbackStartTime = Mathf.Clamp01(fireBreathStartNormalizedTime) * length;
        float fallbackEndTime = Mathf.Clamp01(fireBreathEndNormalizedTime) * length;
        float elapsed = 0f;
        bool hasStartEvent = HasAnimationEvent(demonFireBreathState, "FireBreathStart");
        bool hasEndEvent = HasAnimationEvent(demonFireBreathState, "FireBreathEnd");

        if (fallbackEndTime < fallbackStartTime)
            fallbackEndTime = fallbackStartTime;

        BeginActiveDemonAttack(DemonAttack.FireBreath);

        while (ShouldContinueEventAttack(elapsed, length))
        {
            if (!preciseDodgeTimeStopped)
            {
                elapsed += Time.deltaTime;

                if (!hasStartEvent && !fireBreathStartEventReceived && elapsed >= fallbackStartTime)
                    AnimationEvent_FireBreathStart();

                if (!hasEndEvent && !fireBreathEndEventReceived && elapsed >= fallbackEndTime)
                    AnimationEvent_FireBreathEnd();
            }

            yield return null;
        }

        StopFireBreathDamageWindow();
    }

    private void BeginActiveDemonAttack(DemonAttack attack)
    {
        currentDemonAttack = attack;
        hasActiveDemonAttack = true;
        attackEndEventReceived = false;
        fallbackHitTriggered = false;
        attackHitEventReceived = false;
        fireBreathStartEventReceived = false;
        fireBreathEndEventReceived = false;
        fireBreathActive = false;
        fireBreathTickTimer = 0f;
    }

    private void ClearActiveDemonAttack()
    {
        hasActiveDemonAttack = false;
        attackEndEventReceived = false;
        fallbackHitTriggered = false;
        attackHitEventReceived = false;
        fireBreathStartEventReceived = false;
        fireBreathEndEventReceived = false;
        fireBreathActive = false;
        fireBreathTickTimer = 0f;
    }

    private bool ShouldContinueEventAttack(float elapsed, float animationLength)
    {
        if (currentMode != BossMode.Attack)
            return false;

        if (requireAttackEndEvent)
            return !attackEndEventReceived;

        return !attackEndEventReceived && elapsed < animationLength;
    }

    private bool CanAcceptAttackEvent(DemonAttack attack)
    {
        if (currentMode != BossMode.Attack)
            return false;

        return useAnimationEventCombat && hasActiveDemonAttack && currentDemonAttack == attack;
    }

    public void AnimationEvent_AttackEnd()
    {
        if (!useAnimationEventCombat || currentMode != BossMode.Attack)
            return;

        attackEndEventReceived = true;
        StopFireBreathDamageWindow();
    }

    private void StartFireBreathDamageWindow()
    {
        if (fireBreathActive)
            return;

        fireBreathActive = true;
        fireBreathTickTimer = fireBreathDamageOnStart ? 0f : Mathf.Max(0.01f, fireBreathTickInterval);
        PlayAudioCue("fire_breath");
    }

    private void StopFireBreathDamageWindow()
    {
        if (!fireBreathActive)
            return;

        fireBreathActive = false;
        PlayAudioCue("fire_breath_end");
    }

    private void UpdateFireBreathDamageWindow(float deltaTime)
    {
        if (!fireBreathActive)
            return;

        float safeInterval = Mathf.Max(0.01f, fireBreathTickInterval);
        fireBreathTickTimer -= deltaTime;

        if (fireBreathTickTimer > 0f)
            return;

        if (DealFireBreathDamageTick())
            fireBreathTickTimer = safeInterval;
    }

    private bool DealFireBreathDamageTick()
    {
        if (useSpriteFireBreathHitbox)
        {
            if (spriteFireBreathHitbox == null || visualSpriteRenderer == null)
                return false;

            Physics2D.SyncTransforms();
            return spriteFireBreathHitbox.TryFindPlayer(visualSpriteRenderer, playerContactFilter, overlapHits, out Player hitPlayer)
                && TryDamagePlayer(hitPlayer, fireBreathDamage);
        }

        DealBoxDamage(phaseTwoFireBreathHitbox, fireBreathCheck, fireBreathBoxSize, fireBreathDamage);
        return true;
    }

    public void AnimationEvent_CleaveHit()
    {
        if (!CanAcceptAttackEvent(DemonAttack.Cleave))
            return;

        if (fallbackHitTriggered && attackHitEventReceived)
            return;

        attackHitEventReceived = true;
        DealCleaveHit();
    }

    private void DealCleaveHit()
    {
        PlayAudioCue("cleave");
        DealBoxDamage(phaseTwoCleaveHitbox, cleaveCheck, cleaveBoxSize, cleaveDamage);
    }

    public void AnimationEvent_SmashHit()
    {
        if (!CanAcceptAttackEvent(DemonAttack.Smash))
            return;

        if (fallbackHitTriggered && attackHitEventReceived)
            return;

        attackHitEventReceived = true;
        DealSmashHit();
    }

    private void DealSmashHit()
    {
        PlayAudioCue("smash");
        if (phaseTwoSmashHitbox != null)
            DealBoxDamage(phaseTwoSmashHitbox, smashCheck, new Vector2(smashRadius * 2f, smashRadius * 1.4f), smashDamage);
        else
            DealCircleDamage(smashCheck, smashRadius, smashDamage);
    }

    public void AnimationEvent_FireBreathStart()
    {
        if (!CanAcceptAttackEvent(DemonAttack.FireBreath))
            return;

        fireBreathStartEventReceived = true;
        StartFireBreathDamageWindow();
    }

    public void AnimationEvent_FireBreathEnd()
    {
        if (!CanAcceptAttackEvent(DemonAttack.FireBreath))
            return;

        fireBreathEndEventReceived = true;
        StopFireBreathDamageWindow();
    }

    public void AnimationEvent_CastSpell()
    {
        if (!CanUseCastSpellAttack())
            return;

        if (!CanAcceptAttackEvent(DemonAttack.CastSpell))
            return;

        if (fallbackHitTriggered && attackHitEventReceived)
            return;

        attackHitEventReceived = true;
        DealCastSpellHit();
    }

    private void DealCastSpellHit()
    {
        if (!CanUseCastSpellAttack())
            return;

        PlayAudioCue("cast_spell");
        SpawnProjectile();
    }

    public void AnimationEvent_TransformFinished()
    {
        if (currentMode == BossMode.Transition)
            currentPhase = BossPhase.Demon;
    }

    public void BeginBossCombat(Player targetPlayer)
    {
        if (currentMode == BossMode.Dead)
            return;

        bossCombatStarted = true;
        ClearQueuedDemonAttack();

        if (targetPlayer != null)
            player = targetPlayer.transform;
        else
            ResolvePlayer();

        lastDemonAttackTime = Time.time;
        RefreshBossPhaseUi();
    }

    public Vector3 GetIntroFocusPosition()
    {
        return GetVisualFocusPosition();
    }

    public void SetIntroPresentationLocked(bool locked)
    {
        introPresentationLocked = locked;

        if (currentMode == BossMode.Dead)
            return;

        if (currentActionRoutine != null)
        {
            StopCoroutine(currentActionRoutine);
            currentActionRoutine = null;
        }

        StopFireBreathDamageWindow();
        ClearQueuedDemonAttack();
        ClearActiveDemonAttack();
        StopBodyMotion();
        currentMode = BossMode.Idle;

        if (anim != null)
            anim.speed = locked || !preciseDodgeTimeStopped ? 1f : 0f;

        PlayState(GetIdleStateForCurrentPhase());
    }

    public void ForceIntroIdle()
    {
        if (currentMode == BossMode.Dead)
            return;

        StopBodyMotion();
        PlayStateIfNotCurrent(GetIdleStateForCurrentPhase());
    }

    private bool TryChooseAttack(out DemonAttack attack)
    {
        float effectiveCastSpellWeight = GetEffectiveCastSpellWeight();
        float effectiveFireBreathWeight = GetEffectiveFireBreathWeight();

        return TryChooseWeightedAttack(
            cleaveWeight,
            smashWeight,
            effectiveFireBreathWeight,
            effectiveCastSpellWeight,
            out attack);
    }

    private void MoveTowardQueuedAttackRange(float distance)
    {
        float requiredDistance = GetRequiredDistanceForAttack(queuedDemonAttack);

        if (distance > requiredDistance)
        {
            SetVelocity(GetHorizontalDirectionToPlayer() * GetDemonMoveSpeed(), rb != null ? rb.velocity.y : 0f);
            PlayStateIfNotCurrent(demonWalkState);
            return;
        }

        StopBodyMotion();
        PlayStateIfNotCurrent(demonIdleState);
    }

    private void MoveTowardPreferredDemonDistance(float distance)
    {
        if (distance > demonPreferredDistance)
        {
            SetVelocity(GetHorizontalDirectionToPlayer() * GetDemonMoveSpeed(), rb != null ? rb.velocity.y : 0f);
            PlayStateIfNotCurrent(demonWalkState);
            return;
        }

        StopBodyMotion();
        PlayStateIfNotCurrent(demonIdleState);
    }

    private float GetRequiredDistanceForAttack(DemonAttack attack)
    {
        return attack switch
        {
            DemonAttack.Cleave => GetMeleeStartDistance(),
            DemonAttack.Smash => GetMeleeStartDistance(),
            DemonAttack.FireBreath => GetFireBreathStartDistance(),
            DemonAttack.CastSpell => GetCastSpellStartDistance(),
            _ => demonPreferredDistance
        };
    }

    private void ClearQueuedDemonAttack()
    {
        hasQueuedDemonAttack = false;
        queuedDemonAttack = DemonAttack.Cleave;
    }

    private bool TryChooseWeightedAttack(float cleave, float smash, float fireBreath, float castSpell, out DemonAttack attack)
    {
        castSpell = enableCastSpellAttack ? castSpell : 0f;
        int enabledAttackCount = (cleave > 0f ? 1 : 0) + (smash > 0f ? 1 : 0) +
            (fireBreath > 0f ? 1 : 0) + (castSpell > 0f ? 1 : 0);
        // A single configured attack cannot alternate, but must still obey its cooldown.
        bool allowRepeat = enabledAttackCount == 1;
        float cleaveSelectable = GetSelectableAttackWeight(DemonAttack.Cleave, cleave, allowRepeat);
        float smashSelectable = GetSelectableAttackWeight(DemonAttack.Smash, smash, allowRepeat);
        float fireSelectable = GetSelectableAttackWeight(DemonAttack.FireBreath, fireBreath, allowRepeat);
        float castSelectable = GetSelectableAttackWeight(DemonAttack.CastSpell, castSpell, allowRepeat);
        float totalWeight = cleaveSelectable + smashSelectable + fireSelectable + castSelectable;

        if (totalWeight <= 0f)
        {
            attack = DemonAttack.Cleave;
            return false;
        }

        float roll = Random.Range(0f, totalWeight);

        if (cleaveSelectable > 0f && (roll < cleaveSelectable ||
            (smashSelectable <= 0f && fireSelectable <= 0f && castSelectable <= 0f)))
        {
            attack = DemonAttack.Cleave;
            return true;
        }

        roll -= cleaveSelectable;
        if (smashSelectable > 0f && (roll < smashSelectable ||
            (fireSelectable <= 0f && castSelectable <= 0f)))
        {
            attack = DemonAttack.Smash;
            return true;
        }

        roll -= smashSelectable;
        if (fireSelectable > 0f && (roll < fireSelectable || castSelectable <= 0f))
        {
            attack = DemonAttack.FireBreath;
            return true;
        }

        attack = DemonAttack.CastSpell;
        return true;
    }

    private bool CanStartAttackAtDistance(DemonAttack attack, float distance)
    {
        return attack switch
        {
            DemonAttack.Cleave => distance <= GetMeleeStartDistance(),
            DemonAttack.Smash => distance <= GetMeleeStartDistance(),
            DemonAttack.FireBreath => distance <= GetFireBreathStartDistance(),
            DemonAttack.CastSpell => CanUseCastSpellAttack() && distance <= GetCastSpellStartDistance(),
            _ => false
        };
    }

    private float GetSelectableAttackWeight(DemonAttack attack, float baseWeight, bool allowRepeat)
    {
        return CanSelectAttack(attack, allowRepeat) ? Mathf.Max(0f, baseWeight) : 0f;
    }

    private bool CanSelectAttack(DemonAttack attack, bool allowRepeat)
    {
        if (attack == DemonAttack.CastSpell && !CanUseCastSpellAttack())
            return false;

        return IsAttackCooldownReady(attack) && (allowRepeat || CanRepeatAttack(attack));
    }

    private bool IsAttackCooldownReady(DemonAttack attack)
    {
        float now = Time.time;

        return attack switch
        {
            DemonAttack.Cleave => now >= lastCleaveTime + cleaveCooldown,
            DemonAttack.Smash => now >= lastSmashTime + smashCooldown,
            DemonAttack.FireBreath => now >= lastFireBreathTime + fireBreathCooldown,
            DemonAttack.CastSpell => now >= lastCastSpellTime + castSpellCooldown,
            _ => true
        };
    }

    private bool CanRepeatAttack(DemonAttack attack)
    {
        return maxConsecutiveSameAttack <= 0 ||
               !hasLastSelectedDemonAttack ||
               attack != lastSelectedDemonAttack ||
               consecutiveSameDemonAttackCount < maxConsecutiveSameAttack;
    }

    private void RecordDemonAttackUse(DemonAttack attack)
    {
        float now = Time.time;

        switch (attack)
        {
            case DemonAttack.Cleave:
                lastCleaveTime = now;
                break;
            case DemonAttack.Smash:
                lastSmashTime = now;
                break;
            case DemonAttack.FireBreath:
                lastFireBreathTime = now;
                break;
            case DemonAttack.CastSpell:
                lastCastSpellTime = now;
                break;
        }

        if (hasLastSelectedDemonAttack && attack == lastSelectedDemonAttack)
            consecutiveSameDemonAttackCount++;
        else
            consecutiveSameDemonAttackCount = 1;

        lastSelectedDemonAttack = attack;
        hasLastSelectedDemonAttack = true;
    }

    private float GetMeleeStartDistance()
    {
        return Mathf.Max(0.1f, Mathf.Max(demonMeleeAttackDistance, demonMeleeAttackStartDistance));
    }

    private float GetFireBreathStartDistance()
    {
        return Mathf.Max(0.1f, Mathf.Max(demonFireBreathDistance, demonFireBreathStartDistance));
    }

    private float GetCastSpellStartDistance()
    {
        return Mathf.Max(0.1f, Mathf.Max(demonCastSpellDistance, demonCastSpellStartDistance));
    }

    private bool TryDamagePlayerByCircle(float radius, int damage, bool useCooldown)
    {
        if (useCooldown && Time.time < lastContactDamageTime + slimeContactCooldown)
            return false;

        int hitCount = Physics2D.OverlapCircle(GetBossCenterPosition(), Mathf.Max(0.1f, radius), playerContactFilter, overlapHits);

        for (int i = 0; i < hitCount; i++)
        {
            Player hitPlayer = overlapHits[i] != null ? overlapHits[i].GetComponentInParent<Player>() : null;

            if (TryDamagePlayer(hitPlayer, damage))
            {
                if (useCooldown)
                    lastContactDamageTime = Time.time;

                PlayAudioCue("slime_contact");
                return true;
            }
        }

        return false;
    }

    private bool TryDamagePlayerByPhaseOneContact()
    {
        if (Time.time < lastContactDamageTime + slimeContactCooldown)
            return false;

        if (phaseOneContactHitbox != null && phaseOneContactHitbox.enabled && phaseOneContactHitbox.gameObject.activeInHierarchy)
        {
            int hitCount = phaseOneContactHitbox.OverlapCollider(playerContactFilter, overlapHits);

            for (int i = 0; i < hitCount; i++)
            {
                Player hitPlayer = overlapHits[i] != null ? overlapHits[i].GetComponentInParent<Player>() : null;

                if (TryDamagePlayer(hitPlayer, slimeContactDamage))
                {
                    lastContactDamageTime = Time.time;
                    PlayAudioCue("slime_contact");
                    return true;
                }
            }

            return false;
        }

        return TryDamagePlayerByCircle(slimeContactRadius, slimeContactDamage, true);
    }

    private void DealCircleDamage(Transform center, float radius, int damage)
    {
        Vector2 origin = center != null ? center.position : GetBossCenterPosition();
        int hitCount = Physics2D.OverlapCircle(origin, Mathf.Max(0.1f, radius), playerContactFilter, overlapHits);

        for (int i = 0; i < hitCount; i++)
        {
            if (TryDamagePlayer(overlapHits[i] != null ? overlapHits[i].GetComponentInParent<Player>() : null, damage))
                return;
        }
    }

    private void DealBoxDamage(Transform center, Vector2 size, int damage)
    {
        Vector2 origin = center != null ? center.position : GetBossCenterPosition();
        Vector2 safeSize = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
        int hitCount = Physics2D.OverlapBox(origin, safeSize, 0f, playerContactFilter, overlapHits);

        for (int i = 0; i < hitCount; i++)
        {
            if (TryDamagePlayer(overlapHits[i] != null ? overlapHits[i].GetComponentInParent<Player>() : null, damage))
                return;
        }
    }

    private void DealBoxDamage(BoxCollider2D hitbox, Transform fallbackCenter, Vector2 fallbackSize, int damage)
    {
        if (hitbox == null || !hitbox.enabled || !hitbox.gameObject.activeInHierarchy)
        {
            DealBoxDamage(fallbackCenter, fallbackSize, damage);
            return;
        }

        int hitCount = hitbox.OverlapCollider(playerContactFilter, overlapHits);

        for (int i = 0; i < hitCount; i++)
        {
            if (TryDamagePlayer(overlapHits[i] != null ? overlapHits[i].GetComponentInParent<Player>() : null, damage))
                return;
        }
    }

    private bool TryDamagePlayer(Player hitPlayer, int damage)
    {
        if (hitPlayer == null)
            return false;

        PlayerStats targetStats = hitPlayer.GetComponent<PlayerStats>();

        if (targetStats == null || targetStats.isDead)
            return false;

        if (damage <= 0)
            return false;

        if (hitPlayer.TryStartPreciseDodge(transform))
            return true;

        targetStats.TakeDamage(damage);
        return true;
    }

    private void SpawnProjectile()
    {
        if (!CanUseCastSpellAttack())
            return;

        ResolvePlayer();

        Vector3 spawnPosition = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position + Vector3.right * facingDir * 0.8f + Vector3.up * 0.5f;

        GameObject projectileObject = projectilePrefab != null
            ? Instantiate(projectilePrefab, spawnPosition, Quaternion.identity)
            : CreateFallbackProjectile(spawnPosition);

        DemonBossProjectile projectile = projectileObject.GetComponent<DemonBossProjectile>();

        if (projectile == null)
            projectile = projectileObject.AddComponent<DemonBossProjectile>();

        Vector2 direction = player != null
            ? ((Vector2)player.position - (Vector2)spawnPosition).normalized
            : Vector2.right * facingDir;

        projectile.Setup(this, direction, projectileSpeed, castSpellDamage);
        PlayAudioCue("projectile_spawn");
    }

    private GameObject CreateFallbackProjectile(Vector3 spawnPosition)
    {
        GameObject projectileObject = new GameObject("Demon Boss Projectile");
        projectileObject.transform.position = spawnPosition;

        CircleCollider2D circleCollider = projectileObject.AddComponent<CircleCollider2D>();
        circleCollider.radius = 0.18f;
        circleCollider.isTrigger = true;

        projectileObject.AddComponent<Rigidbody2D>();
        return projectileObject;
    }

    public override void DamageImpact()
    {
        if (currentMode == BossMode.Dead || currentMode == BossMode.Transition || preciseDodgeTimeStopped)
            return;

        if (currentMode == BossMode.Attack)
            return;

        if (currentPhase == BossPhase.Slime)
        {
            StartHitReaction(slimeHitState);
            return;
        }

        if (advancedSuperArmor)
            return;

        phaseTwoHitReactionCount++;
        // Start the armor timer after this stagger, but prevent restarting it.
        if (HasReachedPhaseTwoHitReactionThreshold())
            advancedSuperArmor = true;
        StartHitReaction(demonHitState);
    }

    private void StartHitReaction(string hitState)
    {
        if (currentActionRoutine != null)
            StopCoroutine(currentActionRoutine);

        StopFireBreathDamageWindow();
        ClearQueuedDemonAttack();
        ClearActiveDemonAttack();
        currentActionRoutine = StartCoroutine(HitReactionRoutine(hitState));
    }

    private IEnumerator HitReactionRoutine(string hitState)
    {
        currentMode = BossMode.Hit;
        StopBodyMotion();

        float length = PlayStateAndGetLength(hitState);
        yield return WaitWhileNotTimeStopped(Mathf.Max(hitReactionDuration, length));

        if (currentPhase == BossPhase.Demon && HasReachedPhaseTwoHitReactionThreshold())
            BeginAdvancedSuperArmor();

        currentMode = BossMode.Idle;
        currentActionRoutine = null;
    }

    private bool HasReachedPhaseTwoHitReactionThreshold()
    {
        return phaseTwoHitReactionCount >= Mathf.Max(1, phaseTwoHitReactionsBeforeAdvancedArmor);
    }

    private void BeginAdvancedSuperArmor()
    {
        advancedSuperArmor = true;
        phaseTwoHitReactionCount = 0;

        if (advancedArmorRoutine != null)
            StopCoroutine(advancedArmorRoutine);

        advancedArmorRoutine = StartCoroutine(AdvancedSuperArmorRoutine());
    }

    private IEnumerator AdvancedSuperArmorRoutine()
    {
        yield return WaitWhileNotTimeStopped(advancedSuperArmorDuration);
        advancedSuperArmor = false;
        advancedArmorRoutine = null;
    }

    private void StopAdvancedSuperArmor()
    {
        if (advancedArmorRoutine != null)
        {
            StopCoroutine(advancedArmorRoutine);
            advancedArmorRoutine = null;
        }

        advancedSuperArmor = false;
        phaseTwoHitReactionCount = 0;
    }

    public override void FreezeTime(bool _timeFrozen)
    {
        if (IsImmuneToGenericEnemyControl)
            return;

        ApplyTimeStop(_timeFrozen);
    }

    public override void FreezeTimeFor(float _duration)
    {
    }

    public void SetPreciseDodgeTimeStop(bool timeStopped)
    {
        ApplyTimeStop(timeStopped);
    }

    private void ApplyTimeStop(bool timeStopped)
    {
        preciseDodgeTimeStopped = timeStopped;

        if (anim != null)
            anim.speed = timeStopped ? 0f : 1f;

        if (timeStopped)
            StopBodyMotion();
    }

    public override bool CanBeStunned()
    {
        return false;
    }

    public override void OpenCounterAttackWindow()
    {
        canBeStunned = false;

        if (counterImage != null)
            counterImage.SetActive(false);
    }

    public override void CloseCounterAttackWindow()
    {
        canBeStunned = false;

        if (counterImage != null)
            counterImage.SetActive(false);
    }

    public override void Die()
    {
        if (currentMode == BossMode.Dead)
            return;

        currentMode = BossMode.Dead;
        StopBodyMotion();
        EndPhaseTransitionPhysicsLock();
        StopAdvancedSuperArmor();
        FreezeBodyForDeath();
        NotifyDeathStarted();
        UnlockPlayerForPhaseTransition();
        StopFireBreathDamageWindow();
        ClearQueuedDemonAttack();
        ClearActiveDemonAttack();

        if (currentActionRoutine != null)
        {
            StopCoroutine(currentActionRoutine);
            currentActionRoutine = null;
        }

        DisableBodyCollision(false);

        if (bossStats == null || !bossStats.isDead)
            PlayAudioCue("death");

        PlayState(demonDeathState);
        HideBossUi();
        NotifyBossDefeatedCleanup();

        if (destroyDelayAfterDeath > 0f)
            Destroy(gameObject, destroyDelayAfterDeath);
    }

    private void NotifyDeathStarted()
    {
        if (deathStartedNotified)
            return;

        deathStartedNotified = true;
        OnDeathStarted?.Invoke(this);
        AnyDeathStarted?.Invoke(this);
    }

    private string GetIdleStateForCurrentPhase()
    {
        return currentPhase == BossPhase.Slime ? slimeIdleState : demonIdleState;
    }

    private bool IsLockedPlayerUnavailable()
    {
        if (player == null)
            return true;

        Player playerComponent = player.GetComponent<Player>();

        if (playerComponent == null || !playerComponent.isActiveAndEnabled)
            return true;

        CharacterStats playerStats = playerComponent.stats != null
            ? playerComponent.stats
            : playerComponent.GetComponent<CharacterStats>();

        return playerStats != null && playerStats.isDead;
    }

    private bool CanSeePlayer(float radius)
    {
        if (!CanDetectPlayer())
            return false;

        ResolvePlayer();

        if (player == null)
            return false;

        return GetHorizontalDistanceToPlayer() <= radius;
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            player = PlayerManager.instance.player.transform;
        else
        {
            Player foundPlayer = FindObjectOfType<Player>();

            if (foundPlayer != null)
                player = foundPlayer.transform;
        }
    }

    private void ResolveAudioEvents()
    {
        if (audioEvents != null)
            return;

        audioEvents = GetComponent<AudioEventPlayer>();

        if (audioEvents == null)
            audioEvents = GetComponentInChildren<AudioEventPlayer>();
    }

    private void ResolveBossUiReferences()
    {
        if (bossUiRoot == null)
            bossUiRoot = FindSceneObjectByName(bossUiRootName);

        if (redBossUiGroup == null && bossUiRoot != null)
        {
            Transform group = FindChildRecursive(bossUiRoot.transform, redBossUiGroupName);

            if (group == null)
                group = FindChildRecursiveContaining(bossUiRoot.transform, "\u8d64\u9ad3");

            if (group != null)
                redBossUiGroup = group.gameObject;
        }

        Transform searchRoot = redBossUiGroup != null ? redBossUiGroup.transform : null;

        if (phaseOneUiGroup == null && searchRoot != null)
        {
            Transform phaseOne = FindChildRecursive(searchRoot, phaseOneUiGroupName);

            if (phaseOne != null)
                phaseOneUiGroup = phaseOne.gameObject;
        }

        if (phaseTwoUiGroup == null && searchRoot != null)
        {
            Transform phaseTwo = FindChildRecursive(searchRoot, phaseTwoUiGroupName);

            if (phaseTwo != null)
                phaseTwoUiGroup = phaseTwo.gameObject;
        }

        ConfigurePhaseHealthBar(phaseOneUiGroup, ref phaseOneHealthSlider, ref phaseOneHealthBar);
        ConfigurePhaseHealthBar(phaseTwoUiGroup, ref phaseTwoHealthSlider, ref phaseTwoHealthBar);
        ResolvePhaseTwoIntroText();
    }

    private void ConfigurePhaseHealthBar(GameObject phaseGroup, ref Slider slider, ref UI_BossHealthBar healthBar)
    {
        if (phaseGroup == null)
            return;

        if (slider == null)
        {
            Transform sliderTransform = FindChildRecursive(phaseGroup.transform, healthSliderName);

            if (sliderTransform != null)
                slider = sliderTransform.GetComponent<Slider>();

            if (slider == null)
                slider = phaseGroup.GetComponentInChildren<Slider>(true);
        }

        if (slider == null)
            return;

        if (healthBar == null)
        {
            healthBar = slider.GetComponent<UI_BossHealthBar>();

            if (healthBar == null)
                healthBar = slider.gameObject.AddComponent<UI_BossHealthBar>();
        }

        healthBar.Configure(slider, slider.gameObject);
        healthBar.SetHideWhenTargetDies(false);
    }

    private void RefreshBossPhaseUi()
    {
        ResolveBossUiReferences();

        if (redBossUiGroup == null)
            return;

        if (!bossCombatStarted || currentMode == BossMode.Dead)
        {
            if (phaseOneHealthBar != null)
                phaseOneHealthBar.Hide();

            if (phaseTwoHealthBar != null)
                phaseTwoHealthBar.Hide();

            if (currentMode == BossMode.Dead)
                redBossUiGroup.SetActive(false);

            return;
        }

        if (bossUiRoot != null)
        {
            bossUiRoot.SetActive(true);
            bossUiRoot.transform.SetAsLastSibling();
        }

        redBossUiGroup.SetActive(true);

        bool phaseOneActive = currentPhase == BossPhase.Slime;

        if (phaseOneUiGroup != null)
            phaseOneUiGroup.SetActive(phaseOneActive);

        if (phaseTwoUiGroup != null)
            phaseTwoUiGroup.SetActive(!phaseOneActive);

        UI_BossHealthBar activeHealthBar = phaseOneActive ? phaseOneHealthBar : phaseTwoHealthBar;
        UI_BossHealthBar inactiveHealthBar = phaseOneActive ? phaseTwoHealthBar : phaseOneHealthBar;

        if (inactiveHealthBar != null)
            inactiveHealthBar.Hide();

        if (activeHealthBar != null)
        {
            activeHealthBar.Bind(bossStats);
            activeHealthBar.Show();
        }
    }

    private void HideBossUi()
    {
        ResolveBossUiReferences();

        if (phaseOneHealthBar != null)
            phaseOneHealthBar.Hide();

        if (phaseTwoHealthBar != null)
            phaseTwoHealthBar.Hide();

        if (redBossUiGroup != null)
            redBossUiGroup.SetActive(false);

        if (phaseOneUiGroup != null)
            phaseOneUiGroup.SetActive(false);

        if (phaseTwoUiGroup != null)
            phaseTwoUiGroup.SetActive(false);

        if (bossUiRoot != null)
            bossUiRoot.SetActive(false);
    }

    private void HidePhaseOneBossUiForTransition()
    {
        ResolveBossUiReferences();

        if (phaseOneHealthBar != null)
            phaseOneHealthBar.Hide();

        if (phaseTwoHealthBar != null)
            phaseTwoHealthBar.Hide();

        if (phaseOneUiGroup != null)
            phaseOneUiGroup.SetActive(false);

        if (phaseTwoUiGroup != null)
            phaseTwoUiGroup.SetActive(false);

        if (redBossUiGroup != null)
            redBossUiGroup.SetActive(false);

        if (bossUiRoot != null)
            bossUiRoot.SetActive(false);

        BossIntroSequenceDirector introDirector = GetComponent<BossIntroSequenceDirector>();
        if (introDirector != null)
            introDirector.ForceHideCombatUi(true);
    }

    private void NotifyBossDefeatedCleanup()
    {
        BossIntroSequenceDirector introDirector = GetComponent<BossIntroSequenceDirector>();

        if (introDirector != null)
            introDirector.ForceBossDefeatedCleanup();
        else
            BGMPlayer.PlayLastRegularBGM();
    }

    private void StopBgmForPhaseTransition()
    {
        AudioManager.GetOrCreateInstance().StopBGM();
    }

    private void PlayPhaseTwoBgm()
    {
        if (phaseTwoBgmPlayer != null)
            phaseTwoBgmPlayer.PlayConfiguredBGM();
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];

            if (target == null || !target.gameObject.scene.IsValid() || target.gameObject.scene != gameObject.scene)
                continue;

            if (target.name == objectName)
                return target.gameObject;
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

    private bool PlayAudioCue(string cueName)
    {
        ResolveAudioEvents();
        return audioEvents != null && audioEvents.PlayCue(cueName);
    }

    private Vector2 GetDirectionToPlayer()
    {
        if (player == null)
            return Vector2.right * facingDir;

        Vector2 direction = (Vector2)player.position - GetBossCombatBasePosition();
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right * facingDir;
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        float directionX = player.position.x - GetBossCombatBasePosition().x;

        if (directionX > 0.05f && facingDir < 0)
            Flip();
        else if (directionX < -0.05f && facingDir > 0)
            Flip();
    }

    private float PlayStateAndGetLength(string stateName)
    {
        PlayState(stateName);
        return Mathf.Max(0.05f, GetAnimationLength(stateName));
    }

    private void PlayState(string stateName)
    {
        if (anim == null || string.IsNullOrWhiteSpace(stateName))
            return;

        int hash = Animator.StringToHash(stateName);

        if (!anim.HasState(0, hash))
            return;

        anim.speed = preciseDodgeTimeStopped ? 0f : 1f;
        anim.Play(hash, 0, 0f);
        anim.Update(0f);
        RefreshBodyColliderFromVisual();
    }

    private void PauseAnimatorAtFinalFrame(string stateName)
    {
        if (anim == null || string.IsNullOrWhiteSpace(stateName))
            return;

        int hash = Animator.StringToHash(stateName);

        if (!anim.HasState(0, hash))
            return;

        anim.Play(hash, 0, 0.999f);
        anim.Update(0f);
        anim.speed = 0f;
        RefreshBodyColliderFromVisual();
    }

    private void PlayPhaseTwoIntro()
    {
        string introState = string.IsNullOrWhiteSpace(phaseTwoIntroState)
            ? demonIdleState
            : phaseTwoIntroState;

        PlayState(introState);
    }

    private IEnumerator PhaseTwoIntroRoutine()
    {
        PlayPhaseTwoIntro();
        ResolveBossUiReferences();

        if (bossUiRoot != null)
        {
            bossUiRoot.SetActive(true);
            bossUiRoot.transform.SetAsLastSibling();
        }

        if (redBossUiGroup != null)
            redBossUiGroup.SetActive(true);

        if (phaseOneUiGroup != null)
            phaseOneUiGroup.SetActive(false);

        if (phaseTwoUiGroup != null)
            phaseTwoUiGroup.SetActive(true);

        if (phaseOneHealthBar != null)
            phaseOneHealthBar.Hide();

        if (phaseTwoHealthBar != null)
            phaseTwoHealthBar.Hide();

        TMP_Text introText = ResolvePhaseTwoIntroText();
        float elapsed = 0f;

        if (introText != null)
        {
            string fullText = string.IsNullOrWhiteSpace(phaseTwoIntroDisplayText)
                ? phaseTwoUiGroupName
                : phaseTwoIntroDisplayText;

            EnsureParentsActive(introText.transform, phaseTwoUiGroup != null ? phaseTwoUiGroup.transform : redBossUiGroup != null ? redBossUiGroup.transform : null);
            introText.gameObject.SetActive(true);
            introText.text = string.Empty;

            float secondsPerCharacter = 1f / Mathf.Max(0.1f, phaseTwoIntroCharactersPerSecond);
            float timer = 0f;
            int visibleCharacters = 0;

            while (visibleCharacters < fullText.Length)
            {
                if (!preciseDodgeTimeStopped)
                {
                    float deltaTime = Time.deltaTime;
                    timer += deltaTime;
                    elapsed += deltaTime;

                    while (timer >= secondsPerCharacter && visibleCharacters < fullText.Length)
                    {
                        timer -= secondsPerCharacter;
                        visibleCharacters++;
                        introText.text = fullText.Substring(0, visibleCharacters);
                    }
                }

                yield return null;
            }

            introText.text = fullText;
        }

        while (elapsed < phaseTwoIntroDuration)
        {
            if (!preciseDodgeTimeStopped)
                elapsed += Time.deltaTime;

            yield return null;
        }

        if (introText != null)
        {
            introText.text = string.Empty;
            introText.gameObject.SetActive(false);
        }
    }

    private TMP_Text ResolvePhaseTwoIntroText()
    {
        if (phaseTwoIntroText != null)
            return phaseTwoIntroText;

        if (phaseTwoUiGroup == null || string.IsNullOrWhiteSpace(phaseIntroTextName))
            return null;

        Transform textTransform = FindChildRecursive(phaseTwoUiGroup.transform, phaseIntroTextName);

        if (textTransform == null)
            textTransform = FindChildRecursiveContaining(phaseTwoUiGroup.transform, phaseIntroTextName);

        if (textTransform == null)
            return null;

        phaseTwoIntroText = textTransform.GetComponent<TMP_Text>();

        if (phaseTwoIntroText != null && string.IsNullOrWhiteSpace(phaseTwoIntroDisplayText))
            phaseTwoIntroDisplayText = phaseTwoIntroText.text;

        return phaseTwoIntroText;
    }

    private void EnsureParentsActive(Transform child, Transform stopAt)
    {
        Transform current = child;

        while (current != null)
        {
            current.gameObject.SetActive(true);

            if (current == stopAt)
                break;

            current = current.parent;
        }
    }

    private void PlayStateIfNotCurrent(string stateName)
    {
        if (anim == null || string.IsNullOrWhiteSpace(stateName))
            return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.shortNameHash == Animator.StringToHash(stateName))
            return;

        PlayState(stateName);
    }

    private IEnumerator WaitForAnimation(string stateName)
    {
        yield return WaitWhileNotTimeStopped(GetAnimationLength(stateName));
    }

    private IEnumerator WaitWhileNotTimeStopped(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            if (!preciseDodgeTimeStopped)
                elapsed += Time.deltaTime;

            yield return null;
        }
    }

    private float GetAnimationLength(string stateName)
    {
        if (anim == null || anim.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            return fallbackAnimationLength;

        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;

        if (clips == null)
            return fallbackAnimationLength;

        string normalizedTarget = NormalizeAnimationName(stateName);

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];

            if (clip == null)
                continue;

            if (NormalizeAnimationName(clip.name) == normalizedTarget)
                return Mathf.Max(0.05f, clip.length);
        }

        return fallbackAnimationLength;
    }

    private bool HasAnimationEvent(string stateName, string functionName)
    {
        if (anim == null || anim.runtimeAnimatorController == null)
            return false;

        string normalizedTarget = NormalizeAnimationName(stateName);
        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
        {
            if (clip == null || NormalizeAnimationName(clip.name) != normalizedTarget)
                continue;

            foreach (AnimationEvent animationEvent in clip.events)
                if (animationEvent.functionName == functionName)
                    return true;
        }

        return false;
    }

    private string NormalizeAnimationName(string animationName)
    {
        return string.IsNullOrWhiteSpace(animationName)
            ? string.Empty
            : animationName.Replace("_", string.Empty).Replace("-", string.Empty).Replace("/", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
    }

    private void StopBodyMotion()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private void FreezeBodyForDeath()
    {
        if (rb == null)
            return;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        Physics2D.SyncTransforms();
    }

    private void LockPlayerForPhaseTransition()
    {
        if (phaseTransitionLockedPlayer != null)
            return;

        Player playerComponent = ResolvePlayerComponent();

        if (playerComponent == null)
            return;

        phaseTransitionLockedPlayer = playerComponent;
        phaseTransitionLockedPlayer.BeginCutsceneControlLock();
    }

    private void UnlockPlayerForPhaseTransition()
    {
        if (phaseTransitionLockedPlayer == null)
            return;

        phaseTransitionLockedPlayer.EndCutsceneControlLock();
        phaseTransitionLockedPlayer = null;
    }

    private Player ResolvePlayerComponent()
    {
        ResolvePlayer();

        if (player != null)
        {
            Player playerComponent = player.GetComponent<Player>();

            if (playerComponent != null)
                return playerComponent;
        }

        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            return PlayerManager.instance.player;

        return FindObjectOfType<Player>();
    }

    private void DisableBodyCollision(bool enabled)
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = enabled;
        }
    }

    private void BeginPhaseTransitionPhysicsLock()
    {
        if (rb == null || transitionPhysicsLocked)
            return;

        savedTransitionGravityScale = rb.gravityScale;
        savedTransitionConstraints = rb.constraints;
        savedTransitionBodyType = rb.bodyType;
        savedTransitionRootPosition = transform.position;
        transitionPhysicsLocked = true;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
    }

    private void EndPhaseTransitionPhysicsLock()
    {
        if (rb == null || !transitionPhysicsLocked)
            return;

        RestoreTransitionRootPosition();
        rb.bodyType = savedTransitionBodyType;
        rb.gravityScale = savedTransitionGravityScale;
        rb.constraints = savedTransitionConstraints;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transitionPhysicsLocked = false;
    }

    private void RestoreTransitionRootPosition()
    {
        if (!transitionPhysicsLocked)
            return;

        transform.position = savedTransitionRootPosition;

        if (rb != null)
        {
            rb.position = (Vector2)savedTransitionRootPosition;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Physics2D.SyncTransforms();
    }

    private void SnapVisualBottomToGround()
    {
        if (!snapVisualBottomToGround || whatIsGround.value == 0)
            return;

        if (visualSpriteRenderer == null)
            CacheVisualReferences();

        if (visualSpriteRenderer == null)
            return;

        Bounds bounds = visualSpriteRenderer.bounds;

        if (bounds.size.sqrMagnitude < 0.0001f)
            return;

        float snapDistance = Mathf.Max(0.1f, phaseTransitionGroundSnapDistance);
        Vector2 origin = new Vector2(bounds.center.x, bounds.max.y + 0.2f);
        float rayDistance = bounds.size.y + snapDistance + 0.2f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayDistance, whatIsGround);

        if (hit.collider == null)
            return;

        float targetMinY = hit.point.y + phaseTransitionGroundSkin;
        float deltaY = targetMinY - bounds.min.y;

        if (Mathf.Abs(deltaY) <= 0.001f)
            return;

        MoveRootTo(transform.position + Vector3.up * deltaY, true);
        RefreshBodyColliderFromVisual();
    }

    private void MoveRootTo(Vector3 worldPosition, bool preserveHorizontalVelocity = false)
    {
        Vector2 previousVelocity = rb != null ? rb.velocity : Vector2.zero;
        transform.position = worldPosition;

        if (transitionPhysicsLocked)
            savedTransitionRootPosition = worldPosition;

        if (rb != null)
        {
            rb.position = (Vector2)worldPosition;
            rb.velocity = preserveHorizontalVelocity ? new Vector2(previousVelocity.x, 0f) : Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Physics2D.SyncTransforms();
    }

    private void CacheVisualReferences()
    {
        if (visualRoot == null)
        {
            Animator childAnimator = GetComponentInChildren<Animator>(true);

            if (childAnimator != null && childAnimator.transform != transform)
                visualRoot = childAnimator.transform;
        }

        if (visualSpriteRenderer == null)
        {
            if (visualRoot != null)
                visualSpriteRenderer = visualRoot.GetComponent<SpriteRenderer>();

            if (visualSpriteRenderer == null)
                visualSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
    }

    private void AlignRootToVisualCenter()
    {
        if (!alignRootToVisualOnAwake || rootAlignedToVisual || visualRoot == null || visualRoot == transform)
            return;

        Vector3 visualWorldPosition = visualRoot.position;
        Vector2 offset = visualWorldPosition - transform.position;

        if (offset.sqrMagnitude < 0.0001f)
            return;

        transform.position = new Vector3(visualWorldPosition.x, visualWorldPosition.y, transform.position.z);
        visualRoot.position = visualWorldPosition;
        rootAlignedToVisual = true;
    }

    private void RefreshBodyColliderFromVisual()
    {
        if (!autoFitColliderToVisual)
            return;

        if (bossCollider == null)
            bossCollider = GetComponent<CapsuleCollider2D>();

        if (visualSpriteRenderer == null)
            CacheVisualReferences();

        if (bossCollider == null || visualSpriteRenderer == null)
            return;

        Bounds bounds = visualSpriteRenderer.bounds;

        if (bounds.size.sqrMagnitude < 0.0001f)
            return;

        float scaleX = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);

        bossCollider.direction = CapsuleDirection2D.Vertical;
        bossCollider.offset = new Vector2(localCenter.x, localCenter.y);
        bossCollider.size = new Vector2(
            Mathf.Max(0.3f, bounds.size.x / scaleX * colliderWidthScale + colliderSizePadding.x),
            Mathf.Max(0.3f, bounds.size.y / scaleY * colliderHeightScale + colliderSizePadding.y));
        bossCollider.isTrigger = false;
    }

    private Vector2 GetBossCenterPosition()
    {
        return GetBossCombatBasePosition();
    }

    private Vector2 GetBossCombatBasePosition()
    {
        if (visualRoot == null)
            CacheVisualReferences();

        if (visualRoot != null)
            return visualRoot.position;

        if (groundCheak != null)
            return groundCheak.position;

        return transform.position;
    }

    private Vector3 GetVisualFocusPosition()
    {
        Vector3 focusOffset = currentPhase == BossPhase.Demon || currentMode == BossMode.Transition
            ? phaseTwoFocusOffset
            : new Vector3(0f, 1.2f, 0f);

        return (Vector3)GetBossCombatBasePosition() + focusOffset;
    }

    private float GetHorizontalDistanceToPlayer()
    {
        if (player == null)
            return 999f;

        return Mathf.Abs(player.position.x - GetBossCombatBasePosition().x);
    }

    private float GetHorizontalDirectionToPlayer()
    {
        if (player == null)
            return facingDir;

        float deltaX = player.position.x - GetBossCombatBasePosition().x;
        return Mathf.Abs(deltaX) > 0.05f ? Mathf.Sign(deltaX) : facingDir;
    }

    private float GetDemonMoveSpeed()
    {
        return Mathf.Max(0.1f, Mathf.Max(moveSpeed, demonMoveSpeed));
    }

    private bool CanUseCastSpellAttack()
    {
        return enableCastSpellAttack && GetEffectiveCastSpellWeight() > 0f;
    }

    private float GetEffectiveCastSpellWeight()
    {
        return enableCastSpellAttack ? Mathf.Max(0f, castSpellWeight) : 0f;
    }

    private float GetEffectiveFireBreathWeight()
    {
        return Mathf.Max(0f, fireBreathWeight) * Mathf.Clamp01(fireBreathSelectionWeightMultiplier);
    }

    private void EnsureRequiredComponents()
    {
        if (!TryGetComponent(out Rigidbody2D body))
            body = gameObject.AddComponent<Rigidbody2D>();

        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (!TryGetComponent(out bossCollider))
            bossCollider = gameObject.AddComponent<CapsuleCollider2D>();

        bossCollider.direction = CapsuleDirection2D.Vertical;
        bossCollider.isTrigger = false;

        if (!TryGetComponent<DemonBossStats>(out _))
            gameObject.AddComponent<DemonBossStats>();

        if (!TryGetComponent<EntityFX>(out _))
            gameObject.AddComponent<EntityFX>();

        if (!TryGetComponent<BossPhaseTransitionDirector>(out _))
            gameObject.AddComponent<BossPhaseTransitionDirector>();
    }

    private void EnsureCheckPoints()
    {
        if (attackCheak == null)
            attackCheak = EnsureChildPoint("AttackCheck", new Vector3(-0.75f, 0.35f, 0f));

        if (cleaveCheck == null)
            cleaveCheck = EnsureChildPoint("CleaveCheck", new Vector3(-1.25f, 0.45f, 0f));

        if (fireBreathCheck == null)
            fireBreathCheck = EnsureChildPoint("FireBreathCheck", new Vector3(-1.8f, 0.55f, 0f));

        if (smashCheck == null)
            smashCheck = EnsureChildPoint("SmashCheck", new Vector3(-0.9f, -0.25f, 0f));

        if (projectileSpawnPoint == null)
            projectileSpawnPoint = EnsureChildPoint("ProjectileSpawnPoint", new Vector3(-0.95f, 0.85f, 0f));

        if (groundCheak == null)
            groundCheak = EnsureChildPoint("GroundCheck", new Vector3(0f, -0.95f, 0f));

        if (wallCheak == null)
            wallCheak = EnsureChildPoint("WallCheck", new Vector3(0.7f, -0.2f, 0f));

        if (attackCheakRidus <= 0f)
            attackCheakRidus = slimeContactRadius;
    }

    private void EnsureAttackHitboxes()
    {
        Transform container = transform.Find("AttackHitboxes");

        if (container == null)
        {
            GameObject containerObject = new GameObject("AttackHitboxes");
            container = containerObject.transform;
            container.SetParent(transform, false);
            container.localPosition = Vector3.zero;
            container.localRotation = Quaternion.identity;
            container.localScale = Vector3.one;
        }

        phaseOneContactHitbox = EnsureHitbox(container, phaseOneContactHitbox, "Phase1_Contact", new Vector2(-0.2f, 0.25f), new Vector2(1.5f, 1.2f));
        phaseTwoCleaveHitbox = EnsureHitbox(container, phaseTwoCleaveHitbox, "Phase2_Cleave", new Vector2(-1.45f, 0.45f), new Vector2(3f, 1.6f));
        phaseTwoSmashHitbox = EnsureHitbox(container, phaseTwoSmashHitbox, "Phase2_Smash", new Vector2(-1f, -0.15f), new Vector2(2.4f, 1.2f));
        phaseTwoFireBreathHitbox = EnsureHitbox(container, phaseTwoFireBreathHitbox, "Phase2_FireBreath", new Vector2(-2.3f, 0.55f), new Vector2(4.6f, 1.5f));
    }

    private BoxCollider2D EnsureHitbox(Transform container, BoxCollider2D existingHitbox, string hitboxName, Vector2 localPosition, Vector2 size)
    {
        if (existingHitbox != null)
        {
            existingHitbox.isTrigger = true;
            return existingHitbox;
        }

        Transform hitboxTransform = container.Find(hitboxName);
        bool createdTransform = false;

        if (hitboxTransform == null)
        {
            GameObject hitboxObject = new GameObject(hitboxName);
            hitboxTransform = hitboxObject.transform;
            hitboxTransform.SetParent(container, false);
            hitboxTransform.localPosition = localPosition;
            hitboxTransform.localRotation = Quaternion.identity;
            hitboxTransform.localScale = Vector3.one;
            createdTransform = true;
        }

        BoxCollider2D hitbox = hitboxTransform.GetComponent<BoxCollider2D>();
        bool createdCollider = false;

        if (hitbox == null)
        {
            hitbox = hitboxTransform.gameObject.AddComponent<BoxCollider2D>();
            createdCollider = true;
        }

        if (createdTransform || createdCollider)
        {
            hitbox.offset = Vector2.zero;
            hitbox.size = size;
        }

        hitbox.isTrigger = true;
        return hitbox;
    }

    private void SyncInitialFacingWithVisual()
    {
        bool flippedByRotation = Mathf.Abs(Mathf.DeltaAngle(0f, transform.eulerAngles.y)) > 90f;
        bool flippedByScale = transform.lossyScale.x < 0f;
        bool visualFlipped = flippedByRotation ^ flippedByScale;
        bool visualFacesRight = defaultVisualFacesLeft ? visualFlipped : !visualFlipped;
        bool facingMatchesVisual = visualFacesRight ? facingDir > 0 : facingDir < 0;

        if (facingMatchesVisual)
            return;

        Flip();
        transform.Rotate(0f, 180f, 0f);
    }

    private Transform EnsureChildPoint(string childName, Vector3 localPosition)
    {
        Transform child = transform.Find(childName);

        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(transform, false);
        }

        child.localPosition = localPosition;
        return child;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 bossCenter = Application.isPlaying ? GetBossCenterPosition() : (Vector2)transform.position;

        Gizmos.color = new Color(1f, 0.35f, 0.15f, 0.55f);
        Gizmos.DrawWireSphere(bossCenter, slimeDetectionRadius);

        Gizmos.color = new Color(0.95f, 0.15f, 0.05f, 0.55f);
        Gizmos.DrawWireSphere(bossCenter, demonDetectionRadius);

        DrawHitboxGizmo(phaseOneContactHitbox, new Color(1f, 0.85f, 0.15f, 0.55f));
        DrawHitboxGizmo(phaseTwoCleaveHitbox, new Color(1f, 0.72f, 0.1f, 0.6f));
        DrawHitboxGizmo(phaseTwoSmashHitbox, new Color(1f, 0.45f, 0.05f, 0.6f));
        if (useSpriteFireBreathHitbox)
        {
            if (visualSpriteRenderer == null)
                CacheVisualReferences();
            if (spriteFireBreathHitbox == null)
                spriteFireBreathHitbox = new DemonFireBreathHitbox(fireBreathHitboxData);
            spriteFireBreathHitbox.DrawGizmos(visualSpriteRenderer,
                new Color(1f, 0.15f, 0f, Application.isPlaying && fireBreathActive ? 0.4f : 0.15f));
        }
        else
            DrawHitboxGizmo(phaseTwoFireBreathHitbox, new Color(1f, 0.25f, 0.05f, 0.5f));

        if (phaseTwoCleaveHitbox == null)
            DrawBoxGizmo(cleaveCheck, cleaveBoxSize, new Color(1f, 0.75f, 0.15f, 0.55f));

        if (!useSpriteFireBreathHitbox && phaseTwoFireBreathHitbox == null)
            DrawBoxGizmo(fireBreathCheck, fireBreathBoxSize, new Color(1f, 0.25f, 0.05f, 0.45f));

        if (phaseTwoSmashHitbox == null)
        {
            Gizmos.color = new Color(1f, 0.15f, 0.05f, 0.45f);
            Gizmos.DrawWireSphere(smashCheck != null ? smashCheck.position : (Vector3)bossCenter, smashRadius);
        }
    }

    private void DrawBoxGizmo(Transform center, Vector2 size, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(center != null ? center.position : (Vector3)GetBossCenterPosition(), size);
    }

    private void DrawHitboxGizmo(BoxCollider2D hitbox, Color color)
    {
        if (hitbox == null)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = hitbox.transform.localToWorldMatrix;
        Gizmos.color = color;
        Gizmos.DrawWireCube(hitbox.offset, hitbox.size);
        Gizmos.matrix = previousMatrix;
    }
}
