using System.Collections;
using UnityEngine;

public class Enemy : Entity
{
    [SerializeField] protected LayerMask whatIsPlayer;

    [Header("Stunned Info")]
    public float stunDuration;
    public Vector2 stunDirection;
    protected bool canBeStunned;
    [SerializeField] protected GameObject counterImage;


    [Header("Move Info")]
    public float moveSpeed;
    public float idleTime;
    public float battleTime;
    public float defaultMoveSpeed;

    [Header("Attack Info")]
    public float attackDistance;
    public float AttackCoolDown;
    [HideInInspector] public float lastTimeAttacked;

    public static bool isAnyEnemyAttacking = false; // 是否有敌人处于攻击窗口期
    public static bool IsAnyEnemyInAttackWindow()
    {
        return isAnyEnemyAttacking;
    }

    public EnemyStateMachine stateMachine { get; private set; }
    public string lastAnimBoolName { get; private set; }
    private int timedFreezeCount;
    private bool externalFreeze;
    private int controlGeneration;
    private float slowSpeedMultiplier = 1f;
    public bool IsFrozen => timedFreezeCount > 0 || externalFreeze;

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new EnemyStateMachine();

        defaultMoveSpeed = moveSpeed;
    }

    protected override void Update()
    {
        base.Update();
        if (IsFrozen)
            return;
        stateMachine.currentState?.Update();
    }
    public virtual void AssignLastAnimName(string _animBoolName) => lastAnimBoolName = _animBoolName;

    public override void SlowEntityBy(float _slowPercentage, float _slowDuration)
    {
        if (_slowDuration <= 0f || _slowPercentage <= 0f)
            return;
        slowSpeedMultiplier = Mathf.Min(slowSpeedMultiplier, 1f - Mathf.Clamp01(_slowPercentage));
        CancelInvoke(nameof(ReturnDefaultSpeed));
        ApplyFreezeState();
        Invoke(nameof(ReturnDefaultSpeed), _slowDuration);
    }

    protected override void ReturnDefaultSpeed()
    {
        slowSpeedMultiplier = 1f;
        ApplyFreezeState();
    }
    public virtual void FreezeTime(bool _timeFrozen)
    {
        externalFreeze = _timeFrozen;
        ApplyFreezeState();
    }

    private void ApplyFreezeState()
    {
        if (IsFrozen)
        {
            moveSpeed = 0;
            if (anim != null) anim.speed = 0;
            if (rb != null) rb.velocity = Vector2.zero;
        }
        else
        {
            moveSpeed = defaultMoveSpeed * slowSpeedMultiplier;
            if (anim != null) anim.speed = slowSpeedMultiplier;
        }
    }

    public virtual void FreezeTimeFor(float _duration) => StartCoroutine(FreezeTimerCoroutine(_duration));

    protected virtual IEnumerator FreezeTimerCoroutine(float _seconds)
    {
        if (_seconds <= 0f || (this is IGenericControlImmuneEnemy immune && immune.IsImmuneToGenericEnemyControl))
            yield break;
        timedFreezeCount++;
        int generation = controlGeneration;
        ApplyFreezeState();

        yield return new WaitForSeconds(_seconds);

        if (generation != controlGeneration)
            yield break;
        timedFreezeCount = Mathf.Max(0, timedFreezeCount - 1);
        ApplyFreezeState();
    }

    protected virtual void OnDisable()
    {
        // Disabling a GameObject stops timers; stale timers on a disabled
        // component must not clear a newly applied freeze after re-enabling.
        controlGeneration++;
        timedFreezeCount = 0;
        externalFreeze = false;
        slowSpeedMultiplier = 1f;
        CancelInvoke(nameof(ReturnDefaultSpeed));
        ApplyFreezeState();
    }


    #region Counter Attack Window
    //反击时间窗口
    public virtual void OpenCounterAttackWindow()
    {
        canBeStunned = true;
        counterImage.SetActive(true);
    }

    public virtual void CloseCounterAttackWindow()
    {
        canBeStunned = false;
        counterImage.SetActive(false);
    }
    #endregion



    public virtual bool CanBeStunned()
    {
        if (canBeStunned)
        {
            CloseCounterAttackWindow();
            return true;
        }
        return false;
    }

    public virtual void AnimationFinishTrigger() => stateMachine.currentState.AnimationFinishTrigger();

    public virtual bool CanDetectPlayer()
    {
        Player player = PlayerManager.instance != null ? PlayerManager.instance.player : null;

        if (player == null || !player.isActiveAndEnabled || player.IsInvisible)
            return false;

        CharacterStats playerStats = player.stats != null ? player.stats : player.GetComponent<CharacterStats>();
        return playerStats == null || !playerStats.isDead;
    }

    public virtual RaycastHit2D IsPlayerDetected()
    {
        if (!CanDetectPlayer())
            return new RaycastHit2D();

        return Physics2D.Raycast(wallCheak.position, Vector2.right * facingDir, 50, whatIsPlayer);
    }


    //绘制检测线
    public override void OnDrawGizmos()
    {
        if (Application.isPlaying)
            return;

        base.OnDrawGizmos();

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x + attackDistance * facingDir, transform.position.y));
    }
}
