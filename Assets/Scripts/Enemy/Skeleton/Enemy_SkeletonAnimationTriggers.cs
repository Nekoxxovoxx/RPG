using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_SkeletonAnimationTriggers : MonoBehaviour
{
    private Enemy_Skeleton enemy => GetComponentInParent<Enemy_Skeleton>();
    private AudioEventPlayer audioEvents;


    private void Awake()
    {
        audioEvents = GetComponentInParent<AudioEventPlayer>();
    }

    private void AnimationTrigger()
    {
        enemy.AnimationFinishTrigger();
    }

    private void AttackTrigger()
    {
        if (!enemy.CanDetectPlayer())
            return;

        PlayAudioCue("attack");

        Collider2D[] colliders = Physics2D.OverlapCircleAll(enemy.attackCheak.position, enemy.attackCheakRidus);

        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Player>() != null)
            {
                Player player = hit.GetComponent<Player>();

                if (player.TryStartPreciseDodge(enemy.transform))
                    return;

                PlayerStats target = hit.GetComponent<PlayerStats>();

                if (target == null || target.isDead)
                    return;

                enemy.stats.DoDamage(target);
            }
        }

    }

  

    private IEnumerator CloseAttackWindowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Enemy.isAnyEnemyAttacking = false;
    }

    private void OpenCounterWindow() => enemy.OpenCounterAttackWindow();
    private void CloseCounterWindow() => enemy.CloseCounterAttackWindow();

    private void PlayAudioCue(string cueName)
    {
        if (audioEvents == null)
            audioEvents = GetComponentInParent<AudioEventPlayer>();

        audioEvents?.PlayCue(cueName);
    }
}
