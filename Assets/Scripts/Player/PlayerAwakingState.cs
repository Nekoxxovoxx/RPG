using System.Collections;
using UnityEngine;

public class PlayerAwakingState : PlayerState
{
    private Coroutine awakingRoutine;

    public PlayerAwakingState(Player player, PlayerStateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        rb = player.rb;
        triggerCalled = false;
        player.SetOpeningAwakingActive(true);

        awakingRoutine = player.StartCoroutine(AwakingRoutine());
    }

    public override void Update()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;

        if (player.anim != null && rb != null)
            player.anim.SetFloat("yVelocity", 0f);
    }

    public override void Exit()
    {
        if (awakingRoutine != null)
        {
            player.StopCoroutine(awakingRoutine);
            awakingRoutine = null;
        }

        player.SetOpeningAwakingActive(false);
        player.PlayAnimatorStateIfExists("plyerIdle", 0f);
    }

    private IEnumerator AwakingRoutine()
    {
        yield return player.PlayOpeningAwakingSequence();

        awakingRoutine = null;

        if (stateMachine.currentState == this)
            stateMachine.ChangeState(player.idleState);
    }
}
