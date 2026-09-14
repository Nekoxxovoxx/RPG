using UnityEngine;

public class PlayerSunSkillState : PlayerState
{
    private bool castStarted;
    private bool sunSpawned;
    private bool hasExited;

    public PlayerSunSkillState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
        : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        castStarted = player.skill != null
            && player.skill.sun != null
            && player.skill.sun.TryStartCast();

        if (!castStarted)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        sunSpawned = false;
        hasExited = false;
        stateTimer = player.skill.sun.CastDuration;
        player.SetZeroVelocity();
    }

    public override void Update()
    {
        base.Update();

        if (!castStarted)
            return;

        player.SetZeroVelocity();

        if (HasInterruptInput())
        {
            ExitToInputState();
            return;
        }

        if (!sunSpawned && (triggerCalled || stateTimer <= 0f))
        {
            SpawnSunAndHoldPose();
            return;
        }

        if (sunSpawned && stateTimer <= 0f)
            ExitToLocomotionState();
    }

    private void SpawnSunAndHoldPose()
    {
        player.skill.sun.SpawnSun(player);
        sunSpawned = true;
        triggerCalled = false;
        stateTimer = player.skill.sun.LifeTime;
    }

    private bool HasInterruptInput()
    {
        return HasMovementKeyDown()
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Mouse0)
            || Input.GetKeyDown(KeyCode.Mouse1)
            || (Input.GetKeyDown(KeyCode.Q) && player.CanStartCounterAttack())
            || Input.GetKeyDown(KeyCode.R)
            || Input.GetKeyDown(KeyCode.Alpha1)
            || Input.GetKeyDown(KeyCode.LeftShift)
            || Input.GetKeyDown(KeyCode.RightShift);
    }

    private bool HasMovementKeyDown()
    {
        return Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.D)
            || Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.S)
            || Input.GetKeyDown(KeyCode.LeftArrow)
            || Input.GetKeyDown(KeyCode.RightArrow)
            || Input.GetKeyDown(KeyCode.UpArrow)
            || Input.GetKeyDown(KeyCode.DownArrow);
    }

    private void ExitToInputState()
    {
        if (hasExited)
            return;

        hasExited = true;

        if (!player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.airState);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            stateMachine.ChangeState(player.jumpState);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            stateMachine.ChangeState(player.primaryAttack);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!player.TryStartCounterAttack())
                stateMachine.ChangeState(player.idleState);

            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1) &&
            player.sword == null &&
            player.IsSkillUnlocked(SkillManager.SkillIds.Sword))
        {
            stateMachine.ChangeState(player.aimSword);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)
            && Inventory.instance != null
            && Inventory.instance.CanUseFlask())
        {
            stateMachine.ChangeState(player.healState);
            return;
        }

        if (xInput != 0)
            stateMachine.ChangeState(player.moveState);
        else
            stateMachine.ChangeState(player.idleState);
    }

    private void ExitToLocomotionState()
    {
        if (hasExited)
            return;

        hasExited = true;

        if (player.IsGroundDetected())
            stateMachine.ChangeState(player.idleState);
        else
            stateMachine.ChangeState(player.airState);
    }
}
