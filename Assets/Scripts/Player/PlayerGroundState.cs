using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerGroundState : PlayerState
{
    public PlayerGroundState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.ResetExtraJumps();
        player.ResetJumpAttack();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (Input.GetKeyDown(KeyCode.Mouse1) &&
            IsSkillUnlocked(SkillManager.SkillIds.Sword) &&
            HasNoSword())
            stateMachine.ChangeState(player.aimSword);

        if (Input.GetKeyDown(KeyCode.Q) && player.TryStartCounterAttack())
            return;

        if (Input.GetKeyDown(KeyCode.T) &&
            IsSkillUnlocked(SkillManager.SkillIds.Sun) &&
            player.skill != null &&
            player.skill.sun != null &&
            player.skill.sun.IsReady())
        {
            stateMachine.ChangeState(player.sunSkillState);
            return;
        }

        if(!player.IsGroundDetected())
        {
            if (player.HasJumpAttackInput())
                stateMachine.ChangeState(player.jumpAttackState);
            else
                stateMachine.ChangeState(player.airState);

            return;
        }

        if(Input.GetKeyDown(KeyCode.Mouse0) && !UI_InputBlocker.ShouldBlockPlayerMouseInput())
        {
            stateMachine.ChangeState(player.primaryAttack);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space)&& player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.jumpState);
            return;
        }

        // 主药瓶 (Key 1)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (TryUseMainFlask())
                return;
        }

        // 副药瓶 (Keys 2-5)
        if (Input.GetKeyDown(KeyCode.Alpha2)) { if (TryUseSubFlask(0)) return; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { if (TryUseSubFlask(1)) return; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { if (TryUseSubFlask(2)) return; }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { if (TryUseSubFlask(3)) return; }
    }

    private bool TryUseMainFlask()
    {
        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();
        if (flask == null) return false;

        if (!flask.CanUseMainFlask())
        {
            Debug.Log("不能使用主药瓶（充能耗尽、冷却中或满血）");
            return false;
        }

        player.healState.SetFlaskIndex(0);
        stateMachine.ChangeState(player.healState);
        return true;
    }

    private bool TryUseSubFlask(int index)
    {
        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();
        if (flask == null) return false;

        if (!flask.CanUseSubFlask(index))
        {
            Debug.Log("不能使用副药瓶槽位 " + (index + 2) + "（无物品、库存耗尽或冷却中）");
            return false;
        }

        player.healState.SetFlaskIndex(index + 1);
        stateMachine.ChangeState(player.healState);
        return true;
    }

    private bool HasNoSword()
    {
        if(!player.sword)
        {
            return true;
        }

        player.sword.GetComponent<Sword_Skill_Contorller>().ReturnSword(); 
        return false ;
    }

    private bool IsSkillUnlocked(string skillId)
    {
        return player != null && player.IsSkillUnlocked(skillId);
    }
}
