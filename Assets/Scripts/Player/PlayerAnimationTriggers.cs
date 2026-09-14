using UnityEngine;

public class PlayerAnimationTriggers : MonoBehaviour
{

    private Player player => GetComponentInParent<Player>();
    private void AnimationTrigger()
    {
        player.AnimationTrigger();
    }
    private void AttackTrigger()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(player.attackCheak.position, player.attackCheakRidus);

        foreach(var hit in colliders)
        {
            if (hit.GetComponent<Enemy>() != null)
            {
                EnemyStats _target = hit.GetComponent<EnemyStats>();

                if(_target != null)
                player.stats.DoDamage(_target);

                ItemData_Equipment weaponData = Inventory.instance.GetEquipment(EquipmentType.Weapon);

                if (weaponData != null)
                    weaponData.Effect(_target.transform);
            }
        }
    }

    private void PreciseDodgeFollowUpAttackTrigger()
    {
        SkillManager.instance?.preciseDodge?.DealFollowUpDamage();
    }

    private void FootstepTrigger()
    {
        player.PlayAudioCue("footstep");
    }

    private void PlayAudioCue(string cueName)
    {
        player.PlayAudioCue(cueName);
    }

    private void ThrowSword()
    {
        if (SkillManager.instance != null &&
            SkillManager.instance.IsSkillUnlocked(SkillManager.SkillIds.Sword))
        {
            SkillManager.instance.sword?.CreatSword();
        }
    }
}
