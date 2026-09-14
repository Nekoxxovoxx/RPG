using UnityEngine;

public class Enemy_Imp : Enemy
{
    private enum ImpState
    {
        Wander,
        Orbit,
        Attack,
        Recoil,
        Hit,
        Dead
    }

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 6f;
    [SerializeField, Min(0.1f)] private float loseTargetRadius = 8f;
    [SerializeField, Min(0.1f)] private float bodyDamageRadius = 0.35f;

    [Header("Flight")]
    [SerializeField, Min(0f)] private float orbitSpeedBonus = 0.6f;
    [SerializeField, Min(0f)] private float attackSpeedBonus = 5.5f;
    [SerializeField, Min(0.1f)] private float steeringSharpness = 7f;
    [SerializeField, Min(0f)] private float curveStrength = 0.45f;
    [SerializeField, Min(0f)] private float curveFrequency = 3.5f;
    [SerializeField, Min(0f)] private float randomDriftStrength = 0.25f;
    [SerializeField, Min(0.1f)] private float randomDriftInterval = 0.55f;
    [SerializeField, Min(0.1f)] private float wanderRadius = 1.6f;
    [SerializeField, Min(0.1f)] private float wanderPointInterval = 1.2f;

    [Header("Orbit")]
    [SerializeField, Min(0.1f)] private float preferredPlayerDistance = 2.15f;
    [SerializeField, Min(0.1f)] private float minimumPlayerDistance = 1.05f;
    [SerializeField, Min(0f)] private float distanceCorrectionWeight = 1.25f;
    [SerializeField, Min(0f)] private float orbitTangentWeight = 1.1f;
    [SerializeField, Min(0.1f)] private float orbitDirectionChangeInterval = 2.2f;

    [Header("Collision Attack")]
    [SerializeField, Min(0.05f)] private float attackDuration = 0.78f;
    [SerializeField, Min(0f)] private float recoilDuration = 0.48f;
    [SerializeField, Min(0f)] private float recoilDistance = 1.45f;
    [SerializeField, Min(0f)] private float recoilUpLift = 0.18f;

    [Header("Obstacle Avoidance")]
    [SerializeField, Min(0.05f)] private float obstacleProbeRadius = 0.28f;
    [SerializeField, Min(0.01f)] private float obstacleProbeMinDistance = 0.35f;
    [SerializeField, Min(0f)] private float obstacleSkinWidth = 0.06f;

    [Header("Pathing")]
    [SerializeField, Min(0.1f)] private float pathAroundWeight = 1.45f;
    [SerializeField, Min(0f)] private float pathTowardPlayerWeight = 0.45f;
    [SerializeField, Min(0f)] private float pathAwayFromWallWeight = 0.35f;

    [Header("Hit Reaction")]
    [SerializeField, Min(0f)] private float hitStunDuration = 0.24f;
    [SerializeField, Min(0f)] private float hitKnockbackSpeed = 5.2f;
    [SerializeField, Min(0f)] private float hitKnockbackUpLift = 0.2f;

    [Header("Animation")]
    [SerializeField] private string idleAnimationName = "Idle/Move";
    [SerializeField] private string deathAnimationName = "death";
    [SerializeField, Min(0f)] private float destroyDelayAfterDeath = 1.1f;

    [Header("Facing")]
    [SerializeField] private bool spriteFacesRightByDefault;

    private ImpState currentState;
    private Transform player;
    private SpriteRenderer[] spriteRenderers;
    private Quaternion initialLocalRotation;
    private Vector2 homePosition;
    private Vector2 currentVelocity;
    private Vector2 wanderTarget;
    private Vector2 driftDirection;
    private Vector2 attackDirection;
    private int orbitDirection = 1;
    private float wanderTimer;
    private float driftTimer;
    private float orbitDirectionTimer;
    private float stateTimer;
    private bool timeFrozen;
    private EntityFX impFx;
    private AudioEventPlayer audioEvents;
    private ContactFilter2D obstacleFilter;
    private readonly RaycastHit2D[] obstacleHits = new RaycastHit2D[8];

    protected override void Start()
    {
        base.Start();

        impFx = GetComponent<EntityFX>();

        if (impFx == null)
            impFx = GetComponentInChildren<EntityFX>();

        if (impFx == null)
            impFx = gameObject.AddComponent<EntityFX>();

        ResolveAudioEvents();

        initialLocalRotation = transform.localRotation;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        homePosition = transform.position;
        player = PlayerManager.instance != null && PlayerManager.instance.player != null
            ? PlayerManager.instance.player.transform
            : null;

        if (whatIsPlayer.value == 0)
            whatIsPlayer = LayerMask.GetMask("Player");

        EnsureGroundMask();
        RefreshObstacleFilter();

        if (attackCheak == null)
            attackCheak = transform;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        if (cd != null)
            cd.isTrigger = false;

        if (AttackCoolDown <= 0f)
            AttackCoolDown = 1.35f;

        PickNewWanderPoint();
        PickNewDrift();
        PickNewOrbitDirection();
        ChangeState(ImpState.Wander);
        ApplyVisualFacing();
        PlayFirstExistingState(idleAnimationName, "Idle/Move", "Idle", "idle", "Imp");
    }

    protected override void Update()
    {
        if (currentState == ImpState.Dead)
            return;

        if (timeFrozen)
        {
            SetZeroVelocity();
            return;
        }

        ResolvePlayer();
        UpdateTimers();

        if (!CanDetectPlayer() &&
            (currentState == ImpState.Orbit || currentState == ImpState.Attack))
        {
            ChangeState(ImpState.Wander);
        }

        switch (currentState)
        {
            case ImpState.Wander:
                UpdateWander();
                break;
            case ImpState.Orbit:
                UpdateOrbit();
                break;
            case ImpState.Attack:
                UpdateAttack();
                break;
            case ImpState.Recoil:
                UpdateRecoil();
                break;
            case ImpState.Hit:
                UpdateHit();
                break;
        }
    }

    private void ResolvePlayer()
    {
        if (player == null && PlayerManager.instance != null && PlayerManager.instance.player != null)
            player = PlayerManager.instance.player.transform;
    }

    private void UpdateTimers()
    {
        wanderTimer -= Time.deltaTime;
        driftTimer -= Time.deltaTime;
        orbitDirectionTimer -= Time.deltaTime;
        stateTimer -= Time.deltaTime;

        if (driftTimer <= 0f)
            PickNewDrift();

        if (orbitDirectionTimer <= 0f)
            PickNewOrbitDirection();
    }

    private void UpdateWander()
    {
        if (CanSeePlayer(detectionRadius))
        {
            ChangeState(ImpState.Orbit);
            return;
        }

        if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.2f)
            PickNewWanderPoint();

        Vector2 toTarget = wanderTarget - (Vector2)transform.position;
        Vector2 desiredDirection = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector2.right * facingDir;

        Steer(AddFlightCurve(desiredDirection, 0.45f) * moveSpeed);
        FaceVelocity();
    }

    private void UpdateOrbit()
    {
        if (!CanSeePlayer(loseTargetRadius))
        {
            ChangeState(ImpState.Wander);
            return;
        }

        FacePlayer();

        if (CanBeginAttack())
        {
            BeginAttack();
            return;
        }

        Steer(GetOrbitVelocity());
    }

    private void UpdateAttack()
    {
        if (!CanDetectPlayer() || player == null)
        {
            BeginRecoil(null);
            return;
        }

        if (IsPathBlockedToPlayer())
        {
            BeginRecoil(null);
            return;
        }

        FacePlayer();
        TryDamagePlayerByOverlap();

        if (stateTimer <= 0f)
        {
            BeginRecoil(player);
            return;
        }

        Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position);

        if (toPlayer.sqrMagnitude > 0.001f)
        {
            Vector2 newDirection = toPlayer.normalized;
            attackDirection = Vector2.Lerp(attackDirection, newDirection, 1f - Mathf.Exp(-3f * Time.deltaTime)).normalized;
        }

        Vector2 desiredVelocity = AddFlightCurve(attackDirection, 0.28f) * (moveSpeed + attackSpeedBonus);

        if (TryGetObstacleAhead(desiredVelocity, out RaycastHit2D obstacleHit))
        {
            BeginObstacleRecoil(obstacleHit.normal);
            return;
        }

        Steer(desiredVelocity);
    }

    private void UpdateRecoil()
    {
        FacePlayer();

        if (stateTimer <= 0f)
        {
            ChangeState(CanSeePlayer(detectionRadius) ? ImpState.Orbit : ImpState.Wander);
            return;
        }
    }

    private void UpdateHit()
    {
        FacePlayer();

        if (stateTimer <= 0f)
        {
            ChangeState(CanSeePlayer(detectionRadius) ? ImpState.Orbit : ImpState.Wander);
            return;
        }
    }

    private Vector2 GetOrbitVelocity()
    {
        if (TryGetPathObstacle(out RaycastHit2D pathHit))
            return GetPathAroundObstacleVelocity(pathHit, moveSpeed + orbitSpeedBonus);

        Vector2 fromPlayer = (Vector2)transform.position - (Vector2)player.position;
        float distance = fromPlayer.magnitude;

        if (distance <= 0.001f)
            fromPlayer = Vector2.up;
        else
            fromPlayer /= distance;

        if (distance < minimumPlayerDistance)
            return AddFlightCurve(fromPlayer, 0.25f) * (moveSpeed + orbitSpeedBonus + 1.5f);

        Vector2 toPlayer = -fromPlayer;
        Vector2 tangent = new Vector2(-fromPlayer.y, fromPlayer.x) * orbitDirection;
        float distanceError = distance - preferredPlayerDistance;
        Vector2 distanceCorrection = Mathf.Abs(distanceError) > 0.15f
            ? (distanceError > 0f ? toPlayer : fromPlayer) * distanceCorrectionWeight
            : Vector2.zero;

        Vector2 desiredDirection = tangent * orbitTangentWeight + distanceCorrection + driftDirection * randomDriftStrength;

        if (desiredDirection.sqrMagnitude <= 0.001f)
            desiredDirection = tangent;

        return desiredDirection.normalized * (moveSpeed + orbitSpeedBonus);
    }

    private Vector2 GetPathAroundObstacleVelocity(RaycastHit2D pathHit, float speed)
    {
        Vector2 toPlayer = ((Vector2)player.position - GetObstacleProbeOrigin()).normalized;

        if (toPlayer.sqrMagnitude <= 0.001f)
            toPlayer = Vector2.right * facingDir;

        Vector2 wallNormal = pathHit.normal.sqrMagnitude > 0.001f ? pathHit.normal.normalized : -toPlayer;
        Vector2 tangentA = new Vector2(-wallNormal.y, wallNormal.x);
        Vector2 tangentB = -tangentA;
        float tangentAScore = Vector2.Dot(tangentA, toPlayer);
        float tangentBScore = Vector2.Dot(tangentB, toPlayer);
        Vector2 tangent = tangentAScore >= tangentBScore ? tangentA : tangentB;

        if (Mathf.Abs(tangentAScore - tangentBScore) < 0.05f)
            tangent = tangentA * orbitDirection;

        Vector2 desiredDirection = tangent * pathAroundWeight +
                                   toPlayer * pathTowardPlayerWeight +
                                   wallNormal * pathAwayFromWallWeight +
                                   driftDirection * randomDriftStrength;

        return desiredDirection.sqrMagnitude > 0.001f ? desiredDirection.normalized * speed : tangent * speed;
    }

    private bool CanBeginAttack()
    {
        if (!CanDetectPlayer() || player == null)
            return false;

        if (Time.time < lastTimeAttacked + AttackCoolDown)
            return false;

        if (IsPathBlockedToPlayer())
            return false;

        float distance = Vector2.Distance(transform.position, player.position);
        return distance >= minimumPlayerDistance * 0.95f && distance <= detectionRadius;
    }

    private void BeginAttack()
    {
        lastTimeAttacked = Time.time;
        stateTimer = attackDuration;
        attackDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;

        if (attackDirection.sqrMagnitude <= 0.001f)
            attackDirection = Vector2.right * facingDir;

        ChangeState(ImpState.Attack);
    }

    private Vector2 AddFlightCurve(Vector2 desiredDirection, float curveWeight)
    {
        if (desiredDirection.sqrMagnitude <= 0.001f)
            desiredDirection = Vector2.right * facingDir;

        Vector2 perpendicular = new Vector2(-desiredDirection.y, desiredDirection.x);
        float wave = Mathf.Sin(Time.time * curveFrequency + GetInstanceID() * 0.037f);
        Vector2 curved = desiredDirection + perpendicular * wave * curveStrength * curveWeight + driftDirection * randomDriftStrength * curveWeight;
        return curved.sqrMagnitude > 0.001f ? curved.normalized : desiredDirection;
    }

    private void Steer(Vector2 desiredVelocity)
    {
        currentVelocity = Vector2.Lerp(currentVelocity, desiredVelocity, 1f - Mathf.Exp(-steeringSharpness * Time.deltaTime));
        currentVelocity = ResolveObstacleVelocity(currentVelocity);

        if (rb != null)
            rb.velocity = currentVelocity;
    }

    private Vector2 ResolveObstacleVelocity(Vector2 velocity)
    {
        if (!TryGetObstacleAhead(velocity, out RaycastHit2D hit))
            return velocity;

        Vector2 slideVelocity = velocity - hit.normal * Vector2.Dot(velocity, hit.normal);
        return slideVelocity.sqrMagnitude > 0.01f ? slideVelocity : Vector2.zero;
    }

    private bool TryGetObstacleAhead(Vector2 velocity, out RaycastHit2D closestHit)
    {
        closestHit = new RaycastHit2D();

        if (velocity.sqrMagnitude <= 0.0001f || whatIsGround.value == 0)
            return false;

        Vector2 direction = velocity.normalized;
        float distance = Mathf.Max(obstacleProbeMinDistance, velocity.magnitude * Time.deltaTime + obstacleSkinWidth);
        int hitCount = Physics2D.CircleCast(GetObstacleProbeOrigin(), GetObstacleProbeRadius(), direction, obstacleFilter, obstacleHits, distance);

        return TryGetClosestValidObstacle(hitCount, out closestHit);
    }

    private Vector2 GetObstacleProbeOrigin()
    {
        return cd != null ? (Vector2)cd.bounds.center : (Vector2)transform.position;
    }

    private float GetObstacleProbeRadius()
    {
        if (cd == null)
            return obstacleProbeRadius;

        Vector2 extents = cd.bounds.extents;
        float bodyRadius = Mathf.Min(extents.x, extents.y) * 0.9f;
        return Mathf.Max(obstacleProbeRadius, bodyRadius);
    }

    private bool TryGetClosestValidObstacle(int hitCount, out RaycastHit2D closestHit)
    {
        closestHit = new RaycastHit2D();
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = obstacleHits[i];

            if (hit.collider == null || IsOwnCollider(hit.collider))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
            }
        }

        return closestHit.collider != null;
    }

    private bool CanSeePlayer(float radius)
    {
        if (!CanDetectPlayer() || player == null)
            return false;

        Vector2 center = attackCheak != null ? (Vector2)attackCheak.position : (Vector2)transform.position;

        if (Vector2.Distance(center, player.position) > radius)
            return false;

        Collider2D hit = Physics2D.OverlapCircle(center, radius, whatIsPlayer);
        return hit != null;
    }

    private void TryDamagePlayerByOverlap()
    {
        if (stats == null || !CanDetectPlayer())
            return;

        Vector2 center = attackCheak != null ? (Vector2)attackCheak.position : (Vector2)transform.position;
        Collider2D hit = Physics2D.OverlapCircle(center, bodyDamageRadius, whatIsPlayer);

        if (hit == null || !hit.TryGetComponent(out Player hitPlayer))
            return;

        PlayerStats targetStats = hit.GetComponent<PlayerStats>();

        if (targetStats == null || targetStats.isDead)
            return;

        stats.DoDamage(targetStats);
        PlayAudioCue("hit_player");
        BeginRecoil(hitPlayer.transform);
    }

    private void BeginRecoil(Transform target)
    {
        Vector2 away = target != null
            ? ((Vector2)transform.position - (Vector2)target.position).normalized
            : -attackDirection;

        if (away.sqrMagnitude <= 0.001f)
            away = Vector2.up;

        Vector2 recoilVelocity = (away + Vector2.up * recoilUpLift).normalized * (recoilDistance / Mathf.Max(0.05f, recoilDuration));
        currentVelocity = recoilVelocity;

        if (rb != null)
            rb.velocity = recoilVelocity;

        stateTimer = recoilDuration;
        ChangeState(ImpState.Recoil);
    }

    private void BeginObstacleRecoil(Vector2 obstacleNormal)
    {
        Vector2 away = obstacleNormal.sqrMagnitude > 0.001f ? obstacleNormal.normalized : -attackDirection;
        Vector2 recoilVelocity = (away + Vector2.up * recoilUpLift).normalized * (recoilDistance / Mathf.Max(0.05f, recoilDuration));
        currentVelocity = recoilVelocity;

        if (rb != null)
            rb.velocity = recoilVelocity;

        stateTimer = recoilDuration;
        ChangeState(ImpState.Recoil);
    }

    private void PickNewWanderPoint()
    {
        wanderTimer = wanderPointInterval + Random.Range(-0.25f, 0.35f);
        wanderTarget = homePosition + Random.insideUnitCircle * wanderRadius;
    }

    private void PickNewDrift()
    {
        driftTimer = randomDriftInterval + Random.Range(-0.15f, 0.2f);
        driftDirection = Random.insideUnitCircle.normalized;

        if (driftDirection.sqrMagnitude <= 0.001f)
            driftDirection = Vector2.up;
    }

    private void PickNewOrbitDirection()
    {
        orbitDirectionTimer = orbitDirectionChangeInterval + Random.Range(-0.45f, 0.65f);

        if (Random.value < 0.35f)
            orbitDirection *= -1;
    }

    private void FacePlayer()
    {
        if (!CanDetectPlayer() || player == null)
            return;

        float directionToPlayer = player.position.x - transform.position.x;
        SetFacingByDirection(directionToPlayer);
    }

    private void FaceVelocity()
    {
        SetFacingByDirection(currentVelocity.x);
    }

    private void SetFacingByDirection(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.03f)
            return;

        if (directionX > 0f && facingDir < 0)
            Flip();
        else if (directionX < 0f && facingDir > 0)
            Flip();

        ApplyVisualFacing();
    }

    public override void Flip()
    {
        base.Flip();
        transform.localRotation = initialLocalRotation;
        ApplyVisualFacing();
    }

    private void ApplyVisualFacing()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        bool flipX = spriteFacesRightByDefault ? facingDir < 0 : facingDir > 0;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].flipX = flipX;
        }
    }

    private void ChangeState(ImpState newState)
    {
        if (currentState == newState)
            return;

        ImpState previousState = currentState;
        currentState = newState;

        if (newState == ImpState.Orbit && previousState == ImpState.Wander)
            PlayAudioCue("spot_player");
        else if (newState == ImpState.Attack)
            PlayAudioCue("attack_start");
    }

    private void ResolveAudioEvents()
    {
        if (audioEvents != null)
            return;

        audioEvents = GetComponent<AudioEventPlayer>();

        if (audioEvents == null)
            audioEvents = GetComponentInChildren<AudioEventPlayer>();
    }

    private bool PlayAudioCue(string cueName)
    {
        ResolveAudioEvents();
        return audioEvents != null && audioEvents.PlayCue(cueName);
    }

    private void EnsureGroundMask()
    {
        int groundMask = LayerMask.GetMask("Ground");
        int enemyMask = LayerMask.GetMask("Enemy");

        if (groundMask != 0 && (whatIsGround.value == 0 || whatIsGround.value == enemyMask))
            whatIsGround = groundMask;
    }

    private void RefreshObstacleFilter()
    {
        obstacleFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = whatIsGround
        };
    }

    private bool IsPathBlockedToPlayer()
    {
        Vector2 center = attackCheak != null ? (Vector2)attackCheak.position : (Vector2)transform.position;
        return IsPathBlockedToPlayer(center);
    }

    private bool IsPathBlockedToPlayer(Vector2 origin)
    {
        if (player == null || whatIsGround.value == 0)
            return false;

        Vector2 target = player.position;
        int hitCount = Physics2D.Linecast(origin, target, obstacleFilter, obstacleHits);
        return TryGetClosestValidObstacle(hitCount, out _);
    }

    private bool TryGetPathObstacle(out RaycastHit2D pathHit)
    {
        pathHit = new RaycastHit2D();

        if (player == null || whatIsGround.value == 0)
            return false;

        Vector2 origin = GetObstacleProbeOrigin();
        int hitCount = Physics2D.Linecast(origin, player.position, obstacleFilter, obstacleHits);
        return TryGetClosestValidObstacle(hitCount, out pathHit);
    }

    private bool IsOwnCollider(Collider2D other)
    {
        return other != null && (other.transform == transform || other.transform.IsChildOf(transform));
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

    public override void FreezeTime(bool _timeFrozen)
    {
        base.FreezeTime(_timeFrozen);
        timeFrozen = _timeFrozen;

        if (timeFrozen && rb != null)
            rb.velocity = Vector2.zero;
    }

    public override void DamageImpact()
    {
        if (currentState == ImpState.Dead || timeFrozen)
            return;

        if (impFx != null)
            StartCoroutine(impFx.FlashFX());

        ResolvePlayer();

        Vector2 away = player != null
            ? ((Vector2)transform.position - (Vector2)player.position).normalized
            : Vector2.left * facingDir;

        if (away.sqrMagnitude <= 0.001f)
            away = Vector2.up;

        currentVelocity = (away + Vector2.up * hitKnockbackUpLift).normalized * hitKnockbackSpeed;

        if (rb != null)
            rb.velocity = currentVelocity;

        stateTimer = hitStunDuration;
        ChangeState(ImpState.Hit);
    }

    public override void Die()
    {
        if (currentState == ImpState.Dead)
            return;

        currentState = ImpState.Dead;

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (stats == null || !stats.isDead)
            PlayAudioCue("death");

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        PlayFirstExistingState(deathAnimationName, "Death", "death");

        if (destroyDelayAfterDeath > 0f)
            Destroy(gameObject, destroyDelayAfterDeath);
    }

    private void PlayFirstExistingState(params string[] stateNames)
    {
        if (anim == null || stateNames == null)
            return;

        for (int i = 0; i < stateNames.Length; i++)
        {
            string stateName = stateNames[i];

            if (string.IsNullOrWhiteSpace(stateName))
                continue;

            int hash = Animator.StringToHash(stateName);

            if (!anim.HasState(0, hash))
                continue;

            anim.Play(hash, 0, 0f);
            return;
        }
    }

    public override RaycastHit2D IsPlayerDetected()
    {
        if (!CanDetectPlayer() || player == null)
            return new RaycastHit2D();

        Vector2 center = attackCheak != null ? (Vector2)attackCheak.position : (Vector2)transform.position;
        Collider2D hit = Physics2D.OverlapCircle(center, detectionRadius, whatIsPlayer);

        if (hit == null)
            return new RaycastHit2D();

        Vector2 direction = ((Vector2)hit.transform.position - center).normalized;
        return Physics2D.Raycast(center, direction, detectionRadius, whatIsPlayer);
    }

    public override void OnDrawGizmos()
    {
        if (Application.isPlaying)
            return;

        Vector3 center = attackCheak != null ? attackCheak.position : transform.position;

        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.55f);
        Gizmos.DrawWireSphere(center, detectionRadius);

        Gizmos.color = new Color(1f, 0.92f, 0.25f, 0.45f);
        Gizmos.DrawWireSphere(center, preferredPlayerDistance);

        Gizmos.color = new Color(0.55f, 0.95f, 1f, 0.45f);
        Gizmos.DrawWireSphere(center, minimumPlayerDistance);

        Gizmos.color = new Color(1f, 0f, 0f, 0.75f);
        Gizmos.DrawWireSphere(center, bodyDamageRadius);
    }
}
