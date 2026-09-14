using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Skeleton : Enemy
{
    [Header("Patrol Avoidance")]
    [SerializeField] private LayerMask patrolDangerMask = ~0;
    [SerializeField, Min(0.05f)] private float patrolForwardProbeDistance = 0.45f;
    [SerializeField, Min(0.05f)] private float patrolGroundProbeForwardOffset = 0.45f;
    [SerializeField, Min(0.05f)] private float patrolGroundProbeShortOffset = 0.22f;
    [SerializeField, Min(0.05f)] private float patrolGroundProbeUpOffset = 0.15f;
    [SerializeField, Min(0.1f)] private float patrolGroundProbeDistance = 1.2f;
    [SerializeField, Min(0.02f)] private float patrolHazardProbeRadius = 0.14f;
    [SerializeField, Min(0.05f)] private float patrolTurnCooldown = 0.18f;
    [SerializeField, Min(0.1f)] private float stuckTurnTime = 0.55f;
    [SerializeField, Min(0.001f)] private float stuckMinMoveDistance = 0.04f;
    [SerializeField] private string[] patrolDangerKeywords =
    {
        "lava",
        "magma",
        "spike",
        "thorn",
        "hazard",
        "\u5ca9\u6d46",
        "\u5730\u523a"
    };

    private float lastPatrolTurnTime;
    private float stuckTimer;
    private Vector2 stuckCheckPosition;
    private bool hasStuckCheckPosition;

    #region States

    public SkeletonIdleState idleState {  get; private set; }
    public SkeletonMoveState moveState {  get; private set; }
    public SkeletonBattleState battleState { get; private set; }
    public SkeletonAttackState attackState { get; private set; }
    public SkeletonStunnedState stunnedState { get; private set; }
    public SkeletonDeadState deadState { get; private set; }



    #endregion

    protected override void Awake()
    {
        base.Awake();  
        idleState = new SkeletonIdleState(this, stateMachine, "Idle", this);  
        moveState = new SkeletonMoveState(this, stateMachine, "Move", this);
        battleState = new SkeletonBattleState(this, stateMachine,"Move",this);
        attackState = new SkeletonAttackState(this, stateMachine, "Attack", this);
        stunnedState = new SkeletonStunnedState(this, stateMachine, "Stunned", this);
        deadState = new SkeletonDeadState(this, stateMachine, "Idle", this);
    }

    protected override void Start()
    {
        base.Start();
        SyncInitialFacingWithVisual();
        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        base.Update();
        if(Input.GetKeyUp(KeyCode.U)) 
            stateMachine.ChangeState(stunnedState);
    }

    public  override bool CanBeStunned()
    {
        if (base.CanBeStunned())
        {
            stateMachine.ChangeState(stunnedState);
            return true;
        }
        return false;
    }

    public override void Die()
    {
        base.Die();

        stateMachine.ChangeState(deadState);
    }

    public void ResetPatrolNavigationMemory()
    {
        stuckTimer = 0;
        stuckCheckPosition = transform.position;
        hasStuckCheckPosition = true;
    }

    public bool TryTurnAroundForPatrol()
    {
        if (Time.time < lastPatrolTurnTime + patrolTurnCooldown)
            return false;

        if (!ShouldTurnAroundForPatrol())
            return false;

        SetZeroVelocity();
        Flip();
        lastPatrolTurnTime = Time.time;
        ResetPatrolNavigationMemory();
        return true;
    }

    private bool ShouldTurnAroundForPatrol()
    {
        if (IsWallDetected() || !IsGroundDetected())
            return true;

        if (HasDangerAhead())
            return true;

        if (!HasSafeGroundAhead())
            return true;

        return IsPatrolStuck();
    }

    private bool HasSafeGroundAhead()
    {
        return HasSafeGroundAtOffset(patrolGroundProbeForwardOffset) ||
               HasSafeGroundAtOffset(patrolGroundProbeShortOffset);
    }

    private bool HasSafeGroundAtOffset(float forwardOffset)
    {
        RaycastHit2D hit = CastGroundProbe(forwardOffset, whatIsGround);

        if (hit.collider == null)
            return false;

        return !IsDangerousCollider(hit.collider, hit.point);
    }

    private bool HasDangerAhead()
    {
        Vector2 footPosition = GetFootProbeBase();
        Vector2 forward = Vector2.right * facingDir;
        Vector2 frontFoot = footPosition + forward * patrolGroundProbeForwardOffset;
        Vector2 shortFoot = footPosition + forward * patrolGroundProbeShortOffset;

        if (HasDangerAtPoint(frontFoot) || HasDangerAtPoint(shortFoot))
            return true;

        if (HasDangerAtPoint(frontFoot + Vector2.down * patrolHazardProbeRadius))
            return true;

        RaycastHit2D frontGroundHit = CastGroundProbe(patrolGroundProbeForwardOffset, PatrolDangerMaskValue);
        if (frontGroundHit.collider != null && IsDangerousCollider(frontGroundHit.collider, frontGroundHit.point))
            return true;

        RaycastHit2D shortGroundHit = CastGroundProbe(patrolGroundProbeShortOffset, PatrolDangerMaskValue);
        if (shortGroundHit.collider != null && IsDangerousCollider(shortGroundHit.collider, shortGroundHit.point))
            return true;

        Vector2 wallOrigin = wallCheak != null ? (Vector2)wallCheak.position : (Vector2)transform.position;
        RaycastHit2D[] forwardHits = Physics2D.RaycastAll(wallOrigin, forward, patrolForwardProbeDistance, PatrolDangerMaskValue);

        for (int i = 0; i < forwardHits.Length; i++)
        {
            Collider2D hitCollider = forwardHits[i].collider;

            if (hitCollider != null && IsDangerousCollider(hitCollider, forwardHits[i].point))
                return true;
        }

        return false;
    }

    private void SyncInitialFacingWithVisual()
    {
        bool flippedByRotation = Mathf.Abs(Mathf.DeltaAngle(0f, transform.eulerAngles.y)) > 90f;
        bool flippedByScale = transform.lossyScale.x < 0f;
        bool visualFacesRight = !(flippedByRotation ^ flippedByScale);
        bool facingMatchesVisual = visualFacesRight ? facingDir > 0 : facingDir < 0;

        if (facingMatchesVisual)
            return;

        Flip();
        transform.Rotate(0f, 180f, 0f);
    }

    private bool HasDangerAtPoint(Vector2 point)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(point, patrolHazardProbeRadius, PatrolDangerMaskValue);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (IsDangerousCollider(colliders[i], point))
                return true;
        }

        return false;
    }

    private RaycastHit2D CastGroundProbe(float forwardOffset, int layerMask)
    {
        Vector2 origin = GetFootProbeBase() +
                         Vector2.right * facingDir * forwardOffset +
                         Vector2.up * patrolGroundProbeUpOffset;

        return Physics2D.Raycast(origin, Vector2.down, patrolGroundProbeDistance, layerMask);
    }

    private Vector2 GetFootProbeBase()
    {
        return groundCheak != null ? (Vector2)groundCheak.position : (Vector2)transform.position;
    }

    private bool IsDangerousCollider(Collider2D other, Vector2 samplePoint)
    {
        if (other == null)
            return false;

        Transform otherTransform = other.transform;

        if (otherTransform == transform || otherTransform.IsChildOf(transform))
            return false;

        if (other.GetComponentInParent<InstantDeathHazard>() != null)
            return true;

        TilemapInstantDeathHazard tileHazard = other.GetComponentInParent<TilemapInstantDeathHazard>();

        if (tileHazard != null)
        {
            if (tileHazard.IsLethalAtWorldPosition(samplePoint))
                return true;

            if (tileHazard.IsLethalAtWorldPosition(samplePoint + Vector2.down * patrolHazardProbeRadius))
                return true;

            if (tileHazard.IsLethalAtWorldPosition(samplePoint + Vector2.up * patrolHazardProbeRadius))
                return true;
        }

        return NameContainsDangerKeyword(otherTransform);
    }

    private bool NameContainsDangerKeyword(Transform current)
    {
        if (patrolDangerKeywords == null)
            return false;

        while (current != null)
        {
            string objectName = current.name;

            for (int i = 0; i < patrolDangerKeywords.Length; i++)
            {
                string keyword = patrolDangerKeywords[i];

                if (!string.IsNullOrEmpty(keyword) &&
                    !string.IsNullOrEmpty(objectName) &&
                    objectName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsPatrolStuck()
    {
        if (rb == null || moveSpeed <= 0.01f)
            return false;

        Vector2 currentPosition = transform.position;

        if (!hasStuckCheckPosition)
        {
            ResetPatrolNavigationMemory();
            return false;
        }

        if (Vector2.Distance(currentPosition, stuckCheckPosition) >= stuckMinMoveDistance)
        {
            stuckCheckPosition = currentPosition;
            stuckTimer = 0;
            return false;
        }

        stuckTimer += Time.deltaTime;
        return stuckTimer >= stuckTurnTime;
    }

    private int PatrolDangerMaskValue => patrolDangerMask.value == 0 ? ~0 : patrolDangerMask.value;
}
