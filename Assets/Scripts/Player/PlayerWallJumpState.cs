using UnityEngine;

public class PlayerWallJumpState : PlayerState
{
    public PlayerWallJumpState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.PlayAudioCue("wall_jump");
        stateTimer = 1f;
        player.SetVelocity(5 * -player.facingDir, player.jumpForce);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Space) && player.CanDoubleJump())
        {
            stateMachine.ChangeState(player.doubleJumpState);
            return;
        }

        if (player.ShouldEnterWallSlide(xInput))
        {
            stateMachine.ChangeState(player.wallSlide);
            return;
        }

        if (player.HasJumpAttackInput())
        {
            stateMachine.ChangeState(player.jumpAttackState);
            return;
        }

        if (stateTimer < 0)
        {
            stateMachine.ChangeState(player.airState);
            return;
        }

        if (player.IsGroundDetected())
            stateMachine.ChangeState(player.idleState);
    }
}
