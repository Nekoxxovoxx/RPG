using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill : MonoBehaviour
{
    public  float cooldown;
    protected float cooldownTimer;

    protected Player player;
    protected virtual  void Start()
    {
        ResolvePlayerReference();
    }
    protected virtual void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public virtual bool IsReady() => cooldownTimer <= 0;
    public float CooldownRemaining => Mathf.Max(cooldownTimer, 0);
    public virtual float CooldownDisplayProgress => cooldown <= 0 ? 0f : Mathf.Clamp01(CooldownRemaining / cooldown);

    protected void StartCooldown() => cooldownTimer = cooldown;
    public virtual void ResetCooldown() => cooldownTimer = 0f;

    public virtual bool CanUseSkill()
    {
        if (!HasLivingPlayer())
            return false;

        if(IsReady())
        {
            UseSkill();
            StartCooldown();
            return true;
        }
        Debug.Log("技能正在冷却");
        return false;
        
    }

    public virtual void UseSkill()
    {

    }

    protected bool HasLivingPlayer()
    {
        ResolvePlayerReference();

        if (player == null || !player.isActiveAndEnabled)
            return false;

        CharacterStats playerStats = player.stats != null
            ? player.stats
            : player.GetComponent<CharacterStats>();

        return playerStats == null || !playerStats.isDead;
    }

    protected void ResolvePlayerReference()
    {
        if (player != null)
            return;

        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            player = PlayerManager.instance.player;

        if (player == null)
            player = FindObjectOfType<Player>();
    }

}
