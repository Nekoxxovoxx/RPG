using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDashState : PlayerState
{
    public PlayerDashState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.PlayAudioCue("dash");
        stateTimer = player.dashDuration;
    }

    public override void Exit()
    {
        base.Exit();

        player.SetVelocity(0,rb.velocity.y);

    }

    public override void Update()
    {
        base.Update();

        if (player.ShouldEnterWallSlide(player.dashDir))
        {
            stateMachine.ChangeState(player.wallSlide);
            return;
        }

        player.SetVelocity(player.dashSpeed * player.dashDir, 0);


        if (stateTimer < 0)
        {
            if (player.IsGroundDetected())
                stateMachine.ChangeState(player.idleState);
            else
                stateMachine.ChangeState(player.airState);
        }
    }
}
