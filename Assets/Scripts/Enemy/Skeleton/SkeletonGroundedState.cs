using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonGroundedState : EnemyState
{
    protected Enemy_Skeleton enemy;
    protected Transform player;
    public SkeletonGroundedState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy) : base(_enemyBase, _stateMachine, _animBoolName)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        Player currentPlayer = PlayerManager.instance != null ? PlayerManager.instance.player : null;
        player = currentPlayer != null ? currentPlayer.transform : null;

    }
    public override void Update()
    {
        base.Update();

        if(player != null && enemy.CanDetectPlayer() && (enemy.IsPlayerDetected() || Vector2.Distance(enemy.transform.position,player.position)<2))
            stateMachine.ChangeState(enemy.battleState);
    }

    public override void Exit()
    {
        base.Exit();
    }

}
