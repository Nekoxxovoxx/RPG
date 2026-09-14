using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dash_Skill : Skill
{
    [SerializeField, Min(1)] private int maxCharges = 1;

    private int currentCharges = 1;

    public int CurrentCharges => currentCharges;
    public int MaxCharges => maxCharges;

    protected override void Start()
    {
        base.Start();
        currentCharges = Mathf.Clamp(currentCharges, 1, maxCharges);
    }

    protected override void Update()
    {
        if (currentCharges >= maxCharges)
        {
            cooldownTimer = 0f;
            return;
        }

        base.Update();

        if (cooldownTimer <= 0f)
        {
            currentCharges = Mathf.Min(currentCharges + 1, maxCharges);

            if (currentCharges < maxCharges)
                StartCooldown();
        }
    }

    public override bool IsReady()
    {
        return HasLivingPlayer() && currentCharges > 0;
    }

    public override float CooldownDisplayProgress
    {
        get
        {
            if (currentCharges > 0)
                return 0f;

            return base.CooldownDisplayProgress;
        }
    }

    public override bool CanUseSkill()
    {
        if (!HasLivingPlayer())
            return false;

        if (currentCharges <= 0)
        {
            Debug.Log("Dash skill is cooling down.");
            return false;
        }

        UseSkill();
        currentCharges = Mathf.Max(0, currentCharges - 1);

        if (currentCharges < maxCharges && cooldownTimer <= 0f)
            StartCooldown();

        return true;
    }

    public override void ResetCooldown()
    {
        base.ResetCooldown();
        currentCharges = maxCharges;
    }

    public void SetMaxCharges(int charges)
    {
        int previousMaxCharges = maxCharges;
        maxCharges = Mathf.Max(1, charges);

        if (maxCharges > previousMaxCharges && currentCharges >= previousMaxCharges)
            currentCharges = maxCharges;

        currentCharges = Mathf.Clamp(currentCharges, 0, maxCharges);

        if (currentCharges == 0 && cooldownTimer <= 0f)
            StartCooldown();
    }

    public override void UseSkill()
    {
        base.UseSkill();
    }
}
