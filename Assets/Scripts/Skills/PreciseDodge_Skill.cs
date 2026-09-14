using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreciseDodge_Skill : Skill
{
    [Header("Precise Dodge")]
    [SerializeField] private float dodgeDuration = 0.4f;
    [SerializeField] private float dodgeSpeed = 14f;
    [SerializeField] private float timeStopDuration = 1f;
    [SerializeField] private float inputBufferDuration = 0.18f;
    [SerializeField] private float followUpWindow = 1f;
    [SerializeField] private bool skillUnlocked;
    [SerializeField] private bool followUpUnlocked;

    [Header("Follow Up Attack")]
    [SerializeField] private float followUpAttackDuration = 0.65f;
    [SerializeField] private float[] followUpSegmentDurations = { 0.6f, 0.5f, 1.1f };
    [SerializeField] private float[] followUpSegmentHitTimes = { 0.18f, 0.22f, 0.35f };
    [SerializeField] private float[] followUpSegmentLungeDurations = { 0.08f, 0.08f, 0f };
    [SerializeField] private float[] followUpSegmentLungeSpeeds = { 4f, 5f, 0f };
    [SerializeField] private float followUpLungeDuration = 0.28f;
    [SerializeField] private float followUpLungeSpeed = 16f;
    [SerializeField] private float followUpHitRadiusMultiplier = 1.35f;
    [SerializeField] private float followUpHitForwardOffset = 0.55f;
    [SerializeField] private bool followUpCanHitSameEnemyMultipleTimes = true;
    [SerializeField] private float followUpFinisherStartTime = 0.18f;
    [SerializeField] private float followUpFinisherDuration = 0.65f;
    [SerializeField] private float followUpFinisherForwardDistance = 2.4f;

    private readonly List<Enemy> frozenEnemies = new List<Enemy>();
    private readonly HashSet<EnemyStats> followUpHitTargets = new HashSet<EnemyStats>();
    private Coroutine timeStopRoutine;
    private float bonusTimeStopDuration;
    private float followUpExpiresAt;
    private bool followUpQueued;
    private int followUpComboCounter;

    public bool IsDodging { get; private set; }
    public bool IsFollowUpAttacking { get; private set; }
    public float DodgeDuration => dodgeDuration;
    public float DodgeSpeed => dodgeSpeed;
    public float FollowUpAttackDuration => GetFollowUpSegmentDuration(followUpComboCounter);
    public float CurrentFollowUpLungeDuration => GetArrayValue(followUpSegmentLungeDurations, followUpComboCounter, followUpLungeDuration);
    public float CurrentFollowUpLungeSpeed => GetArrayValue(followUpSegmentLungeSpeeds, followUpComboCounter, followUpLungeSpeed);
    public float FollowUpFinisherStartTime => followUpFinisherStartTime;
    public float FollowUpFinisherDuration => followUpFinisherDuration;
    public float FollowUpFinisherForwardDistance => followUpFinisherForwardDistance;
    public int CurrentFollowUpComboIndex => followUpComboCounter;
    public bool CurrentFollowUpUsesFinisher => followUpComboCounter >= MaxFollowUpComboIndex;
    public int DodgeDirection { get; private set; } = -1;

    private int MaxFollowUpComboIndex => 2;

    private void Reset()
    {
        cooldown = 1f;
    }

    protected override void Start()
    {
        base.Start();

        if (cooldown <= 0)
            cooldown = 1f;
    }

    public bool TryStartFromAttackFrame(Transform attacker)
    {
        if (!skillUnlocked)
            return false;

        if (IsDodging)
            return true;

        EnsurePlayerReference();

        if (!HasLivingPlayer())
            return false;

        if (!player.HasBufferedPreciseDodgeInput(inputBufferDuration))
            return false;

        if (!IsReady())
            return false;

        player.ConsumePreciseDodgeInput();
        StartCooldown();
        StartPreciseDodge(attacker);

        return true;
    }

    public void BeginDodgeState()
    {
        EnsurePlayerReference();
        IsDodging = true;
    }

    public void EndDodgeState()
    {
        IsDodging = false;
    }

    public void AddTimeStopDuration(float extraDuration)
    {
        bonusTimeStopDuration += Mathf.Max(0, extraDuration);
    }

    public void UnlockFollowUpAttack()
    {
        followUpUnlocked = true;
    }

    public void ApplySkillTreeUnlocks(bool preciseDodgeUnlocked, bool followUpAttackUnlocked)
    {
        skillUnlocked = preciseDodgeUnlocked;
        followUpUnlocked = followUpAttackUnlocked;

        if (!skillUnlocked)
            CancelPreciseDodge();
    }

    public bool TryUseFollowUpAttack()
    {
        EnsurePlayerReference();

        if (!followUpUnlocked || !HasLivingPlayer())
            return false;

        if (player.stateMachine.currentState == player.preciseDodgeAttackState)
        {
            if (followUpComboCounter >= MaxFollowUpComboIndex)
                return false;

            followUpQueued = true;
            return true;
        }

        if (followUpExpiresAt <= 0 || Time.time > followUpExpiresAt)
            return false;

        if (player.stateMachine.currentState == player.preciseDodgeState)
        {
            followUpQueued = true;
            return true;
        }

        return StartFollowUpAttack();
    }

    public bool TryStartQueuedFollowUpAttack()
    {
        if (!followUpQueued)
            return false;

        followUpQueued = false;

        if (!followUpUnlocked || !HasLivingPlayer() || followUpExpiresAt <= 0 || Time.time > followUpExpiresAt)
            return false;

        return StartFollowUpAttack();
    }

    public void CancelFollowUpAttack()
    {
        followUpQueued = false;
        followUpExpiresAt = 0;
        followUpComboCounter = 0;
        EndFollowUpAttack();
    }

    public void CancelPreciseDodge()
    {
        CancelFollowUpAttack();
        IsDodging = false;

        if (timeStopRoutine != null)
        {
            StopCoroutine(timeStopRoutine);
            timeStopRoutine = null;
        }

        ReleaseFrozenEnemies();
    }

    private bool StartFollowUpAttack()
    {
        if (!HasLivingPlayer())
            return false;

        followUpQueued = false;
        followUpExpiresAt = 0;
        player.stateMachine.ChangeState(player.preciseDodgeAttackState);
        return true;
    }

    public float GetCurrentFollowUpHitTime()
    {
        return GetArrayValue(followUpSegmentHitTimes, followUpComboCounter, 0.2f);
    }

    public bool TryPrepareQueuedFollowUpSegment()
    {
        if (!followUpQueued || followUpComboCounter >= MaxFollowUpComboIndex)
            return false;

        followUpQueued = false;
        followUpComboCounter++;
        followUpExpiresAt = Time.time + followUpWindow;
        return true;
    }

    public void CompleteFollowUpAttackSegment()
    {
        EndFollowUpAttack();

        if (followUpComboCounter >= MaxFollowUpComboIndex)
        {
            followUpComboCounter = 0;
            followUpQueued = false;
            followUpExpiresAt = 0;
            return;
        }

        followUpComboCounter++;
        followUpExpiresAt = Time.time + followUpWindow;
    }

    public void BeginFollowUpAttack()
    {
        EnsurePlayerReference();
        IsFollowUpAttacking = true;
        followUpHitTargets.Clear();
    }

    public void EndFollowUpAttack()
    {
        IsFollowUpAttacking = false;
        followUpHitTargets.Clear();
    }

    private float GetFollowUpSegmentDuration(int index)
    {
        return GetArrayValue(followUpSegmentDurations, index, followUpAttackDuration);
    }

    private float GetArrayValue(float[] values, int index, float fallback)
    {
        if (values == null || values.Length == 0)
            return fallback;

        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    public void DealFollowUpDamage()
    {
        EnsurePlayerReference();

        if (!IsFollowUpAttacking || !HasLivingPlayer() || player.attackCheak == null)
            return;

        Vector2 center = player.attackCheak.position;
        center += Vector2.right * player.facingDir * followUpHitForwardOffset;

        float radius = Mathf.Max(0.1f, player.attackCheakRidus * followUpHitRadiusMultiplier);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);
        HashSet<EnemyStats> hitThisPulse = new HashSet<EnemyStats>();

        foreach (Collider2D hit in colliders)
        {
            EnemyStats target = hit.GetComponentInParent<EnemyStats>();

            if (target == null || !hitThisPulse.Add(target))
                continue;

            if (!followUpCanHitSameEnemyMultipleTimes && !followUpHitTargets.Add(target))
                continue;

            player.stats.DoDamage(target);

            ItemData_Equipment weaponData = Inventory.instance != null ? Inventory.instance.GetEquipment(EquipmentType.Weapon) : null;

            if (weaponData != null)
                weaponData.Effect(target.transform);
        }
    }

    private void StartPreciseDodge(Transform attacker)
    {
        float totalTimeStopDuration = timeStopDuration + bonusTimeStopDuration;

        FaceAttackerAndSetDodgeDirection(attacker);

        IsDodging = true;
        followUpQueued = false;
        followUpComboCounter = 0;
        followUpExpiresAt = Time.time + totalTimeStopDuration + followUpWindow;

        player.MarkPreciseDodgeStarted();
        player.stateMachine.ChangeState(player.preciseDodgeState);

        StartEnemyTimeStop(attacker, totalTimeStopDuration);
    }

    private void FaceAttackerAndSetDodgeDirection(Transform attacker)
    {
        DodgeDirection = -player.facingDir;

        if (attacker == null)
            return;

        float directionToAttacker = attacker.position.x - player.transform.position.x;

        if (Mathf.Abs(directionToAttacker) < 0.05f)
            return;

        int desiredFacingDir = directionToAttacker > 0 ? 1 : -1;

        if (player.facingDir != desiredFacingDir)
            player.Flip();

        DodgeDirection = -desiredFacingDir;
    }

    private void StartEnemyTimeStop(Transform attacker, float duration)
    {
        if (timeStopRoutine != null)
        {
            StopCoroutine(timeStopRoutine);
            ReleaseFrozenEnemies();
        }

        Enemy dodgedEnemy = FindDodgedEnemy(attacker);

        if (dodgedEnemy == null)
            return;

        timeStopRoutine = StartCoroutine(EnemyTimeStopCoroutine(dodgedEnemy, duration));
    }

    private IEnumerator EnemyTimeStopCoroutine(Enemy dodgedEnemy, float duration)
    {
        frozenEnemies.Clear();

        SetEnemyTimeStopped(dodgedEnemy, true);
        frozenEnemies.Add(dodgedEnemy);

        yield return new WaitForSeconds(duration);

        ReleaseFrozenEnemies();
        timeStopRoutine = null;
    }

    private void ReleaseFrozenEnemies()
    {
        foreach (Enemy enemy in frozenEnemies)
        {
            if (enemy != null)
                SetEnemyTimeStopped(enemy, false);
        }

        frozenEnemies.Clear();
    }

    private Enemy FindDodgedEnemy(Transform attacker)
    {
        if (attacker == null)
            return null;

        Enemy enemy = attacker.GetComponentInParent<Enemy>();

        if (enemy != null)
            return enemy;

        return attacker.GetComponentInChildren<Enemy>();
    }

    private void SetEnemyTimeStopped(Enemy enemy, bool timeStopped)
    {
        if (enemy == null)
            return;

        if (enemy is IPreciseDodgeTimeStopTarget preciseDodgeTarget)
        {
            preciseDodgeTarget.SetPreciseDodgeTimeStop(timeStopped);
            return;
        }

        enemy.FreezeTime(timeStopped);
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
}
