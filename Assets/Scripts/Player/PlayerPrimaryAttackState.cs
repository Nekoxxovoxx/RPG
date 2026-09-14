using UnityEngine;

public class PlayerPrimaryAttackState : PlayerState
{
    public int comboCounter { get; private set; }

    private float lastTimeAttacked;
    private float comboWindow = 2f;
    private bool queuedNextAttack;
    private bool currentAttackAwakened;
    private int currentMaxComboCounter;
    private float attackElapsedTime;
    private bool finisherMotionPrepared;
    private Vector2 finisherStartPosition;
    private Vector2 finisherTargetPosition;

    public PlayerPrimaryAttackState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        queuedNextAttack = false;
        attackElapsedTime = 0;
        finisherMotionPrepared = false;
        xInput = 0;
        currentAttackAwakened = IsAwakened();
        currentMaxComboCounter = currentAttackAwakened ? 5 : 2;

        if (comboCounter > currentMaxComboCounter || Time.time >= lastTimeAttacked + comboWindow)
            comboCounter = 0;

        player.anim.SetInteger("ComboCounter", currentAttackAwakened ? comboCounter + 3 : comboCounter);
        string comboCueName = "attack_" + (comboCounter + 1);

        if (!player.PlayAudioCue(comboCueName))
            player.PlayAudioCue("attack");

        float attackDir = player.facingDir;

        if (xInput != 0)
            attackDir = xInput;

        Vector2 attackVelocity = GetAttackMovement(comboCounter);
        player.SetVelocity(attackVelocity.x * attackDir, attackVelocity.y);

        stateTimer = 0.1f;
    }

    public override void Exit()
    {
        base.Exit();

        float busyTime = queuedNextAttack && currentAttackAwakened ? 0.02f : 0.1f;
        player.StartCoroutine("BusyFor", busyTime);

        comboCounter++;
        lastTimeAttacked = Time.time;
    }

    public override void Update()
    {
        base.Update();

        attackElapsedTime += Time.deltaTime;
        BufferNextAttackInput();

        bool finisherMotionApplied = ApplyAwakenedFinisherMotion();

        if (stateTimer < 0 && !finisherMotionApplied)
            player.SetZeroVelocity();

        if (!triggerCalled)
            return;

        if (queuedNextAttack && comboCounter < currentMaxComboCounter)
        {
            player.StartCoroutine(ContinueComboNextFrame());
            stateMachine.ChangeState(player.idleState);
            return;
        }

        stateMachine.ChangeState(player.idleState);
    }

    public void ResetCombo()
    {
        comboCounter = 0;
        lastTimeAttacked = -999f;
        queuedNextAttack = false;
    }

    private void BufferNextAttackInput()
    {
        if (comboCounter >= currentMaxComboCounter)
            return;

        if ((Input.GetKeyDown(KeyCode.Mouse0) || (currentAttackAwakened && Input.GetKey(KeyCode.Mouse0))) &&
            !UI_InputBlocker.ShouldBlockPlayerMouseInput())
            queuedNextAttack = true;
    }

    private System.Collections.IEnumerator ContinueComboNextFrame()
    {
        yield return null;

        if (player == null || player.stats == null || player.stats.isDead)
            yield break;

        if (!player.IsGroundDetected())
            yield break;

        if (player.stateMachine.currentState == player.primaryAttack)
            yield break;

        if (player.stateMachine.currentState != player.idleState && player.stateMachine.currentState != player.moveState)
            yield break;

        player.stateMachine.ChangeState(player.primaryAttack);
    }

    private bool IsAwakened()
    {
        return player.skill != null && player.skill.awakening != null && player.skill.awakening.IsAwakened;
    }

    private bool ApplyAwakenedFinisherMotion()
    {
        if (!currentAttackAwakened || comboCounter != currentMaxComboCounter || player.skill.awakening == null)
            return false;

        float startTime = player.skill.awakening.FinisherMotionStartTime;
        float duration = player.skill.awakening.FinisherMotionDuration;

        if (attackElapsedTime < startTime)
            return false;

        if (!finisherMotionPrepared)
            PrepareFinisherMotion(player.skill.awakening.FinisherForwardDistance);

        float progress = Mathf.Clamp01((attackElapsedTime - startTime) / duration);
        MoveAlongFinisherArc(progress);

        return progress < 1f;
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

    private Vector2 GetAttackMovement(int index)
    {
        if (player.attackMovement == null || player.attackMovement.Length == 0)
            return Vector2.zero;

        return player.attackMovement[Mathf.Clamp(index, 0, player.attackMovement.Length - 1)];
    }
}
