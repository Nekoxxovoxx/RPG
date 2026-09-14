using UnityEngine;

public class DemonBossAnimationTriggers : MonoBehaviour
{
    private Enemy_DemonBoss boss;

    private void Awake()
    {
        boss = GetComponentInParent<Enemy_DemonBoss>();
    }

    private Enemy_DemonBoss Boss
    {
        get
        {
            if (boss == null)
                boss = GetComponentInParent<Enemy_DemonBoss>();

            return boss;
        }
    }

    private void AnimationTrigger()
    {
        Boss?.AnimationEvent_AttackEnd();
    }

    private void AttackEndTrigger()
    {
        Boss?.AnimationEvent_AttackEnd();
    }

    private void CleaveTrigger()
    {
        Boss?.AnimationEvent_CleaveHit();
    }

    private void SmashTrigger()
    {
        Boss?.AnimationEvent_SmashHit();
    }

    private void FireBreathStart()
    {
        Boss?.AnimationEvent_FireBreathStart();
    }

    private void FireBreathEnd()
    {
        Boss?.AnimationEvent_FireBreathEnd();
    }

    private void CastSpellTrigger()
    {
        Boss?.AnimationEvent_CastSpell();
    }

    private void TransformFinish()
    {
        Boss?.AnimationEvent_TransformFinished();
    }
}
