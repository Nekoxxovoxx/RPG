using UnityEngine;

public class PlayerJumpAttackState : PlayerState
{
    public PlayerJumpAttackState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.PlayAudioCue("jump_attack");
        player.ConsumeJumpAttack();
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

        if (xInput != 0)
            player.SetVelocity(player.moveSpeed * 0.8f * xInput, rb.velocity.y);

        if (!triggerCalled)
            return;

        if (player.IsGroundDetected())
            stateMachine.ChangeState(player.idleState);
        else
            stateMachine.ChangeState(player.airState);
    }
}
