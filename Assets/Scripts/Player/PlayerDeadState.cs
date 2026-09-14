using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void AnimationFinishTrigger()
    {
        base.AnimationFinishTrigger();
        player.NotifyDeathAnimationFinished();
    }

    public override void Enter()
    {
        base.Enter();
        player.BeginDeathAnimationWait(animBoolName);
    }

    public override void Exit()
    {
        base.Exit();
        player.CancelDeathAnimationWait();
    }

    public override void Update()
    {
        base.Update();

        player.SetZeroVelocity();
    }
}
