using UnityEngine;

public class ChaosWolfLordAnimationTriggers : MonoBehaviour
{
    private Enemy_ChaosWolfLord wolfLord;
    private AudioEventPlayer audioEvents;

    private void Awake()
    {
        wolfLord = GetComponentInParent<Enemy_ChaosWolfLord>();
        audioEvents = GetComponentInParent<AudioEventPlayer>();
    }

    private Enemy_ChaosWolfLord WolfLord
    {
        get
        {
            if (wolfLord == null)
                wolfLord = GetComponentInParent<Enemy_ChaosWolfLord>();

            return wolfLord;
        }
    }

    private void AnimationTrigger()
    {
        WolfLord?.AnimationFinishTrigger();
    }

    private void AttackTrigger()
    {
        PlayAudioCue("attack");
        WolfLord?.AnimationEvent_AttackHit();
    }

    private void Attack1Trigger()
    {
        PlayAudioCue("attack_1");
        WolfLord?.AnimationEvent_Attack1Hit();
    }

    private void Attack2Trigger()
    {
        PlayAudioCue("attack_2");
        WolfLord?.AnimationEvent_Attack2Hit();
    }

    private void Attack3Trigger()
    {
        PlayAudioCue("attack_3");
        WolfLord?.AnimationEvent_Attack3Hit();
    }

    private void AttackNumberTrigger(int attackNumber)
    {
        PlayAudioCue("attack_" + attackNumber);
        WolfLord?.AnimationEvent_AttackHitByNumber(attackNumber);
    }

    private void PlayAudioCue(string cueName)
    {
        if (audioEvents == null)
            audioEvents = GetComponentInParent<AudioEventPlayer>();

        audioEvents?.PlayCue(cueName);
    }
}
