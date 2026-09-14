using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAirState : PlayerState
{
    public PlayerAirState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if(player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if(player.ShouldEnterWallSlide(xInput))
        {
            stateMachine.ChangeState(player.wallSlide);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && player.CanDoubleJump())
        {
            stateMachine.ChangeState(player.doubleJumpState);
            return;
        }

        if (player.HasJumpAttackInput())
        {
            stateMachine.ChangeState(player.jumpAttackState);
            return;
        }

        if (xInput != 0)
            player.SetVelocity(player.moveSpeed * 0.8f * xInput, rb.velocity.y);
    }
}
