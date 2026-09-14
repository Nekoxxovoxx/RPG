using UnityEngine;

public class PlayerPreciseDodgeState : PlayerState
{
    public PlayerPreciseDodgeState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = player.skill.preciseDodge.DodgeDuration;
        player.skill.preciseDodge.BeginDodgeState();
    }

    public override void Exit()
    {
        base.Exit();

        rb.velocity = new Vector2(0, rb.velocity.y);
        player.skill.preciseDodge.EndDodgeState();
    }

    public override void Update()
    {
        base.Update();

        rb.velocity = new Vector2(player.skill.preciseDodge.DodgeSpeed * player.skill.preciseDodge.DodgeDirection, 0);

        if (stateTimer < 0)
        {
            if (player.skill.preciseDodge.TryStartQueuedFollowUpAttack())
                return;

            if (player.IsGroundDetected())
                stateMachine.ChangeState(player.idleState);
            else
                stateMachine.ChangeState(player.airState);
        }
    }
}
