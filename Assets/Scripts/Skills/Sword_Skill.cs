using System;
using UnityEngine;


public enum SwordType
{
    Regular,
    Bounce,
    Pierce,
    Spin
}
public class Sword_Skill : Skill
{
    public SwordType swordType = SwordType.Regular;

    [Header("Bounce Info")]
    [SerializeField] private int bounceAmount;
    [SerializeField] private float bounceGravity;
    [SerializeField] private float bounceSpeed;

    [Header("Peirce Info")]
    [SerializeField] private int pierceAmount;
    [SerializeField] private float pierceGravity;

    [Header("Spin Info")]
    [SerializeField] private float hitCooldown = 0.35f;
    [SerializeField] private float maxTraveDistance = 7f;
    [SerializeField] private float spinDuration = 2f;
    [SerializeField] private float SpinGravity = 1f;

    [Header("Skill Info")]
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private Vector2 launchForce;
    [SerializeField] private float swordGravity;
    [SerializeField] private float freezeTimeDuration;
    [SerializeField] private float returnSpeed;

    [Header("Skill Tree")]
    [SerializeField] private bool skillUnlocked;
    [SerializeField, Min(1f)] private float soulRiftDamageMultiplier = 1.1f;
    [SerializeField, Min(0f)] private float soulRiftDuration = 10f;
    [SerializeField, Min(0f)] private float timeStopOnHitDuration = 0.65f;

    private bool soulRiftUnlocked;
    private bool timeStopUnlocked;

    private Vector2 finalDir;

    [Header("Aim dots")]
    [SerializeField] private int numberOfDots;
    [SerializeField] private float spaceBeetwenDots;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private Transform dotsParent;

    private GameObject[] dots;

    protected override void Start()
    {
        base.Start();

        GenereateDots();

        SetupGraivity();
    }

    private void SetupGraivity()
    {
        if (swordType == SwordType.Bounce)
            swordGravity = bounceGravity;
        else if (swordType == SwordType.Pierce)
            swordGravity = pierceGravity;
        else if (swordType == SwordType.Spin)
            swordGravity = SpinGravity;
    }

    protected override void Update()
    {
        if (!skillUnlocked)
        {
            DotsActive(false);
            return;
        }

        if (!HasLivingPlayer())
        {
            DotsActive(false);
            return;
        }

        if (Input.GetKeyUp(KeyCode.Mouse1))
            finalDir = new Vector2(AimDirection().normalized.x * launchForce.x, AimDirection().normalized.y * launchForce.y);

        if (Input.GetKey(KeyCode.Mouse1))
        {
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i].transform.position = DotsPosition(i * spaceBeetwenDots);
            }
        }
    }

    public void CreatSword()
    {
        if (!skillUnlocked || !HasLivingPlayer() || swordPrefab == null)
            return;

        GameObject newSword = Instantiate(swordPrefab, player.transform.position, transform.rotation);
        Sword_Skill_Contorller newSwordScript = newSword.GetComponent<Sword_Skill_Contorller>();

        if (swordType == SwordType.Bounce)
            newSwordScript.SetupBounce(true, bounceAmount,bounceSpeed);
        else if (swordType == SwordType.Pierce)
            newSwordScript.SetupPierce(pierceAmount);
        else if (swordType == SwordType.Spin)
            newSwordScript.SetupSpin(true, maxTraveDistance, spinDuration,hitCooldown);


        newSwordScript.SetupSword(finalDir, swordGravity,player,freezeTimeDuration,returnSpeed);

        player.AssignNewSword(newSword);

        DotsActive(false);
    }

    public void ApplySkillTreeUnlocks(bool unlocked, SwordType unlockedSwordType, bool soulRift, bool timeStop)
    {
        skillUnlocked = unlocked;
        swordType = unlockedSwordType;
        soulRiftUnlocked = soulRift;
        timeStopUnlocked = timeStop;
        SetupGraivity();

        if (!skillUnlocked)
            DotsActive(false);
    }

    public void ApplySkillTreeHitEffects(Enemy enemy)
    {
        if (enemy == null)
            return;

        CharacterStats targetStats = enemy.GetComponent<CharacterStats>();

        if (soulRiftUnlocked && targetStats != null)
            targetStats.ApplyIncomingDamageMultiplier(soulRiftDamageMultiplier, soulRiftDuration);

        if (timeStopUnlocked && timeStopOnHitDuration > 0f)
            enemy.FreezeTimeFor(timeStopOnHitDuration);
    }

    #region Aim region
    public Vector2 AimDirection()
    {
        if (player == null || Camera.main == null)
            return Vector2.right;

        Vector2 playerPosition = player.transform.position;
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePosition - playerPosition;

        return direction;

    }

    public void DotsActive(bool _isActive)
    {
        if (dots == null)
            return;

        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null)
                dots[i].SetActive(_isActive);
        }
    }

    private void GenereateDots()
    {
        dots = new GameObject[numberOfDots];

        for (int i = 0; i < numberOfDots; i++)
        {
            dots[i] = Instantiate(dotPrefab, player.transform.position, Quaternion.identity, dotsParent);
            dots[i].SetActive(false);
        }

    }

    private Vector2 DotsPosition(float t)
    {
        Vector2 position = (Vector2)player.transform.position + new Vector2(
            AimDirection().normalized.x * launchForce.x,
            AimDirection().normalized.y * launchForce.y) * t + 0.5f * (Physics2D.gravity * swordGravity) * (t * t);

        return position;
    }
    #endregion
}

