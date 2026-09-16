using System.Collections;
using UnityEngine;


public class Player : Entity
{

    [Header("攻击详情")]
    public Vector2[] attackMovement;
    public float counterAttackDuration = 0.2f;

    public bool isBusy { get;  set; }
    public bool IsInvisible { get; private set; }


    [Header("移动信息")]
    public float moveSpeed = 12f;
    public float jumpForce;
    public float swordReturnImpact;
    private float defaultMoveSpeed;
    private float defaultJumpForce;

    [Header("二段跳信息")]
    public float doubleJumpForce;
    [SerializeField] private int extraJumpCount = 1;
    private int availableExtraJumps;
    private bool canJumpAttack = true;

    [Header("冲刺信息")]
    public float dashSpeed;
    public float dashDuration;
    private float defaultDashSpeed;
    public float dashDir { get; private set; }
    private Coroutine queuedDashCoroutine;
    private float queuedDashDir;
    private int lastPreciseDodgeFrame = -1000;
    private float lastShiftPressedTime = -999f;

    public SkillManager skill { get; private set; }
    public PlayerInteraction interaction { get; private set; }
    public GameObject sword;
    public bool canHeal;
    [SerializeField, Min(0.1f)] private float deathUiFallbackDelay = 1.1f;

    [Header("开局苏醒")]
    [SerializeField] private string awakingAnimationStateName = "playerAwaking";
    [SerializeField, Min(0.1f)] private float awakingFallbackDuration = 2.5f;
    [SerializeField] private bool useAwakingCameraZoom = false;
    [SerializeField, Min(0.1f)] private float awakingFocusOrthographicSize = 4.2f;
    [SerializeField, Min(0f)] private float awakingCameraRestoreDuration = 0.8f;
    [SerializeField, Min(0f)] private float awakingEyeFirstOpenDuration = 0.45f;
    [SerializeField, Min(0f)] private float awakingEyeCloseDuration = 0.12f;
    [SerializeField, Min(0f)] private float awakingEyeFinalOpenDuration = 0.75f;
    [SerializeField] private bool freezeGravityDuringAwaking = true;

    [Header("楼梯")]
    [SerializeField] private bool enableStairAssist = true;
    [SerializeField, Range(0.2f, 1.2f)] private float stairCheckWidthMultiplier = 0.75f;
    [SerializeField, Min(0.02f)] private float stairCheckHeight = 0.28f;
    [SerializeField, Min(0f)] private float stairCheckExtraDistance = 0.45f;
    [SerializeField, Min(0f)] private float stairVerticalSpeedRatio = 0.42f;
    [SerializeField, Min(0.1f)] private float stairMaxVerticalSpeed = 4.8f;
    [SerializeField, Min(0f)] private float stairGroundMemoryTime = 0.12f;

    private Coroutine deathAnimationFallbackCoroutine;
    private bool deathAnimationFinishedNotified;
    public event System.Action OnDeathAnimationFinished;
    private float defaultGravityScale = 1f;
    private bool pendingOpeningAwakingSequence;
    private bool isOpeningAwakingSequenceActive;
    private Collider2D currentStairCollider;
    private StairSurface currentStairSurface;
    private float lastStairGroundedTime = -999f;
    private Coroutine hazardImpactCoroutine;
    private bool isHazardControlLocked;
    private bool isCutsceneControlLocked;
    private bool wasInteractionEnabledBeforeCutscene = true;
    private bool wasBusyBeforeHazardImpact;
    private float lastHazardContactTime = -999f;

    [Header("SFX 音效 Sound Effects")]
    public AudioClip jumpClip;     // 音效用：普通跳跃音效，角色从地面起跳时播放一次
    public AudioClip airJumpClip;  // 音效用：二段跳音效，角色在空中二段跳时播放一次
    public AudioClip attackClip;   // 音效用：攻击音效，角色触发攻击动画时播放一次
    public AudioClip hurtClip;     // 音效用：受伤音效，角色进入受伤状态时播放一次
    public AudioClip deathClip;    // 音效用：死亡音效，角色进入死亡状态时播放一次

    [Header("脚步音效 Footstep SFX")]
    public AudioSource footstepSource; // 音效用：脚步声专用 AudioSource，用来循环播放脚步声，不走 PlaySFX 一次性播放
    private AudioEventPlayer audioEvents;
    //状态机设置
    #region States
    public PlayerStateMachine stateMachine { get; private set; }
    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerDoubleJumpState doubleJumpState { get; private set; }
    public PlayerAirState airState { get; private set; }
    public PlayerWallSlideState wallSlide { get; private set; }
    public PlayerWallJumpState wallJump { get; private set; }
    public PlayerDashState dashState { get; private set; }
    public PlayerPrimaryAttackState primaryAttack { get; private set; }
    public PlayerJumpAttackState jumpAttackState { get; private set; }
    public PlayerCounterAttackState counterAttack { get; private set; }
    public PlayerAimSwordState aimSword { get; private set; }
    public PlayerCatchSwordState catchSword { get; private set; }
    public PlayerPreciseDodgeState preciseDodgeState { get; private set; }
    public PlayerPreciseDodgeAttackState preciseDodgeAttackState { get; private set; }
    public PlayerSunSkillState sunSkillState { get; private set; }
    public PlayerDeadState deadState { get; private set; }
    public PlayerHealingState healState { get; private set; }
    public PlayerAwakingState awakingState { get; private set; }
    #endregion

    public bool IsOpeningAwakingSequenceActive => isOpeningAwakingSequenceActive;
    public event System.Action<bool> OnOpeningAwakingSequenceActiveChanged;

    protected override void Awake()
    {
        base.Awake();
        EnsureInteractionComponent();

        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        doubleJumpState = new PlayerDoubleJumpState(this, stateMachine, "DoubleJump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
        dashState = new PlayerDashState(this, stateMachine, "Dash");
        wallSlide = new PlayerWallSlideState(this, stateMachine, "WallSlide");
        wallJump = new PlayerWallJumpState(this, stateMachine, "Jump");

        primaryAttack = new PlayerPrimaryAttackState(this, stateMachine, "Attack");
        jumpAttackState = new PlayerJumpAttackState(this, stateMachine, "JumpAttack");
        counterAttack = new PlayerCounterAttackState(this, stateMachine, "CounterAttack");

        aimSword = new PlayerAimSwordState(this, stateMachine, "AimSword");
        catchSword = new PlayerCatchSwordState(this, stateMachine, "CatchSword");
        preciseDodgeState = new PlayerPreciseDodgeState(this, stateMachine, "PreciseDodge");
        preciseDodgeAttackState = new PlayerPreciseDodgeAttackState(this, stateMachine, "PreciseDodgeAttack");
        sunSkillState = new PlayerSunSkillState(this, stateMachine, "SunSkill");
        deadState = new PlayerDeadState(this, stateMachine, "Die");
        healState = new PlayerHealingState(this, stateMachine, "Healing");
        awakingState = new PlayerAwakingState(this, stateMachine, awakingAnimationStateName);
    }

    private void EnsureInteractionComponent()
    {
        if (!TryGetComponent<PlayerInteraction>(out PlayerInteraction playerInteraction))
            playerInteraction = gameObject.AddComponent<PlayerInteraction>();

        interaction = playerInteraction;
    }

    protected override void Start()
    {

        base.Start();
        skill = SkillManager.instance;
        ResolveAudioEvents();
        ResetExtraJumps();

        defaultMoveSpeed = moveSpeed;
        defaultJumpForce = jumpForce;
        defaultDashSpeed = dashSpeed;
        defaultGravityScale = rb != null ? rb.gravityScale : 1f;

        pendingOpeningAwakingSequence = PlayerOpeningAwakingRequest.ConsumeIfMatchesActiveScene();
        PlayerState initialState = pendingOpeningAwakingSequence ? awakingState : idleState;

        stateMachine.Initialize(initialState);

        // 音效用：初始化脚步声 AudioSource
        // 脚步声是“走路时持续播放、停下就停止”的循环音效，所以用独立 AudioSource
        if (footstepSource != null)
        {
            footstepSource.loop = true;          // 脚步声需要循环播放
            footstepSource.playOnAwake = false; // 防止游戏一开始脚步声就自动播放
            footstepSource.Stop();              // 确保初始状态脚步声是停止的
        }
    }



    protected override void Update()
    {
        base.Update();
        RefreshStairState();

        if (isHazardControlLocked)
        {
            if (anim != null && rb != null)
                anim.SetFloat("yVelocity", rb.velocity.y);

            return;
        }

        if (isCutsceneControlLocked)
        {
            if (rb != null)
                rb.velocity = new Vector2(0f, rb.velocity.y);

            if (anim != null && rb != null)
                anim.SetFloat("yVelocity", rb.velocity.y);

            return;
        }

        if (IsDeadOrDying())
        {
            if (stateMachine != null && stateMachine.currentState != null)
            {
                if (stateMachine.currentState != deadState && deadState != null)
                    stateMachine.ChangeState(deadState);

                stateMachine.currentState.Update();
            }

            return;
        }

        if (TryStartStairJumpFromInput())
            return;

        stateMachine.currentState.Update();

        if (stateMachine.currentState == awakingState)
            return;

        RecordShiftInput();

        CheckForPreciseDodgeFollowUpInput();

        CheckForAwakeningInput();
        CheckForInvisibilityInput();

        CheakForDashInput();
    }

    private void FixedUpdate()
    {
        if (isHazardControlLocked)
        {
            RestoreDefaultGravityScale();
            return;
        }

        if (isCutsceneControlLocked)
        {
            RestoreDefaultGravityScale();
            return;
        }

        ApplyStairAssistPhysics();
    }

    private bool TryStartStairJumpFromInput()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || !IsOnStair())
            return false;

        if (stateMachine == null)
            return false;

        PlayerState currentState = stateMachine.currentState;

        if (currentState != idleState &&
            currentState != moveState &&
            currentState != airState)
        {
            return false;
        }

        RestoreDefaultGravityScale();
        stateMachine.ChangeState(jumpState);
        return true;
    }
    public override void SlowEntityBy(float _slowPercentage, float slowDuration)
    {
        moveSpeed = moveSpeed * (1- _slowPercentage);
        jumpForce = jumpForce * (1- _slowPercentage);
        dashSpeed = dashSpeed * (1- _slowPercentage);
        anim.speed = anim.speed * (1- _slowPercentage);

        Invoke("ReturnDefaultsSpeed", _slowPercentage); 


    }
    public void AssignNewSword(GameObject _newSword)
    {
        sword = _newSword;
    }

    protected override void ReturnDefaultSpeed()
    {
        base.ReturnDefaultSpeed();

        moveSpeed = defaultMoveSpeed;
        jumpForce = defaultJumpForce;
        dashSpeed = defaultDashSpeed;
    }

    public void CatchTheSword()
    {
        stateMachine.ChangeState(catchSword);
        Destroy(sword);
    }

    public void ResetExtraJumps()
    {
        availableExtraJumps = extraJumpCount;
    }

    public bool CanDoubleJump() => availableExtraJumps > 0 && IsSkillUnlocked(SkillManager.SkillIds.DashDoubleJump);
    public bool IsSkillUnlocked(string skillId)
    {
        SkillManager skillManager = GetSkillManager();
        return skillManager != null && skillManager.IsSkillUnlocked(skillId);
    }

    public void ConsumeExtraJump()
    {
        availableExtraJumps = Mathf.Max(availableExtraJumps - 1, 0);
    }

    public float GetDoubleJumpForce()
    {
        return doubleJumpForce > 0 ? doubleJumpForce : jumpForce;
    }

    public void ResetJumpAttack()
    {
        canJumpAttack = true;
    }

    public void ConsumeJumpAttack()
    {
        canJumpAttack = false;
    }

    public override bool IsGroundDetected()
    {
        return base.IsGroundDetected() || IsOnStair();
    }

    public bool IsOnStair()
    {
        if (!enableStairAssist)
            return false;

        RefreshStairState();
        return HasStairGrounding();
    }

    public void SetGroundMoveVelocity(float xVelocity)
    {
        RefreshStairState();

        if (currentStairCollider == null)
        {
            SetVelocity(xVelocity, rb.velocity.y);
            return;
        }

        float yVelocity = CalculateStairVerticalVelocity(xVelocity);
        SetVelocity(xVelocity, yVelocity);
    }

    private void RefreshStairState()
    {
        currentStairCollider = null;
        currentStairSurface = null;

        if (!enableStairAssist || cd == null)
            return;

        Bounds bounds = cd.bounds;
        Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y + stairCheckHeight * 0.5f);
        Vector2 boxSize = new Vector2(bounds.size.x * stairCheckWidthMultiplier, stairCheckHeight);
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, whatIsGround);

        for (int i = 0; i < hits.Length; i++)
        {
            if (TryUseStairCollider(hits[i]))
                return;
        }

        float rayDistance = groundCheakDistance + stairCheckExtraDistance;
        float halfWidth = boxSize.x * 0.5f;
        Vector2 rayOrigin = new Vector2(bounds.center.x, bounds.min.y + stairCheckHeight);

        if (TryRaycastStair(rayOrigin, rayDistance))
            return;

        if (TryRaycastStair(rayOrigin + Vector2.left * halfWidth, rayDistance))
            return;

        TryRaycastStair(rayOrigin + Vector2.right * halfWidth, rayDistance);
    }

    private bool TryRaycastStair(Vector2 origin, float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, distance, whatIsGround);
        return hit.collider != null && TryUseStairCollider(hit.collider);
    }

    private bool TryUseStairCollider(Collider2D collider)
    {
        if (!IsStairCollider(collider))
            return false;

        currentStairCollider = collider;
        currentStairSurface = collider.GetComponent<StairSurface>();
        lastStairGroundedTime = Time.time;
        return true;
    }

    private bool IsStairCollider(Collider2D collider)
    {
        if (collider == null)
            return false;

        if (collider.GetComponent<StairSurface>() != null)
            return true;

        if (collider.name.StartsWith("Slope_"))
            return true;

        Transform current = collider.transform.parent;

        while (current != null)
        {
            if (current.name == "__GeneratedSlopeColliders")
                return true;

            current = current.parent;
        }

        return false;
    }

    private float CalculateStairVerticalVelocity(float xVelocity)
    {
        if (Mathf.Abs(xVelocity) < 0.01f)
            return 0f;

        int upDirection = GetCurrentStairUpDirection();
        float verticalVelocity = Mathf.Abs(xVelocity) * stairVerticalSpeedRatio * Mathf.Sign(xVelocity) * upDirection;
        return Mathf.Clamp(verticalVelocity, -stairMaxVerticalSpeed, stairMaxVerticalSpeed);
    }

    private int GetCurrentStairUpDirection()
    {
        if (currentStairSurface != null)
            return currentStairSurface.UpDirection;

        if (currentStairCollider != null && currentStairCollider.name.Contains("UpLeft"))
            return -1;

        return 1;
    }

    private void ApplyStairAssistPhysics()
    {
        if (stateMachine != null && stateMachine.currentState == awakingState)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;

                if (freezeGravityDuringAwaking)
                    rb.gravityScale = 0f;
            }

            return;
        }

        RefreshStairState();

        if (!ShouldApplyStairAssist())
        {
            ClearStairExitLift();
            RestoreDefaultGravityScale();
            return;
        }

        rb.gravityScale = 0f;

        if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.01f)
            rb.velocity = Vector2.zero;
    }

    private bool ShouldApplyStairAssist()
    {
        if (!enableStairAssist || currentStairCollider == null || rb == null || stateMachine == null)
            return false;

        PlayerState currentState = stateMachine.currentState;

        return currentState != jumpState &&
               currentState != doubleJumpState &&
               currentState != airState &&
               currentState != wallJump &&
               currentState != wallSlide &&
               currentState != dashState &&
               currentState != deadState;
    }

    private bool HasStairGrounding()
    {
        return currentStairCollider != null || Time.time <= lastStairGroundedTime + stairGroundMemoryTime;
    }

    private void ClearStairExitLift()
    {
        if (!enableStairAssist || rb == null || currentStairCollider != null)
            return;

        if (IsInActiveJumpState())
            return;

        if (Time.time > lastStairGroundedTime + stairGroundMemoryTime)
            return;

        if (rb.velocity.y > 0f)
            rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    private bool IsInActiveJumpState()
    {
        if (stateMachine == null)
            return false;

        PlayerState currentState = stateMachine.currentState;

        return currentState == jumpState ||
               currentState == doubleJumpState ||
               currentState == wallJump ||
               currentState == jumpAttackState;
    }

    private void RestoreDefaultGravityScale()
    {
        if (rb != null && !Mathf.Approximately(rb.gravityScale, defaultGravityScale))
            rb.gravityScale = defaultGravityScale;
    }

    public bool TryApplyHazardContact(
        HazardDamageType hazardType,
        int damage,
        Vector2 sourcePosition,
        float contactCooldown,
        float horizontalKnockback,
        float upwardKnockback,
        float controlLockDuration)
    {
        _ = hazardType;

        if (stats == null || stats.isDead)
            return false;

        if (damage <= 0)
            return false;

        if (Time.time < lastHazardContactTime + contactCooldown)
            return false;

        lastHazardContactTime = Time.time;
        stats.TakeDamage(damage);

        if (stats == null || stats.isDead)
            return true;

        ApplyHazardImpact(sourcePosition, horizontalKnockback, upwardKnockback, controlLockDuration);
        return true;
    }

    private void ApplyHazardImpact(Vector2 sourcePosition, float horizontalKnockback, float upwardKnockback, float controlLockDuration)
    {
        if (stateMachine == null || rb == null)
            return;

        if (stateMachine.currentState == deadState || stateMachine.currentState == awakingState)
            return;

        if (hazardImpactCoroutine != null)
        {
            StopCoroutine(hazardImpactCoroutine);
            isKnocked = false;
            isHazardControlLocked = false;
            isBusy = wasBusyBeforeHazardImpact;
        }

        RestoreDefaultGravityScale();

        if (stateMachine.currentState != airState)
            stateMachine.ChangeState(airState);

        float horizontalDirection = transform.position.x - sourcePosition.x;

        if (Mathf.Abs(horizontalDirection) < 0.05f)
            horizontalDirection = -facingDir;
        else
            horizontalDirection = Mathf.Sign(horizontalDirection);

        Vector2 impactVelocity = new Vector2(
            horizontalDirection * Mathf.Max(0f, horizontalKnockback),
            Mathf.Max(0f, upwardKnockback));

        hazardImpactCoroutine = StartCoroutine(HazardImpactRoutine(impactVelocity, controlLockDuration));
    }

    private IEnumerator HazardImpactRoutine(Vector2 impactVelocity, float controlLockDuration)
    {
        wasBusyBeforeHazardImpact = isBusy;
        isHazardControlLocked = true;
        isBusy = true;
        isKnocked = true;

        rb.velocity = impactVelocity;

        yield return new WaitForSeconds(Mathf.Max(0f, controlLockDuration));

        isKnocked = false;
        isHazardControlLocked = false;
        isBusy = wasBusyBeforeHazardImpact;
        hazardImpactCoroutine = null;

        if (stats != null && !stats.isDead && IsGroundDetected() && stateMachine.currentState == airState)
            stateMachine.ChangeState(idleState);
    }

    private void CancelHazardImpact(bool restoreBusyState)
    {
        if (hazardImpactCoroutine != null)
        {
            StopCoroutine(hazardImpactCoroutine);
            hazardImpactCoroutine = null;
        }

        isKnocked = false;
        isHazardControlLocked = false;

        if (restoreBusyState)
            isBusy = wasBusyBeforeHazardImpact;
    }


    public IEnumerator BusyFor(float _seconds)
    {
        isBusy = true;

        yield return new WaitForSeconds(_seconds);

        isBusy = false;
    }



    public void AnimationTrigger() => stateMachine.currentState.AnimationFinishTrigger();

    public bool PlayAudioCue(string cueName)
    {
        ResolveAudioEvents();

        if (audioEvents != null && audioEvents.PlayCue(cueName))
            return true;

        AudioClip fallbackClip = GetLegacyAudioClip(cueName);

        if (fallbackClip == null)
            return false;

        AudioManager.GetOrCreateInstance().PlaySFX(fallbackClip);
        return true;
    }

    public bool HasJumpAttackInput()
    {
        return canJumpAttack && Input.GetKeyDown(KeyCode.Mouse0) && !UI_InputBlocker.ShouldBlockPlayerMouseInput();
    }

    public bool CanStartCounterAttack()
    {
        return skill != null && skill.parry != null && skill.parry.CanAttemptParry();
    }

    public bool TryStartCounterAttack()
    {
        if (!CanStartCounterAttack())
            return false;

        stateMachine.ChangeState(counterAttack);
        return true;
    }

    public void NotifyCounterAttackSuccess()
    {
        NotifyCounterAttackSuccess(null);
    }

    public void NotifyCounterAttackSuccess(Enemy parriedEnemy)
    {
        PlayAudioCue("parry_success");
        skill?.parry?.HandleSuccessfulParry(parriedEnemy);
        skill?.parry?.StartCooldownOnSuccess();
    }

    private void ResolveAudioEvents()
    {
        if (audioEvents != null)
            return;

        audioEvents = GetComponent<AudioEventPlayer>();

        if (audioEvents == null)
            audioEvents = GetComponentInChildren<AudioEventPlayer>();
    }

    private AudioClip GetLegacyAudioClip(string cueName)
    {
        switch (cueName)
        {
            case "jump":
            case "wall_jump":
                return jumpClip;
            case "double_jump":
                return airJumpClip;
            case "attack":
            case "attack_1":
            case "attack_2":
            case "attack_3":
            case "jump_attack":
                return attackClip;
            case "hurt":
                return hurtClip;
            case "death":
                return deathClip;
            default:
                return null;
        }
    }

    public bool ShouldEnterWallSlide(float inputX)
    {
        return IsWallDetected() && !IsGroundDetected() && !IsMovingAwayFromWall(inputX);
    }

    public bool IsMovingAwayFromWall(float inputX)
    {
        return inputX != 0 && Mathf.Sign(inputX) != facingDir;
    }

    public string AwakingAnimationStateName => string.IsNullOrWhiteSpace(awakingAnimationStateName)
        ? "playerAwaking"
        : awakingAnimationStateName;

    private void SetOpeningAwakingSequenceActive(bool active)
    {
        if (isOpeningAwakingSequenceActive == active)
            return;

        isOpeningAwakingSequenceActive = active;
        OnOpeningAwakingSequenceActiveChanged?.Invoke(active);
    }

    public void SetOpeningAwakingActive(bool active)
    {
        isBusy = active;

        if (!active)
            SetOpeningAwakingSequenceActive(false);

        if (rb == null)
            return;

        rb.velocity = Vector2.zero;

        if (freezeGravityDuringAwaking)
            rb.gravityScale = active ? 0f : defaultGravityScale;
    }

    public void BeginCutsceneControlLock()
    {
        if (isCutsceneControlLocked)
            return;

        isCutsceneControlLocked = true;
        isBusy = true;

        if (queuedDashCoroutine != null)
        {
            StopCoroutine(queuedDashCoroutine);
            queuedDashCoroutine = null;
        }

        if (interaction != null)
        {
            wasInteractionEnabledBeforeCutscene = interaction.enabled;
            interaction.enabled = false;
        }

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (stateMachine != null &&
            stateMachine.currentState != null &&
            stateMachine.currentState != deadState &&
            stateMachine.currentState != awakingState &&
            idleState != null &&
            IsGroundDetected())
        {
            stateMachine.ChangeState(idleState);
        }
    }

    public void EndCutsceneControlLock()
    {
        if (!isCutsceneControlLocked)
            return;

        isCutsceneControlLocked = false;
        isBusy = false;

        if (interaction != null)
            interaction.enabled = wasInteractionEnabledBeforeCutscene;

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (stateMachine != null &&
            stateMachine.currentState != null &&
            stateMachine.currentState != deadState &&
            stateMachine.currentState != awakingState)
        {
            stateMachine.ChangeState(IsGroundDetected() ? idleState : airState);
        }
    }

    public void PlayRespawnAwaking()
    {
        if (stateMachine == null || awakingState == null)
            return;

        if (stateMachine.currentState == awakingState)
            return;

        pendingOpeningAwakingSequence = false;
        SetOpeningAwakingSequenceActive(false);
        stateMachine.ChangeState(awakingState);
    }

    public IEnumerator PlayOpeningAwakingSequence()
    {
        SetOpeningAwakingActive(true);
        bool showOpeningUi = pendingOpeningAwakingSequence;
        pendingOpeningAwakingSequence = false;
        SetOpeningAwakingSequenceActive(showOpeningUi);

        PlayerOpeningAwakingCameraFocus cameraFocus = new PlayerOpeningAwakingCameraFocus(
            transform,
            awakingFocusOrthographicSize,
            useAwakingCameraZoom);
        cameraFocus.ApplyImmediateFocus();

        float awakingDuration = GetAnimationClipLength(AwakingAnimationStateName);

        if (awakingDuration <= 0f)
            awakingDuration = awakingFallbackDuration;

        PlayAnimatorStateIfExists(AwakingAnimationStateName, 0f);

        UI_EyeBlinkTransition.Instance.SetClosed();
        UI_ScreenFadeTransition.Instance.SetAlpha(0f);

        yield return UI_EyeBlinkTransition.Instance.PlayOpeningBlink(
            awakingEyeFirstOpenDuration,
            awakingEyeCloseDuration,
            awakingEyeFinalOpenDuration);

        float blinkDuration = awakingEyeFirstOpenDuration + awakingEyeCloseDuration + awakingEyeFinalOpenDuration;
        float remainingAwakingDuration = Mathf.Max(0f, awakingDuration - blinkDuration);

        if (remainingAwakingDuration > 0f)
            yield return new WaitForSecondsRealtime(remainingAwakingDuration);

        yield return cameraFocus.Restore(awakingCameraRestoreDuration);

        SetOpeningAwakingSequenceActive(false);
        SetOpeningAwakingActive(false);
    }

    public bool PlayAnimatorStateIfExists(string stateName, float normalizedTime)
    {
        if (anim == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        int stateHash = Animator.StringToHash(stateName);

        if (!anim.HasState(0, stateHash))
            return false;

        anim.Play(stateHash, 0, normalizedTime);
        return true;
    }

    public void InterruptPreciseDodgeFollowUp()
    {
        if (stateMachine.currentState != preciseDodgeAttackState)
            return;

        skill?.preciseDodge?.CancelFollowUpAttack();

        if (IsGroundDetected())
            stateMachine.ChangeState(idleState);
        else
            stateMachine.ChangeState(airState);
    }

    public bool TryStartPreciseDodge(Transform attacker)
    {
        if (skill == null || skill.preciseDodge == null)
            return false;

        bool dodged = skill.preciseDodge.TryStartFromAttackFrame(attacker);

        if (dodged)
            MarkPreciseDodgeStarted();

        return dodged;
    }

    public void MarkPreciseDodgeStarted()
    {
        lastPreciseDodgeFrame = Time.frameCount;
    }

    public bool HasBufferedPreciseDodgeInput(float bufferDuration)
    {
        return ShiftWasPressedThisFrame() || Time.time <= lastShiftPressedTime + bufferDuration;
    }

    public void ConsumePreciseDodgeInput()
    {
        lastShiftPressedTime = -999f;
    }

    private void RecordShiftInput()
    {
        if (ShiftWasPressedThisFrame())
            lastShiftPressedTime = Time.time;
    }

    private void CheckForPreciseDodgeFollowUpInput()
    {
        if (stateMachine.currentState == preciseDodgeAttackState)
            return;

        if (Input.GetKeyDown(KeyCode.R))
            skill?.preciseDodge?.TryUseFollowUpAttack();
    }

    private void CheckForAwakeningInput()
    {
        if (Input.GetKeyDown(KeyCode.V) && IsSkillUnlocked(SkillManager.SkillIds.Awakening))
            skill?.awakening?.TryActivate();
    }

    private void CheckForInvisibilityInput()
    {
        if (Input.GetKeyDown(KeyCode.X) && IsSkillUnlocked(SkillManager.SkillIds.Invisibility))
            skill?.invisibility?.TryActivate();
    }

    public void SetInvisible(bool invisible)
    {
        IsInvisible = invisible;
    }

    private void CheakForDashInput()
    {


        if (IsWallDetected() && !IsGroundDetected())
            return;


        if (!ShiftWasPressedThisFrame())
            return;

        if (lastPreciseDodgeFrame == Time.frameCount)
            return;

        SkillManager skillManager = GetSkillManager();

        if (skillManager == null ||
            !skillManager.IsSkillUnlocked(SkillManager.SkillIds.DashEmberStep) ||
            skillManager.dash == null ||
            !skillManager.dash.IsReady())
            return;

        queuedDashDir = Input.GetAxisRaw("Horizontal");

        if (queuedDashDir == 0)
            queuedDashDir = facingDir;

        if (queuedDashCoroutine != null)
            StopCoroutine(queuedDashCoroutine);

        queuedDashCoroutine = StartCoroutine(UseDashAfterPreciseDodgeCheck(Time.frameCount, queuedDashDir));
    }

    private IEnumerator UseDashAfterPreciseDodgeCheck(int inputFrame, float requestedDashDir)
    {
        yield return null;

        queuedDashCoroutine = null;

        if (lastPreciseDodgeFrame >= inputFrame)
            yield break;

        if (skill != null && skill.preciseDodge != null && skill.preciseDodge.IsDodging)
            yield break;

        if (IsWallDetected() && !IsGroundDetected())
            yield break;

        SkillManager skillManager = GetSkillManager();

        if (skillManager != null &&
            skillManager.IsSkillUnlocked(SkillManager.SkillIds.DashEmberStep) &&
            skillManager.dash != null &&
            skillManager.dash.CanUseSkill())
        {
            dashDir = requestedDashDir;
            stateMachine.ChangeState(dashState);
        }
    }

    private bool ShiftWasPressedThisFrame()
    {
        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
    }
    public void HealEvent()
    {
        HealEvent(0);
    }

    public void HealEvent(int flaskIndex)
    {
        if (!canHeal) return;

        PlayerFlaskSystem flaskSystem = PlayerFlaskSystem.GetOrCreate();

        if (flaskSystem != null)
        {
            if (flaskIndex <= 0)
                flaskSystem.UseMainFlask();
            else
                flaskSystem.TryUseSubFlask(flaskIndex - 1);
        }

        canHeal = false; // 防止重复触发
    }
    private bool IsDeadOrDying()
    {
        return (stats != null && stats.isDead) ||
               (stateMachine != null && stateMachine.currentState == deadState);
    }

    private void CancelQueuedSkillInputs()
    {
        lastShiftPressedTime = -999f;
        queuedDashDir = 0f;

        if (queuedDashCoroutine != null)
        {
            StopCoroutine(queuedDashCoroutine);
            queuedDashCoroutine = null;
        }
    }

    private SkillManager GetSkillManager()
    {
        if (skill == null)
            skill = SkillManager.instance;

        return skill;
    }

    private void ResetSkillsAfterRevive()
    {
        SkillManager skillManager = GetSkillManager();

        if (skillManager == null)
            return;

        skillManager.CancelAllActiveSkills();
        skillManager.ResetAllCooldowns();
    }

    public void RespawnAt(Vector3 position)
    {
        CancelDeathAnimationWait();
        CancelHazardImpact(false);
        CancelQueuedSkillInputs();

        isBusy = false;
        SetInvisible(false);
        ResetExtraJumps();

        if (rb != null)
            rb.velocity = Vector2.zero;

        transform.position = position;
        stats?.ReviveToFullHealth();
        ResetSkillsAfterRevive();

        if (stateMachine != null && stateMachine.currentState != null && idleState != null)
            stateMachine.ChangeState(idleState);
    }

    public void RestoreAtSavedState(Vector3 position, int health)
    {
        CancelDeathAnimationWait();
        CancelHazardImpact(false);
        CancelQueuedSkillInputs();

        isBusy = false;
        SetInvisible(false);
        ResetExtraJumps();

        if (rb != null)
            rb.velocity = Vector2.zero;

        transform.position = position;

        if (stats != null)
            stats.ReviveWithHealth(health);

        ResetSkillsAfterRevive();

        if (stateMachine != null && stateMachine.currentState != null && idleState != null)
            stateMachine.ChangeState(idleState);
    }

    public void BeginDeathAnimationWait(string deathAnimationStateName)
    {
        CancelDeathAnimationWait();
        deathAnimationFinishedNotified = false;
        deathAnimationFallbackCoroutine = StartCoroutine(DeathAnimationFallbackRoutine(deathAnimationStateName));
    }

    public void NotifyDeathAnimationFinished()
    {
        if (deathAnimationFinishedNotified)
            return;

        deathAnimationFinishedNotified = true;

        if (deathAnimationFallbackCoroutine != null)
        {
            StopCoroutine(deathAnimationFallbackCoroutine);
            deathAnimationFallbackCoroutine = null;
        }

        OnDeathAnimationFinished?.Invoke();
    }

    public void CancelDeathAnimationWait()
    {
        if (deathAnimationFallbackCoroutine != null)
        {
            StopCoroutine(deathAnimationFallbackCoroutine);
            deathAnimationFallbackCoroutine = null;
        }

        deathAnimationFinishedNotified = false;
    }

    private IEnumerator DeathAnimationFallbackRoutine(string deathAnimationStateName)
    {
        yield return null;

        float waitTime = GetAnimationClipLength(deathAnimationStateName);

        if (waitTime <= 0f)
            waitTime = deathUiFallbackDelay;

        yield return new WaitForSeconds(waitTime);
        deathAnimationFallbackCoroutine = null;
        NotifyDeathAnimationFinished();
    }

    private float GetAnimationClipLength(string stateName)
    {
        if (anim == null || anim.runtimeAnimatorController == null)
            return 0f;

        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;

        if (clips == null)
            return 0f;

        string normalizedStateName = NormalizeAnimationName(stateName);

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];

            if (clip == null)
                continue;

            if (NormalizeAnimationName(clip.name) == normalizedStateName)
                return clip.length;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];

            if (clip == null)
                continue;

            string normalizedClipName = NormalizeAnimationName(clip.name);

            if (normalizedClipName.Contains(normalizedStateName) || normalizedStateName.Contains(normalizedClipName))
                return clip.length;
        }

        return 0f;
    }

    private string NormalizeAnimationName(string animationName)
    {
        if (string.IsNullOrWhiteSpace(animationName))
            return string.Empty;

        return animationName.Replace("/", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }

    public override void Die()
    {
        CancelHazardImpact(false);
        CancelQueuedSkillInputs();
        GetSkillManager()?.CancelAllActiveSkills();
        base.Die();

        stateMachine.ChangeState(deadState);
    }
}
