using UnityEngine;

public class PlayerHealingState : PlayerGroundState
{
    private int flaskIndex = 0; // 0=主药瓶, 1-4=副药瓶

    public void SetFlaskIndex(int index)
    {
        flaskIndex = Mathf.Clamp(index, 0, 4);
    }

    public PlayerHealingState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
        : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 检查对应药瓶是否可用
        bool canUse = false;
        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();

        if (flask != null)
        {
            if (flaskIndex == 0)
                canUse = flask.CanUseMainFlask();
            else
                canUse = flask.CanUseSubFlask(flaskIndex - 1);
        }

        if (!canUse)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        player.SetZeroVelocity();
        player.canHeal = true;
    }

    public override void Exit()
    {
        base.Exit();
        flaskIndex = 0;
    }

    public override void Update()
    {
        base.Update();

        // 动画播放结束 → 回 Idle
        if (triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 移动打断
        if (xInput != 0)
        {
            stateMachine.ChangeState(player.moveState);
            return;
        }

        // 跳跃打断
        if (Input.GetKeyDown(KeyCode.Space))
        {
            stateMachine.ChangeState(player.jumpState);
            return;
        }

        // 攻击打断
        if (Input.GetMouseButtonDown(0))
        {
            stateMachine.ChangeState(player.primaryAttack);
            return;
        }

        // 冲刺打断
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            stateMachine.ChangeState(player.dashState);
        }
    }
}
