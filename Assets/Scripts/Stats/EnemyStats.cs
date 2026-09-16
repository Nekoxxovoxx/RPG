using UnityEngine;

public class EnemyStats : CharacterStats
{
    public static event System.Action<EnemyStats> AnyEnemyDied;
    public static event System.Action<EnemyStats> AnyEnemyKilledByPlayer;

    private Enemy enemy;
    private EnemyEmberDrop emberDropSystem;
    private bool lastDamageWasPlayerCaused;
    private bool statusDamageSourceIsPlayer;

    [Header("敌人等级与等阶")]
    [SerializeField] private EnemyKind kind = EnemyKind.骷髅敌人;
    [SerializeField] private bool useManagerSettings = true;
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField] private EnemyRank rank = EnemyRank.普通敌人;

    [Range(0, 1f)]
    [SerializeField] private float percantageModifier = .4f;

    public EnemyKind Kind => kind;
    public bool UseManagerSettings => useManagerSettings;
    public int Level => Mathf.Max(1, level);
    public EnemyRank Rank => rank;
    public bool WasKilledByPlayer { get; private set; }

    protected override void Start()
    {
        EnsureCoreStatObjects();
        ApplyManagedLevelSettings();
        ApplyLevelModifiers();

        base.Start();

        enemy = GetComponent<Enemy>();
        emberDropSystem = GetComponent<EnemyEmberDrop>();

        if (emberDropSystem == null)
            emberDropSystem = gameObject.AddComponent<EnemyEmberDrop>();
    }

    public void ApplyManagedLevelSettings()
    {
        if (!useManagerSettings)
            return;

        EnemyLevelManager.GetOrCreate().ApplySettingsTo(this);
    }

    public void SetManagedLevelAndRank(int managedLevel, EnemyRank managedRank)
    {
        level = Mathf.Max(1, managedLevel);
        rank = managedRank;
    }

    public void SetEnemyKind(EnemyKind enemyKind)
    {
        kind = enemyKind;
    }

    private void ApplyLevelModifiers()
    {
        Modify(strength);
        Modify(agility);
        Modify(intelligence);
        Modify(vitality);

        Modify(damage);

        Modify(maxHealth);
        Modify(armor);
        Modify(magicResistance);

        Modify(fireDamage);
        Modify(iceDamage);
        Modify(lightingDamage);
    }

    private void Modify(Stat _stat)
    {
        if (_stat == null)
            return;

        for (int i = 1; i < Level; i++)
        {
            float modifier = _stat.GetValue() * percantageModifier;

            _stat.AddModifier(Mathf.RoundToInt(modifier));
        }
    }

    public override void TakeDamage(int _damage)
    {
        if (_damage > 0)
            lastDamageWasPlayerCaused = DamageAttribution.IsPlayerDamage;

        base.TakeDamage(_damage);
    }

    protected override void DecreaseHealthByStatusDamage(int _damage)
    {
        if (_damage > 0)
            lastDamageWasPlayerCaused = statusDamageSourceIsPlayer || DamageAttribution.IsPlayerDamage;

        base.DecreaseHealthByStatusDamage(_damage);
    }

    public void MarkPlayerDamageSource()
    {
        lastDamageWasPlayerCaused = true;
    }

    public void MarkPlayerStatusDamageSource()
    {
        statusDamageSourceIsPlayer = true;
    }

    public void SetStatusDamageSource(bool causedByPlayer)
    {
        statusDamageSourceIsPlayer = causedByPlayer;
    }

    protected override void Die()
    {
        if (isDead)
            return;

        WasKilledByPlayer = lastDamageWasPlayerCaused;

        base.Die();

        enemy?.Die();

        EnemyDropManager.GetOrCreate().DropForEnemy(this);
        emberDropSystem?.DropEmbers(this);
        AnyEnemyDied?.Invoke(this);

        if (WasKilledByPlayer)
            AnyEnemyKilledByPlayer?.Invoke(this);
    }
}
