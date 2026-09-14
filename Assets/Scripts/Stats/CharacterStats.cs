using System.Collections;
using UnityEngine;


public enum StatType
{
    力量,
    敏捷,
    智慧,
    活力,
    攻击力,
    暴击几率,
    暴击伤害,
    生命,
    防御,
    闪避,
    魔法抗性,
    火焰伤害,
    冰霜伤害,
    雷电伤害,
}
public class CharacterStats : MonoBehaviour
{
    private EntityFX fx;
    private AudioEventPlayer audioEvents;

    [Header("主要属性")]
    public Stat strength; // 每1点力量，增加1点物理伤害和1%暴击伤害
    public Stat agility;  // 每1点敏捷，增加1%闪避和1%暴击率
    public Stat intelligence; // 每1点智慧，增加3点魔法伤害和3点魔法抗性
    public Stat vitality; // 每1点活力，增加3点最大生命值和3点护甲

    [Header("攻击属性")]
    public Stat damage;       // 基础伤害
    public Stat critChance;   // 暴击几率
    public Stat critPower;    // 暴击伤害  默认值150%

    [Header("防御属性")]
    public Stat maxHealth;  // 最大生命值
    public Stat armor;      // 护甲
    public Stat evasion;    // 闪避
    public Stat magicResistance; // 魔法抗性

    [Header("元素/魔法伤害")]
    public Stat fireDamage;    // 火焰伤害
    public Stat iceDamage;     // 冰霜伤害
    public Stat lightingDamage;// 雷电伤害

    public bool isIgnited;  // 燃烧（异常状态）
    public bool isChilled;   // 冰冷减速20%（异常状态）
    public bool isShocked;   // 感电增加20%（异常状态）

    [SerializeField] private float ailmentsDuration = 4;
    private float ignitedTimer;
    private float chilledTimer;
    private float shockedTimer;

    private float igniteDamageTimer;
    private float igniteDamageCoolDown = .3f;
    private int igniteDamage;
    [SerializeField] private GameObject shockStrikePrefab;
    private int shockDamage;
    public int currentHealth; // 当前生命值

    private Coroutine incomingDamageMultiplierRoutine;
    private float incomingDamageMultiplier = 1f;

    public System.Action onHealthChanged;
    public bool isDead { get; private set; }

    protected virtual void Start()
    {
        critPower.SetDefaultValue(150);
        currentHealth = GetMaxHealthValue();
        fx = GetComponent<EntityFX>();
        audioEvents = GetComponent<AudioEventPlayer>();

        if (audioEvents == null)
            audioEvents = GetComponentInChildren<AudioEventPlayer>();

        onHealthChanged?.Invoke();
    }

    protected virtual void Update()
    {
        ignitedTimer -= Time.deltaTime;
        chilledTimer -= Time.deltaTime;
        shockedTimer -= Time.deltaTime;

        igniteDamageTimer -= Time.deltaTime;


        if (ignitedTimer < 0)
            isIgnited = false;

        if (chilledTimer < 0)
            isChilled = false;

        if (shockedTimer < 0)
            isShocked = false;

        if (isIgnited)
            ApplyIgniteDamage();
    }

    public virtual void IncreaseStatBy(int _modifier, float _duration, Stat _statToModify)
    {
        StartCoroutine(StatModCoroutine(_modifier, _duration, _statToModify));
    }

    private IEnumerator StatModCoroutine(int _modifier, float _duration, Stat _statToModify)
    {
        _statToModify.AddModifier(_modifier);

        yield return new WaitForSeconds(_duration); 

        _statToModify.RemoveModifier(_modifier);
    }

    public virtual void DoDamage(CharacterStats _targetStats)
    {
        if (_targetStats == null || _targetStats.isDead)
            return;

        if (TargetCanAvoidAttack(_targetStats))
            return;

        int totalDamage = damage.GetValue() + strength.GetValue();

        if (CanCrit())
        {
            totalDamage = CalculateCriticalDamage(totalDamage);
        }

        totalDamage = CheckTargetArmor(_targetStats, totalDamage);
        _targetStats.TakeDamage(totalDamage);

        DoMagicalDamage(_targetStats);
    }

    #region 魔法伤害和元素
    public virtual void DoMagicalDamage(CharacterStats _targetStats)
    {
        if (_targetStats == null || _targetStats.isDead)
            return;

        int _fireDamage = fireDamage.GetValue();
        int _iceDamage = iceDamage.GetValue();
        int _lightingDamage = lightingDamage.GetValue();

        int totalMagicalDamage = _fireDamage + _iceDamage + _lightingDamage + intelligence.GetValue();

        totalMagicalDamage = CheckTargetResistance(_targetStats, totalMagicalDamage);
        _targetStats.TakeDamage(totalMagicalDamage);

        if (Mathf.Max(_fireDamage, _iceDamage, _lightingDamage) <= 0)
            return;



        AttemptryToApplyAilements(_targetStats, _fireDamage, _iceDamage, _lightingDamage);

    }

    private void AttemptryToApplyAilements(CharacterStats _targetStats, int _fireDamage, int _iceDamage, int _lightingDamage)
    {
        bool canApplyIgnite = _fireDamage > _iceDamage && _fireDamage > _lightingDamage;
        bool canApplyChill = _iceDamage > _fireDamage && _iceDamage > _lightingDamage;
        bool canApplyShock = _lightingDamage > _fireDamage && _lightingDamage > _iceDamage;

        while (!canApplyIgnite && !canApplyChill && !canApplyShock)
        {
            if (Random.value < .3f && _fireDamage > 0)
            {
                canApplyIgnite = true;
                _targetStats.ApplyAilments(canApplyIgnite, canApplyChill, canApplyShock);
                return;
            }

            if (Random.value < .5f && _iceDamage > 0)
            {
                canApplyChill = true;
                _targetStats.ApplyAilments(canApplyIgnite, canApplyChill, canApplyShock);
                return;
            }

            if (Random.value < .5f && _lightingDamage > 0)
            {
                canApplyShock = true;
                _targetStats.ApplyAilments(canApplyIgnite, canApplyChill, canApplyShock);
                return;
            }
        }

        if (canApplyIgnite)
            _targetStats.SetupIngniteDamage(Mathf.RoundToInt(_fireDamage * .2f));

        if (canApplyShock)
            _targetStats.SetupShockStrikeDamage(Mathf.RoundToInt(_lightingDamage * .1f));

        _targetStats.ApplyAilments(canApplyIgnite, canApplyChill, canApplyShock);
        return;
    }



    public void ApplyAilments(bool _ignite, bool _chill, bool _shock)
    {
        bool canApplyIgnite = !isIgnited && !isChilled && !isShocked;
        bool canApplyChill = !isIgnited && !isChilled && !isShocked;
        bool canApplyShock = !isIgnited && !isChilled;

        if (_ignite && canApplyIgnite)
        {
            isIgnited = _ignite;
            ignitedTimer = ailmentsDuration;

            fx.IgniteFxFor(ailmentsDuration);

        }
        if (_chill && canApplyChill)
        {
            chilledTimer = ailmentsDuration;
            isChilled = _chill;

            float slowPercentage = 0.2f;

            GetComponent<Entity>().SlowEntityBy(slowPercentage, ailmentsDuration);
            fx.ChillFxFor(ailmentsDuration);
        }
        if (_shock && canApplyShock)
        {
            if (!isShocked)
            {
                ApplyShock(_shock);
            }
            else
            {
                if (GetComponent<Player>() != null)
                    return;
                HitNearestTargetWithShockStrike();
            }
        }
    }

    public void ApplyShock(bool _shock)
    {
        if (isShocked)
            return;

        shockedTimer = ailmentsDuration;
        isShocked = _shock;

        fx.ShockFxFor(ailmentsDuration);
    }

    private void HitNearestTargetWithShockStrike()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 25);

        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Enemy>() != null && Vector2.Distance(transform.position, hit.transform.position) > 1)
            {
                float distanceToEnemy = Vector2.Distance(transform.position, hit.transform.position);

                if (distanceToEnemy < closestDistance)
                {
                    closestDistance = distanceToEnemy;
                    closestEnemy = hit.transform;
                }
            }
            if (closestEnemy == null)
                closestEnemy = transform;
        }



        if (closestEnemy != null)
        {
            GameObject newShockStrike = Instantiate(shockStrikePrefab, transform.position, Quaternion.identity);

            newShockStrike.GetComponent<ShockStrike_Controller>().Setup(shockDamage, closestEnemy.GetComponent<CharacterStats>());
        }
    }
    private void ApplyIgniteDamage()
    {
        if (igniteDamageTimer < 0)
        {
            DecreaseHealthByStatusDamage(igniteDamage);

            if (currentHealth <= 0 && !isDead)
                Die();

            igniteDamageTimer = igniteDamageCoolDown;
        }
    }

    public void SetupIngniteDamage(int _damage) => igniteDamage = _damage;
    public void SetupShockStrikeDamage(int _damage) => shockDamage = _damage;
    #endregion
    public virtual void TakeDamage(int _damage)
    {
        _damage = ApplyIncomingDamageMultiplier(_damage);
        Debug.Log("受到伤害: " + _damage);
        bool willDie = _damage > 0 && currentHealth - _damage <= 0 && !isDead;

        DecreaseHealthBy(_damage);

        if (_damage > 0)
            PlayAudioCue(willDie ? "death" : "hurt");

        if (currentHealth <= 0 && !isDead)
            Die();
    }

    public virtual void IncreaseHealthBy(int _amount)
    {
        currentHealth += _amount;

        if (currentHealth > GetMaxHealthValue())
            currentHealth = GetMaxHealthValue();

        if(onHealthChanged != null)
            onHealthChanged();
    }

    public virtual void IncreaseCurrentHealthByMaxHealthDelta(int previousMaxHealth)
    {
        int newMaxHealth = GetMaxHealthValue();
        int healthIncrease = Mathf.Max(0, newMaxHealth - Mathf.Max(0, previousMaxHealth));

        if (healthIncrease > 0)
            currentHealth += healthIncrease;

        currentHealth = Mathf.Clamp(currentHealth, 0, newMaxHealth);
        onHealthChanged?.Invoke();
    }

    public virtual void ApplyIncomingDamageMultiplier(float multiplier, float duration)
    {
        multiplier = Mathf.Max(1f, multiplier);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f || multiplier <= 1f)
            return;

        if (incomingDamageMultiplierRoutine != null)
            StopCoroutine(incomingDamageMultiplierRoutine);

        incomingDamageMultiplierRoutine = StartCoroutine(IncomingDamageMultiplierCoroutine(multiplier, duration));
    }

    private IEnumerator IncomingDamageMultiplierCoroutine(float multiplier, float duration)
    {
        incomingDamageMultiplier = multiplier;

        yield return new WaitForSeconds(duration);

        incomingDamageMultiplier = 1f;
        incomingDamageMultiplierRoutine = null;
    }

    private int ApplyIncomingDamageMultiplier(int damage)
    {
        if (damage <= 0 || incomingDamageMultiplier <= 1f)
            return damage;

        return Mathf.Max(1, Mathf.RoundToInt(damage * incomingDamageMultiplier));
    }

    protected virtual void DecreaseHealthBy(int _damage)
    {
        currentHealth -= _damage;


        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.DamageImpact();
        }

        if (fx != null)
        {
            StartCoroutine(fx.FlashFX());
        }

        onHealthChanged?.Invoke();
    }

    protected virtual void DecreaseHealthByStatusDamage(int _damage)
    {
        currentHealth -= _damage;
        onHealthChanged?.Invoke();
    }

    protected virtual void Die()
    {
        isDead = true;
    }

    protected bool PlayAudioCue(string cueName)
    {
        if (audioEvents == null)
        {
            audioEvents = GetComponent<AudioEventPlayer>();

            if (audioEvents == null)
                audioEvents = GetComponentInChildren<AudioEventPlayer>();
        }

        return audioEvents != null && audioEvents.PlayCue(cueName);
    }

    public virtual void ReviveWithHealth(int health)
    {
        int maxHealthValue = Mathf.Max(1, GetMaxHealthValue());
        isDead = false;
        currentHealth = Mathf.Clamp(health, 1, maxHealthValue);
        onHealthChanged?.Invoke();
    }

    public virtual void ReviveToFullHealth()
    {
        ReviveWithHealth(GetMaxHealthValue());
    }
    #region 状态计算
    private int CheckTargetArmor(CharacterStats _targetStats, int totalDamage)
    {
        int armor = _targetStats.armor.GetValue();

        if (_targetStats.isChilled)
            armor = Mathf.RoundToInt(armor * 0.8f);

        totalDamage -= armor;
        totalDamage = Mathf.Clamp(totalDamage, 0, int.MaxValue);
        return totalDamage;
    }
    private int CheckTargetResistance(CharacterStats _targetStats, int totalMagicalDamage)
    {
        totalMagicalDamage -= _targetStats.magicResistance.GetValue() + (_targetStats.intelligence.GetValue() * 3);
        totalMagicalDamage = Mathf.Clamp(totalMagicalDamage, 0, int.MaxValue);
        return totalMagicalDamage;
    }

    private bool TargetCanAvoidAttack(CharacterStats _targetStats)
    {
        int totalEvasion = _targetStats.evasion.GetValue() + _targetStats.agility.GetValue();

        if (isShocked)
            totalEvasion += 20;

        if (Random.Range(0, 100) < totalEvasion)
        {
            EntityFX targetFx = _targetStats.GetComponent<EntityFX>();
            if (targetFx != null)
                targetFx.CreateDodgeText();
            return true;
        }
        return false;
    }

    private bool CanCrit()
    {
        int totalCriticalChance = critChance.GetValue() + agility.GetValue();

        if (Random.Range(0, 100) <= totalCriticalChance)
        {
            return true;
        }
        return false;
    }

    private int CalculateCriticalDamage(int _damage)
    {
        float totalCritPower = (critPower.GetValue() + strength.GetValue()) * .01f;
        Debug.Log("total crit power %" + totalCritPower);

        float critDamage = _damage * totalCritPower;
        Debug.Log("crit damage before round up " + critDamage);

        return Mathf.RoundToInt(critDamage);
    }

    public int GetMaxHealthValue()
    {
        return maxHealth.GetValue() + vitality.GetValue() * 5;
    }
    #endregion
    public Stat GetStat(StatType _statType)
    {
        if (_statType == StatType.力量) return strength;
        else if (_statType == StatType.敏捷) return agility;
        else if (_statType == StatType.智慧) return intelligence;
        else if (_statType == StatType.活力) return vitality;
        else if (_statType == StatType.攻击力) return damage;
        else if (_statType == StatType.暴击几率) return critChance;
        else if (_statType == StatType.暴击伤害) return critPower;
        else if (_statType == StatType.生命) return maxHealth;
        else if (_statType == StatType.防御) return armor;
        else if (_statType == StatType.闪避) return evasion;
        else if (_statType == StatType.魔法抗性) return magicResistance;
        else if (_statType == StatType.火焰伤害) return fireDamage;
        else if (_statType == StatType.冰霜伤害) return iceDamage;
        else if (_statType == StatType.雷电伤害) return lightingDamage;

        return null;
    }
}
