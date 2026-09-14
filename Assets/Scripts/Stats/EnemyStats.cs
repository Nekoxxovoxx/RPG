using UnityEngine;

public class EnemyStats : CharacterStats
{
    private Enemy enemy;
    private EnemyEmberDrop emberDropSystem;

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

    protected override void Start()
    {
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
        base.TakeDamage(_damage);
    }

    protected override void Die()
    {
        base.Die();

        enemy?.Die();

        EnemyDropManager.GetOrCreate().DropForEnemy(this);
        emberDropSystem?.DropEmbers(this);
    }
}
