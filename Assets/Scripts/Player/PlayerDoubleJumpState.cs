using UnityEngine;

public class PlayerDoubleJumpState : PlayerState
{
    public PlayerDoubleJumpState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.PlayAudioCue("double_jump");
        player.ConsumeExtraJump();
        rb.velocity = new Vector2(rb.velocity.x, player.GetDoubleJumpForce());
    }

    public override void Update()
    {
        base.Update();

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

        if (xInput != 0)
            player.SetVelocity(player.moveSpeed * 0.8f * xInput, rb.velocity.y);

        if (rb.velocity.y < 0)
            stateMachine.ChangeState(player.airState);
    }
}
