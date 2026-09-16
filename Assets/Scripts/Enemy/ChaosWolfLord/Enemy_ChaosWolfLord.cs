using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_ChaosWolfLord : Enemy, IGenericControlImmuneEnemy, IPreciseDodgeTimeStopTarget, IBossIntroPresentationTarget, IBossCombatActivationTarget
{
    private enum WolfState
    {
        Idle,
        Chase,
        Dash,
        Attack,
        Hit,
        Dead
    }

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 9f;
    [SerializeField, Min(0.1f)] private float loseTargetRadius = 11f;
    [SerializeField, Min(0.1f)] private float preferredAttackDistance = 2.6f;
    [SerializeField, Min(0.1f)] private float chaseMoveSpeed = 4.2f;

    [Header("Dash Chase")]
    [SerializeField] private bool enableDashChase = true;
    [SerializeField, Min(0.1f)] private float dashCooldown = 5f;
    [SerializeField, Min(0.1f)] private float dashSpeed = 16f;
    [SerializeField, Min(0.05f)] private float dashDuration = 0.22f;
    [SerializeField, Min(0.1f)] private float dashMinDistance = 4f;
    [SerializeField, Min(0.1f)] private float dashStopDistance = 2.2f;
    [SerializeField, Min(0f)] private float dashWindupTime = 0.12f;
    [SerializeField, Min(0f)] private float dashRecovery = 0.14f;

    [Header("Combo Attack")]
    [SerializeField, Min(0.05f)] private float attackCooldown = 1.15f;
    [SerializeField, Min(0f)] private float attackRecovery = 0.12f;
    [SerializeField] private string[] attackStateNames =
    {
        "langzhu attack1",
        "langzhu attack2",
        "langzhu attack3"
    };
    [SerializeField] private int[] attackDamages = { 16, 18, 24 };
    [SerializeField] private float[] attackStartDistances = { 2.6f, 2.8f, 3f };
    [SerializeField, Min(0.1f)] private float attackVerticalTolerance = 1.8f;
    [SerializeField] private bool useAnimationEventAttackHits = true;

    [Header("Attack Hitboxes")]
    [SerializeField] private BoxCollider2D[] attack1Hitboxes;
    [SerializeField] private BoxCollider2D[] attack2Hitboxes;
    [SerializeField] private BoxCollider2D[] attack3Hitboxes;

    [Header("Legacy Timed Attack Fallback")]
    [SerializeField, Range(0f, 1f)] private float[] attackHitNormalizedTimes = { 0.45f, 0.5f, 0.58f };
    [SerializeField] private Vector2[] attackBoxOffsets =
    {
        new Vector2(1.2f, 0.35f),
        new Vector2(1.35f, 0.35f),
        new Vector2(1.55f, 0.35f)
    };
    [SerializeField] private Vector2[] attackBoxSizes =
    {
        new Vector2(2.1f, 1.2f),
        new Vector2(2.35f, 1.25f),
        new Vector2(2.65f, 1.35f)
    };

    [Header("Hit Reaction / Armor")]
    [SerializeField, Min(1)] private int hitReactionsBeforeAdvancedArmor = 3;
    [SerializeField, Min(0f)] private float hitReactionDuration = 0.22f;
    [SerializeField, Min(0f)] private float advancedSuperArmorDuration = 3f;

    [Header("Animation State Names")]
    [SerializeField] private string idleStateName = "idle";
    [SerializeField] private string moveStateName = "move";
    [SerializeField] private string hitStateName = "langzhu hit";
    [SerializeField] private string deathStateName = "langzhu death";
    [SerializeField, Min(0.1f)] private float fallbackAnimationLength = 0.75f;

    [Header("Body Alignment")]
    [SerializeField] private bool defaultVisualFacesLeft = true;

    private WolfState currentState = WolfState.Idle;
    private Transform player;
    private Coroutine actionRoutine;
    private Coroutine advancedArmorRoutine;
    private ContactFilter2D playerContactFilter;
    private readonly Collider2D[] overlapHits = new Collider2D[12];
    private float lastAttackTime = -999f;
    private float lastDashTime = -999f;
    private int comboCounter;
    private int currentAttackIndex = -1;
    private int currentAttackHitboxIndex;
    private int hitReactionCount;
    private bool currentAttackMissingHitboxWarningLogged;
    private bool preciseDodgeTimeStopped;
    private bool advancedSuperArmor;
    private bool deathEventRaised;
    private bool introPresentationLocked;
    private bool bossCombatStarted;

    public event System.Action<Enemy_ChaosWolfLord> OnDeathStarted;

    public bool IsImmuneToGenericEnemyControl => true;
    public bool IsInAdvancedSuperArmor => advancedSuperArmor;
    public bool IsDead => currentState == WolfState.Dead;
    public bool IsIntroPresentationLocked => introPresentationLocked;
    public bool IsIntroUnavailable => IsDead;
    public bool HasBossCombatStarted => bossCombatStarted;

    protected override void Awake()
    {
        EnsureRequiredComponents();
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

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

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (moveSpeed <= 0f)
            moveSpeed = chaseMoveSpeed;

        defaultMoveSpeed = chaseMoveSpeed;
        PlayState(idleStateName);
    }

    protected override void Update()
    {
        if (currentState == WolfState.Dead)
            return;

        if (introPresentationLocked)
        {
            StopBodyMotion();
            PlayStateIfNotCurrent(idleStateName);
            return;
        }

        ResolvePlayer();

        if (preciseDodgeTimeStopped)
        {
            StopBodyMotion();
            return;
        }

        if (currentState == WolfState.Attack || currentState == WolfState.Hit || currentState == WolfState.Dash)
            return;

        if (!bossCombatStarted || IsLockedPlayerUnavailable())
        {
            ChangeState(WolfState.Idle);
            StopBodyMotion();
            PlayStateIfNotCurrent(idleStateName);
            return;
        }

        FacePlayer();

        if (IsPlayerInAttackRange() && Time.time >= lastAttackTime + attackCooldown)
        {
            StartAttack();
            return;
        }

        if (IsPlayerInAttackRange())
        {
            StopBodyMotion();
            PlayStateIfNotCurrent(idleStateName);
            return;
        }

        if (CanStartDashChase())
        {
            StartDashChase();
            return;
        }

        ChangeState(WolfState.Chase);
        ChasePlayer();
    }

    public void BeginBossCombat(Player targetPlayer)
    {
        if (currentState == WolfState.Dead)
            return;

        bossCombatStarted = true;

        if (targetPlayer != null)
            player = targetPlayer.transform;
        else
            ResolvePlayer();

        lastAttackTime = Time.time;
        lastDashTime = Time.time;
        ChangeState(WolfState.Idle);
    }

    private void StartAttack()
    {
        StopActionRoutine();
        int attackIndex = comboCounter;
        comboCounter = (comboCounter + 1) % 3;
        actionRoutine = StartCoroutine(AttackRoutine(attackIndex));
    }

    public void SetIntroPresentationLocked(bool locked)
    {
        introPresentationLocked = locked;

        if (currentState == WolfState.Dead)
            return;

        StopActionRoutine();
        StopBodyMotion();
        ChangeState(WolfState.Idle);

        if (locked && anim != null)
            anim.speed = 1f;

        if (!locked)
            lastAttackTime = Time.time;

        PlayState(idleStateName);
    }

    public void ForceIntroIdle()
    {
        if (currentState == WolfState.Dead)
            return;

        StopBodyMotion();
        PlayStateIfNotCurrent(idleStateName);
    }

    public Vector3 GetIntroFocusPosition()
    {
        Collider2D bodyCollider = GetComponent<Collider2D>();

        if (bodyCollider != null && bodyCollider.enabled)
            return bodyCollider.bounds.center;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null)
            return spriteRenderer.bounds.center;

        return transform.position;
    }

    private IEnumerator AttackRoutine(int attackIndex)
    {
        ChangeState(WolfState.Attack);
        lastAttackTime = Time.time;
        StopBodyMotion();
        FacePlayer();

        string stateName = GetAttackStateName(attackIndex);
        float length = PlayStateAndGetLength(stateName);

        currentAttackIndex = attackIndex;
        currentAttackHitboxIndex = 0;
        currentAttackMissingHitboxWarningLogged = false;

        if (useAnimationEventAttackHits)
        {
            yield return WaitWhileNotTimeStopped(length);
        }
        else
        {
            float hitTime = Mathf.Clamp01(GetAttackHitTime(attackIndex)) * length;

            yield return WaitWhileNotTimeStopped(hitTime);
            TriggerAttackHit(attackIndex, 0, true);
            yield return WaitWhileNotTimeStopped(Mathf.Max(0f, length - hitTime));
        }

        if (attackRecovery > 0f)
            yield return WaitWhileNotTimeStopped(attackRecovery);

        currentAttackIndex = -1;
        currentAttackHitboxIndex = 0;
        currentAttackMissingHitboxWarningLogged = false;
        ChangeState(WolfState.Idle);
        actionRoutine = null;
    }

    public void AnimationEvent_AttackHit()
    {
        TriggerNextAttackHitFromAnimationEvent(currentAttackIndex);
    }

    public void AnimationEvent_Attack1Hit()
    {
        TriggerNextAttackHitFromAnimationEvent(0);
    }

    public void AnimationEvent_Attack2Hit()
    {
        TriggerNextAttackHitFromAnimationEvent(1);
    }

    public void AnimationEvent_Attack3Hit()
    {
        TriggerNextAttackHitFromAnimationEvent(2);
    }

    public void AnimationEvent_AttackHitByNumber(int attackNumber)
    {
        TriggerNextAttackHitFromAnimationEvent(Mathf.Clamp(attackNumber - 1, 0, 2));
    }

    private void ChasePlayer()
    {
        if (player == null || rb == null)
            return;

        float directionX = Mathf.Sign(player.position.x - transform.position.x);
        SetVelocity(directionX * chaseMoveSpeed, rb.velocity.y);
        PlayStateIfNotCurrent(moveStateName);
    }

    private bool CanStartDashChase()
    {
        if (!enableDashChase || player == null || rb == null)
            return false;

        if (Time.time < lastDashTime + dashCooldown)
            return false;

        float distanceToPlayer = GetHorizontalDistanceToPlayer();
        return distanceToPlayer >= dashMinDistance && distanceToPlayer > GetNextAttackStartDistance();
    }

    private void StartDashChase()
    {
        StopActionRoutine();
        actionRoutine = StartCoroutine(DashChaseRoutine());
    }

    private IEnumerator DashChaseRoutine()
    {
        ChangeState(WolfState.Dash);
        lastDashTime = Time.time;
        FacePlayer();

        StopBodyMotion();

        if (dashWindupTime > 0f)
            yield return WaitWhileNotTimeStopped(dashWindupTime);

        if (currentState == WolfState.Dead)
            yield break;

        FacePlayer();

        float directionX = player != null
            ? Mathf.Sign(player.position.x - transform.position.x)
            : facingDir;

        if (Mathf.Abs(directionX) < 0.01f)
            directionX = facingDir;

        PlayStateIfNotCurrent(moveStateName);

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            if (currentState == WolfState.Dead)
                yield break;

            if (preciseDodgeTimeStopped)
            {
                StopBodyMotion();
                yield return null;
                continue;
            }

            if (player != null && Vector2.Distance(transform.position, player.position) <= dashStopDistance)
                break;

            SetVelocity(directionX * dashSpeed, rb != null ? rb.velocity.y : 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        StopBodyMotion();

        if (dashRecovery > 0f)
            yield return WaitWhileNotTimeStopped(dashRecovery);

        ChangeState(WolfState.Idle);
        actionRoutine = null;
    }

    private void DealAttackDamage(int attackIndex, int hitboxIndex, bool allowLegacyFallback)
    {
        BoxCollider2D hitbox = GetAttackHitbox(attackIndex, hitboxIndex);

        if (hitbox == null || !hitbox.enabled || !hitbox.gameObject.activeInHierarchy)
        {
            WarnMissingHitbox(attackIndex, hitboxIndex);

            if (allowLegacyFallback)
                DealLegacyAttackDamage(attackIndex);

            return;
        }

        int hitCount = hitbox.OverlapCollider(playerContactFilter, overlapHits);

        for (int i = 0; i < hitCount; i++)
        {
            Player hitPlayer = overlapHits[i] != null ? overlapHits[i].GetComponentInParent<Player>() : null;

            if (TryDamagePlayer(hitPlayer, GetAttackDamage(attackIndex)))
                return;
        }
    }

    private void DealLegacyAttackDamage(int attackIndex)
    {
        Vector2 origin = GetAttackBoxCenter(attackIndex);
        Vector2 size = GetAttackBoxSize(attackIndex);
        int hitCount = Physics2D.OverlapBox(origin, size, 0f, playerContactFilter, overlapHits);

        for (int i = 0; i < hitCount; i++)
        {
            Player hitPlayer = overlapHits[i] != null ? overlapHits[i].GetComponentInParent<Player>() : null;

            if (TryDamagePlayer(hitPlayer, GetAttackDamage(attackIndex)))
                return;
        }
    }

    private void TriggerNextAttackHitFromAnimationEvent(int attackIndex)
    {
        if (!useAnimationEventAttackHits)
            return;

        if (currentState != WolfState.Attack || currentAttackIndex < 0)
            return;

        if (attackIndex != currentAttackIndex)
            return;

        int hitboxIndex = currentAttackHitboxIndex;
        currentAttackHitboxIndex++;
        TriggerAttackHit(attackIndex, hitboxIndex, false);
    }

    private void TriggerAttackHit(int attackIndex, int hitboxIndex, bool allowLegacyFallback)
    {
        DealAttackDamage(attackIndex, hitboxIndex, allowLegacyFallback);
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

    public override void DamageImpact()
    {
        if (currentState == WolfState.Dead || preciseDodgeTimeStopped)
            return;

        if (stats != null && stats.currentHealth <= 0)
            return;

        if (currentState == WolfState.Attack)
            return;

        if (advancedSuperArmor)
            return;

        hitReactionCount++;
        // Protect the final stagger from being restarted by rapid successive hits.
        if (hitReactionCount >= Mathf.Max(1, hitReactionsBeforeAdvancedArmor))
            advancedSuperArmor = true;
        StartHitReaction();
    }

    private void StartHitReaction()
    {
        StopActionRoutine();
        actionRoutine = StartCoroutine(HitReactionRoutine());
    }

    private IEnumerator HitReactionRoutine()
    {
        ChangeState(WolfState.Hit);
        StopBodyMotion();

        float length = PlayStateAndGetLength(hitStateName);
        yield return WaitWhileNotTimeStopped(Mathf.Max(hitReactionDuration, length));

        if (hitReactionCount >= Mathf.Max(1, hitReactionsBeforeAdvancedArmor))
            BeginAdvancedSuperArmor();

        ChangeState(WolfState.Idle);
        actionRoutine = null;
    }

    private void BeginAdvancedSuperArmor()
    {
        advancedSuperArmor = true;
        hitReactionCount = 0;

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

    public override void Die()
    {
        if (currentState == WolfState.Dead)
            return;

        ChangeState(WolfState.Dead);
        StopActionRoutine();
        advancedSuperArmor = false;
        hitReactionCount = 0;

        if (advancedArmorRoutine != null)
        {
            StopCoroutine(advancedArmorRoutine);
            advancedArmorRoutine = null;
        }

        StopBodyMotion();
        FreezeBodyAtDeathPose();
        DisableBodyCollision(false);
        PlayState(deathStateName);

        if (!deathEventRaised)
        {
            deathEventRaised = true;
            OnDeathStarted?.Invoke(this);
        }

        BossIntroSequenceDirector introDirector = GetComponent<BossIntroSequenceDirector>();

        if (introDirector != null)
            introDirector.ForceBossDefeatedCleanup();
    }

    public void ScheduleDestroyAfterCutscene()
    {
    }

    public override void SlowEntityBy(float _slowPercentage, float slowDuration)
    {
    }

    public override void FreezeTime(bool _timeFrozen)
    {
        if (IsImmuneToGenericEnemyControl)
            return;

        SetPreciseDodgeTimeStop(_timeFrozen);
    }

    public override void FreezeTimeFor(float _duration)
    {
    }

    public void SetPreciseDodgeTimeStop(bool timeStopped)
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

    private void ChangeState(WolfState newState)
    {
        currentState = newState;
    }

    private void StopActionRoutine()
    {
        if (actionRoutine == null)
            return;

        StopCoroutine(actionRoutine);
        actionRoutine = null;
        currentAttackIndex = -1;
        currentAttackHitboxIndex = 0;
        currentAttackMissingHitboxWarningLogged = false;
    }

    private bool CanSeePlayer(float radius)
    {
        if (!CanDetectPlayer())
            return false;

        ResolvePlayer();

        if (player == null)
            return false;

        return Vector2.Distance(transform.position, player.position) <= radius;
    }

    private bool IsPlayerInAttackRange()
    {
        if (player == null)
            return false;

        return GetHorizontalDistanceToPlayer() <= GetNextAttackStartDistance() &&
               GetVerticalDistanceToPlayer() <= attackVerticalTolerance;
    }

    private float GetHorizontalDistanceToPlayer()
    {
        if (player == null)
            return 999f;

        return Mathf.Abs(player.position.x - transform.position.x);
    }

    private float GetVerticalDistanceToPlayer()
    {
        if (player == null)
            return 999f;

        return Mathf.Abs(player.position.y - transform.position.y);
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

    private void FacePlayer()
    {
        if (player == null)
            return;

        float directionX = player.position.x - transform.position.x;

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
    }

    private void PlayStateIfNotCurrent(string stateName)
    {
        if (anim == null || string.IsNullOrWhiteSpace(stateName))
            return;

        if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(stateName))
            return;

        PlayState(stateName);
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

            if (clip != null && NormalizeAnimationName(clip.name) == normalizedTarget)
                return Mathf.Max(0.05f, clip.length);
        }

        return fallbackAnimationLength;
    }

    private string NormalizeAnimationName(string animationName)
    {
        return string.IsNullOrWhiteSpace(animationName)
            ? string.Empty
            : animationName.Replace("_", string.Empty).Replace("-", string.Empty).Replace("/", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
    }

    private string GetAttackStateName(int index)
    {
        if (attackStateNames == null || attackStateNames.Length == 0)
            return "langzhu attack1";

        return attackStateNames[Mathf.Clamp(index, 0, attackStateNames.Length - 1)];
    }

    private int GetAttackDamage(int index)
    {
        if (attackDamages == null || attackDamages.Length == 0)
            return 1;

        return Mathf.Max(1, attackDamages[Mathf.Clamp(index, 0, attackDamages.Length - 1)]);
    }

    private float GetNextAttackStartDistance()
    {
        return GetAttackStartDistance(comboCounter);
    }

    private float GetAttackStartDistance(int index)
    {
        if (attackStartDistances == null || attackStartDistances.Length == 0)
            return preferredAttackDistance;

        return Mathf.Max(0.1f, attackStartDistances[Mathf.Clamp(index, 0, attackStartDistances.Length - 1)]);
    }

    private float GetAttackHitTime(int index)
    {
        if (attackHitNormalizedTimes == null || attackHitNormalizedTimes.Length == 0)
            return 0.5f;

        return attackHitNormalizedTimes[Mathf.Clamp(index, 0, attackHitNormalizedTimes.Length - 1)];
    }

    private Vector2 GetAttackBoxCenter(int index)
    {
        Vector2 offset = GetAttackBoxOffset(index);
        Vector2 basePosition = attackCheak != null ? (Vector2)attackCheak.position : (Vector2)transform.position;
        return basePosition + Vector2.right * GetAttackFacingDirection() * Mathf.Abs(offset.x) + Vector2.up * offset.y;
    }

    private Vector2 GetAttackBoxOffset(int index)
    {
        if (attackBoxOffsets == null || attackBoxOffsets.Length == 0)
            return new Vector2(1.2f, 0.35f);

        return attackBoxOffsets[Mathf.Clamp(index, 0, attackBoxOffsets.Length - 1)];
    }

    private Vector2 GetAttackBoxSize(int index)
    {
        if (attackBoxSizes == null || attackBoxSizes.Length == 0)
            return new Vector2(2f, 1.2f);

        Vector2 size = attackBoxSizes[Mathf.Clamp(index, 0, attackBoxSizes.Length - 1)];
        return new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
    }

    private BoxCollider2D GetAttackHitbox(int attackIndex, int hitboxIndex)
    {
        if (hitboxIndex < 0)
            return null;

        BoxCollider2D[] hitboxes = GetAttackHitboxes(attackIndex);

        if (hitboxes == null || hitboxIndex >= hitboxes.Length)
            return null;

        return hitboxes[hitboxIndex];
    }

    private BoxCollider2D[] GetAttackHitboxes(int attackIndex)
    {
        switch (attackIndex)
        {
            case 0:
                return attack1Hitboxes;
            case 1:
                return attack2Hitboxes;
            case 2:
                return attack3Hitboxes;
            default:
                return null;
        }
    }

    private void WarnMissingHitbox(int attackIndex, int hitboxIndex)
    {
        if (currentAttackMissingHitboxWarningLogged)
            return;

        currentAttackMissingHitboxWarningLogged = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning(
            $"ChaosWolfLord attack{attackIndex + 1} hit event #{hitboxIndex + 1} has no configured hitbox.",
            this);
#endif
    }

    private int GetAttackFacingDirection()
    {
        if (Application.isPlaying)
            return facingDir;

        bool flippedByRotation = Mathf.Abs(Mathf.DeltaAngle(0f, transform.eulerAngles.y)) > 90f;
        bool flippedByScale = transform.lossyScale.x < 0f;
        bool visualFlipped = flippedByRotation ^ flippedByScale;
        bool visualFacesRight = defaultVisualFacesLeft ? visualFlipped : !visualFlipped;
        return visualFacesRight ? 1 : -1;
    }

    private void StopBodyMotion()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;
    }

    private void FreezeBodyAtDeathPose()
    {
        if (rb == null)
            return;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
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

    private void EnsureRequiredComponents()
    {
        if (!TryGetComponent<Rigidbody2D>(out Rigidbody2D body))
            body = gameObject.AddComponent<Rigidbody2D>();

        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (!TryGetComponent<CapsuleCollider2D>(out CapsuleCollider2D capsuleCollider))
            capsuleCollider = gameObject.AddComponent<CapsuleCollider2D>();

        capsuleCollider.isTrigger = false;

        if (!TryGetComponent<ChaosWolfLordStats>(out _))
            gameObject.AddComponent<ChaosWolfLordStats>();

        if (!TryGetComponent<EntityFX>(out _))
            gameObject.AddComponent<EntityFX>();
    }

    private void EnsureCheckPoints()
    {
        if (attackCheak == null)
            attackCheak = EnsureChildPoint("AttackCheck", new Vector3(-0.35f, 0.35f, 0f));

        if (groundCheak == null)
            groundCheak = EnsureChildPoint("GroundCheck", new Vector3(0f, -1.55f, 0f));

        if (wallCheak == null)
            wallCheak = EnsureChildPoint("WallCheck", new Vector3(-1.25f, -0.15f, 0f));

        if (attackCheakRidus <= 0f)
            attackCheakRidus = 1.05f;

        if (groundCheakDistance <= 0f)
            groundCheakDistance = 1f;

        if (wallCheakDistance <= 0f)
            wallCheakDistance = 1f;
    }

    private void EnsureAttackHitboxes()
    {
        Transform container = EnsureHitboxContainer();

        attack1Hitboxes = EnsureHitboxArray(
            container,
            attack1Hitboxes,
            new[] { "Attack1_Hit1", "Attack1_Hit2" },
            new[] { new Vector2(-1.3f, 0.45f), new Vector2(-1.8f, 0.35f) },
            new[] { new Vector2(2.6f, 1.3f), new Vector2(3f, 1.4f) });

        attack2Hitboxes = EnsureHitboxArray(
            container,
            attack2Hitboxes,
            new[] { "Attack2_Hit1", "Attack2_Hit2" },
            new[] { new Vector2(-1.4f, 0.55f), new Vector2(-2f, 0.45f) },
            new[] { new Vector2(2.8f, 1.5f), new Vector2(3.4f, 1.6f) });

        attack3Hitboxes = EnsureHitboxArray(
            container,
            attack3Hitboxes,
            new[] { "Attack3_Hit1" },
            new[] { new Vector2(-1.9f, 0.55f) },
            new[] { new Vector2(3.6f, 1.8f) });
    }

    private Transform EnsureHitboxContainer()
    {
        Transform container = transform.Find("AttackHitboxes");

        if (container != null)
            return container;

        GameObject containerObject = new GameObject("AttackHitboxes");
        container = containerObject.transform;
        container.SetParent(transform, false);
        container.localPosition = Vector3.zero;
        container.localRotation = Quaternion.identity;
        container.localScale = Vector3.one;
        return container;
    }

    private BoxCollider2D[] EnsureHitboxArray(
        Transform container,
        BoxCollider2D[] configuredHitboxes,
        string[] hitboxNames,
        Vector2[] defaultPositions,
        Vector2[] defaultSizes)
    {
        BoxCollider2D[] result = configuredHitboxes;

        if (result == null || result.Length != hitboxNames.Length)
            result = new BoxCollider2D[hitboxNames.Length];

        for (int i = 0; i < hitboxNames.Length; i++)
        {
            if (result[i] == null)
                result[i] = EnsureHitbox(container, hitboxNames[i], defaultPositions[i], defaultSizes[i]);
            else
                ConfigureHitbox(result[i]);
        }

        return result;
    }

    private BoxCollider2D EnsureHitbox(Transform container, string hitboxName, Vector2 defaultPosition, Vector2 defaultSize)
    {
        Transform hitboxTransform = container.Find(hitboxName);
        bool createdTransform = false;

        if (hitboxTransform == null)
        {
            GameObject hitboxObject = new GameObject(hitboxName);
            hitboxTransform = hitboxObject.transform;
            hitboxTransform.SetParent(container, false);
            hitboxTransform.localPosition = defaultPosition;
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
            hitbox.size = defaultSize;
        }

        ConfigureHitbox(hitbox);
        return hitbox;
    }

    private void ConfigureHitbox(BoxCollider2D hitbox)
    {
        if (hitbox == null)
            return;

        hitbox.isTrigger = true;
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
        Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.35f, 0.15f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);

        Gizmos.color = new Color(1f, 0.1f, 0.05f, 0.55f);
        Gizmos.DrawWireSphere(transform.position, preferredAttackDistance);

        Gizmos.color = new Color(0.25f, 0.65f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, dashMinDistance);

        for (int i = 0; i < 3; i++)
        {
            Gizmos.color = new Color(1f, 0.05f * (i + 1), 0.05f, 0.35f);
            DrawAttackHitboxGizmos(GetAttackHitboxes(i));
        }
    }

    private void DrawAttackHitboxGizmos(BoxCollider2D[] hitboxes)
    {
        if (hitboxes == null)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;

        for (int i = 0; i < hitboxes.Length; i++)
        {
            BoxCollider2D hitbox = hitboxes[i];

            if (hitbox == null)
                continue;

            Gizmos.matrix = hitbox.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(hitbox.offset, hitbox.size);
        }

        Gizmos.matrix = previousMatrix;
    }
}
