using UnityEngine;

public class PlayerPreciseDodgeAttackState : PlayerState
{
    private static readonly string[] AnimationStateNames =
    {
        "playerPreciseDodgeAttack1",
        "playerPreciseDodgeAttack2",
        "playerPreciseDodgeAttack3"
    };

    private float elapsedTime;
    private bool hitTriggered;
    private bool continuingToNextSegment;
    private bool finisherMotionPrepared;
    private Vector2 finisherStartPosition;
    private Vector2 finisherTargetPosition;

    public PlayerPreciseDodgeAttackState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        elapsedTime = 0;
        hitTriggered = false;
        continuingToNextSegment = false;
        finisherMotionPrepared = false;
        stateTimer = player.skill.preciseDodge.FollowUpAttackDuration;
        player.anim.SetInteger("PreciseDodgeComboCounter", player.skill.preciseDodge.CurrentFollowUpComboIndex);
        player.anim.Play(GetAnimationStateName(), 0, 0);
        player.skill.preciseDodge.BeginFollowUpAttack();
    }

    public override void Exit()
    {
        base.Exit();

        player.SetZeroVelocity();
        player.skill.preciseDodge.EndFollowUpAttack();

        if (!continuingToNextSegment)
            player.StartCoroutine("BusyFor", 0.08f);
    }

    public override void Update()
    {
        base.Update();

        if (ShouldInterruptWithAttackInput())
            return;

        elapsedTime += Time.deltaTime;
        ApplyMovement();
        TryDealTimedHits();

        if (triggerCalled || stateTimer < 0)
        {
            FinishSegment();
        }
    }

    private bool ShouldInterruptWithAttackInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            player.skill.preciseDodge.CancelFollowUpAttack();
            ChangeToLocomotionState();
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            player.skill.preciseDodge.CancelFollowUpAttack();
            stateMachine.ChangeState(player.primaryAttack);
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            player.skill.preciseDodge.CancelFollowUpAttack();
            stateMachine.ChangeState(player.aimSword);
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Q) && player.CanStartCounterAttack())
        {
            player.skill.preciseDodge.CancelFollowUpAttack();
            player.TryStartCounterAttack();
            return true;
        }

        if (Input.GetKeyDown(KeyCode.R))
            player.skill.preciseDodge.TryUseFollowUpAttack();

        return false;
    }

    private void ChangeToLocomotionState()
    {
        if (player.IsGroundDetected())
            stateMachine.ChangeState(player.idleState);
        else
            stateMachine.ChangeState(player.airState);
    }

    private void ApplyMovement()
    {
        if (!player.skill.preciseDodge.CurrentFollowUpUsesFinisher &&
            elapsedTime <= player.skill.preciseDodge.CurrentFollowUpLungeDuration &&
            player.skill.preciseDodge.CurrentFollowUpLungeSpeed > 0)
        {
            rb.velocity = new Vector2(player.facingDir * player.skill.preciseDodge.CurrentFollowUpLungeSpeed, 0);
            return;
        }

        if (player.skill.preciseDodge.CurrentFollowUpUsesFinisher &&
            elapsedTime >= player.skill.preciseDodge.FollowUpFinisherStartTime)
        {
            if (!finisherMotionPrepared)
                PrepareFinisherMotion(player.skill.preciseDodge.FollowUpFinisherForwardDistance);

            float progress = Mathf.Clamp01((elapsedTime - player.skill.preciseDodge.FollowUpFinisherStartTime) / player.skill.preciseDodge.FollowUpFinisherDuration);
            MoveAlongFinisherArc(progress);

            return;
        }

        player.SetZeroVelocity();
    }

    private void PrepareFinisherMotion(float forwardDistance)
    {
        finisherMotionPrepared = true;
        finisherStartPosition = rb.position;
        finisherTargetPosition = FindLandingPosition(forwardDistance);
    }

    private Vector2 FindLandingPosition(float forwardDistance)
    {
        Vector2 start = rb.position;
        Vector2 currentGround;
        float rootGroundOffset = 0;

        if (player.TryGetGroundPointBelow(start + Vector2.up * 2f, 5f, out currentGround))
            rootGroundOffset = start.y - currentGround.y;

        Vector2 landingProbe = start + Vector2.right * player.facingDir * forwardDistance + Vector2.up * 4f;
        Vector2 landingGround;

        if (player.TryGetGroundPointBelow(landingProbe, 9f, out landingGround))
            return new Vector2(landingGround.x, landingGround.y + rootGroundOffset);

        return start + Vector2.right * player.facingDir * forwardDistance;
    }

    private void MoveAlongFinisherArc(float progress)
    {
        float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);
        Vector2 nextPosition = Vector2.Lerp(finisherStartPosition, finisherTargetPosition, easedProgress);

        rb.MovePosition(nextPosition);

        if (progress >= 1f)
            player.SetZeroVelocity();
    }

    private void TryDealTimedHits()
    {
        if (hitTriggered || elapsedTime < player.skill.preciseDodge.GetCurrentFollowUpHitTime())
            return;

        player.skill.preciseDodge.DealFollowUpDamage();
        hitTriggered = true;
    }

    private void FinishSegment()
    {
        if (player.skill.preciseDodge.TryPrepareQueuedFollowUpSegment())
        {
            continuingToNextSegment = true;
            stateMachine.ChangeState(player.preciseDodgeAttackState);
            return;
        }

        player.skill.preciseDodge.CompleteFollowUpAttackSegment();

        ChangeToLocomotionState();
    }

    private string GetAnimationStateName()
    {
        int index = Mathf.Clamp(player.skill.preciseDodge.CurrentFollowUpComboIndex, 0, AnimationStateNames.Length - 1);
        return AnimationStateNames[index];
    }
}
