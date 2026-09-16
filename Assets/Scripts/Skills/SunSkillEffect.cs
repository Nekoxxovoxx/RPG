using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class SunSkillEffect : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float defaultTargetDiameter = 8f;
    [SerializeField, Min(0.05f)] private float growDuration = 0.75f;
    [SerializeField, Range(0.01f, 1f)] private float startScalePercent = 0.1f;
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float lifeTime = 5f;
    [SerializeField] private bool destroyAfterLifeTime = true;

    [Header("Gravity")]
    [SerializeField] private bool useCameraVisiblePullArea = true;
    [SerializeField, Min(0f)] private float cameraAreaPadding;
    [SerializeField, Min(1)] private int maxPullTargets = 5;
    [SerializeField, Min(0.1f)] private float pullRadius = 6.5f;
    [SerializeField] private bool useScreenBasedPullRadius;
    [SerializeField, Min(0f)] private float screenPullPadding = 2f;
    [SerializeField, Min(0.1f)] private float fallbackPullRadius = 30f;
    [SerializeField, Min(0.1f)] private float pullSpeed = 8f;
    [SerializeField, Min(0.01f)] private float captureRadius = 0.2f;
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Header("Damage")]
    [SerializeField] private bool damageUsesCameraVisibleArea;
    [SerializeField, Min(1f)] private float damageMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float damageRadius = 5f;
    [SerializeField, Min(1)] private int damagePerTick = 8;
    [SerializeField, Min(0.05f)] private float damageInterval = 0.4f;

    private SpriteRenderer spriteRenderer;
    private Vector3 startScale;
    private Vector3 targetScale;
    private float growTimer;
    private float lifeTimer;
    private float activeTargetDiameter;
    private float currentPullRadius;
    private float currentDamageRadius;
    private Bounds currentCameraBounds;
    private bool hasCurrentCameraBounds;
    private bool isPlaying;
    private readonly Dictionary<Enemy, AffectedEnemy> affectedEnemies = new Dictionary<Enemy, AffectedEnemy>();
    private readonly List<Enemy> enemiesToRemove = new List<Enemy>();
    private readonly List<Enemy> pullCandidates = new List<Enemy>();
    private readonly HashSet<Enemy> pullTargets = new HashSet<Enemy>();

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        Play(defaultTargetDiameter, growDuration, lifeTime);
    }

    public void Play(float targetDiameter, float duration, float activeLifeTime)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        float spriteDiameter = GetSpriteDiameter();
        float scale = Mathf.Max(targetDiameter, 0.1f) / spriteDiameter;
        targetScale = Vector3.one * scale;
        startScale = targetScale * startScalePercent;

        growDuration = Mathf.Max(duration, 0.05f);
        lifeTime = Mathf.Max(activeLifeTime, 0f);
        activeTargetDiameter = Mathf.Max(targetDiameter, 0.1f);
        UpdateCurrentCameraBounds();
        currentPullRadius = CalculatePullRadius();
        currentDamageRadius = CalculateDamageRadius();
        growTimer = 0f;
        lifeTimer = 0f;
        isPlaying = true;
        transform.localScale = startScale;
    }

    public void ConfigureSkillTree(int pullTargetLimit, float tickDamageMultiplier, bool useWideDamageArea)
    {
        maxPullTargets = Mathf.Max(1, pullTargetLimit);
        damageMultiplier = Mathf.Max(1f, tickDamageMultiplier);
        damageUsesCameraVisibleArea = useWideDamageArea;
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        growTimer += Time.deltaTime;
        lifeTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(growTimer / growDuration);
        float curvedProgress = growCurve != null ? growCurve.Evaluate(progress) : progress;
        transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curvedProgress);
        UpdateCurrentCameraBounds();
        currentPullRadius = CalculatePullRadius();
        currentDamageRadius = CalculateDamageRadius();

        PullAndDamageEnemies();

        if (destroyAfterLifeTime && lifeTime > 0f && lifeTimer >= lifeTime)
            Destroy(gameObject);
    }

    private void OnDisable()
    {
        ReleaseAffectedEnemies();
    }

    private void PullAndDamageEnemies()
    {
        RegisterEnemiesInRange();
        UpdateAffectedEnemies();
    }

    private void RegisterEnemiesInRange()
    {
        if (useCameraVisiblePullArea && hasCurrentCameraBounds)
        {
            Collider2D[] cameraHits = Physics2D.OverlapBoxAll(
                (Vector2)currentCameraBounds.center,
                (Vector2)currentCameraBounds.size,
                0f,
                enemyLayers);

            for (int i = 0; i < cameraHits.Length; i++)
                RegisterEnemyCollider(cameraHits[i]);
        }
        else
        {
            Collider2D[] pullHits = Physics2D.OverlapCircleAll(transform.position, GetDetectionRadius(), enemyLayers);

            for (int i = 0; i < pullHits.Length; i++)
                RegisterEnemyCollider(pullHits[i]);
        }

        if (!damageUsesCameraVisibleArea)
        {
            Collider2D[] damageHits = Physics2D.OverlapCircleAll(transform.position, currentDamageRadius, enemyLayers);

            for (int i = 0; i < damageHits.Length; i++)
                RegisterEnemyCollider(damageHits[i]);
        }
    }

    private void UpdateAffectedEnemies()
    {
        enemiesToRemove.Clear();
        RefreshPullTargets();

        foreach (KeyValuePair<Enemy, AffectedEnemy> pair in affectedEnemies)
        {
            Enemy enemy = pair.Key;
            AffectedEnemy affectedEnemy = pair.Value;

            if (enemy == null || affectedEnemy.Stats == null || affectedEnemy.Stats.isDead)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            bool isInPullRange = IsEnemyInPullArea(enemy);
            bool isSelectedPullTarget = isInPullRange && pullTargets.Contains(enemy);
            bool isInDamageRange = IsEnemyInDamageRange(enemy);

            if (!isInPullRange && !isInDamageRange)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            UpdateEnemyControl(enemy, affectedEnemy, isSelectedPullTarget);

            if (isInDamageRange)
                DamageEnemyOverTime(affectedEnemy);

            if (affectedEnemy.Stats.isDead)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            if (isSelectedPullTarget)
                PullEnemy(enemy);
        }

        foreach (Enemy enemy in enemiesToRemove)
            RemoveAffectedEnemy(enemy);
    }

    private void PullEnemy(Enemy enemy)
    {
        if (enemy.rb == null)
            return;

        Vector2 currentPosition = enemy.rb.position;
        Vector2 targetPosition = transform.position;
        float distance = Vector2.Distance(currentPosition, targetPosition);

        if (distance <= captureRadius)
        {
            enemy.rb.velocity = Vector2.zero;
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, pullSpeed * Time.deltaTime);
        enemy.rb.velocity = Vector2.zero;
        enemy.transform.position = new Vector3(nextPosition.x, nextPosition.y, enemy.transform.position.z);
    }

    private void DamageEnemyOverTime(AffectedEnemy affectedEnemy)
    {
        affectedEnemy.DamageTimer -= Time.deltaTime;

        if (affectedEnemy.DamageTimer > 0f)
            return;

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damagePerTick * damageMultiplier));
        DamageAttribution.RunAsPlayerDamage(() => affectedEnemy.Stats.TakeDamage(finalDamage));
        affectedEnemy.DamageTimer = damageInterval;
    }

    private void RegisterEnemyCollider(Collider2D hit)
    {
        if (hit == null)
            return;

        Enemy enemy = hit.GetComponentInParent<Enemy>();
        EnemyStats enemyStats = hit.GetComponentInParent<EnemyStats>();

        if (enemy == null || enemyStats == null || enemyStats.isDead || affectedEnemies.ContainsKey(enemy))
            return;

        affectedEnemies.Add(enemy, new AffectedEnemy(enemyStats));
    }

    private void RefreshPullTargets()
    {
        pullTargets.Clear();
        pullCandidates.Clear();

        foreach (Enemy enemy in affectedEnemies.Keys)
        {
            if (enemy == null || IsGenericControlImmune(enemy) || !IsEnemyInPullArea(enemy))
                continue;

            pullCandidates.Add(enemy);
        }

        pullCandidates.Sort(CompareEnemiesByDistanceToSun);

        int limit = Mathf.Max(1, maxPullTargets);

        for (int i = 0; i < pullCandidates.Count && i < limit; i++)
            pullTargets.Add(pullCandidates[i]);
    }

    private int CompareEnemiesByDistanceToSun(Enemy a, Enemy b)
    {
        float aDistance = ((Vector2)GetEnemyBounds(a).center - (Vector2)transform.position).sqrMagnitude;
        float bDistance = ((Vector2)GetEnemyBounds(b).center - (Vector2)transform.position).sqrMagnitude;
        return aDistance.CompareTo(bDistance);
    }

    private void UpdateEnemyControl(Enemy enemy, AffectedEnemy affectedEnemy, bool shouldControl)
    {
        if (enemy == null || IsGenericControlImmune(enemy))
            return;

        if (shouldControl)
        {
            if (!affectedEnemy.ControlApplied)
            {
                enemy.FreezeTime(true);
                affectedEnemy.ControlApplied = true;
            }

            return;
        }

        if (affectedEnemy.ControlApplied)
        {
            enemy.FreezeTime(false);
            affectedEnemy.ControlApplied = false;

            if (enemy.rb != null)
                enemy.rb.velocity = Vector2.zero;
        }
    }

    private void RemoveAffectedEnemy(Enemy enemy)
    {
        if (enemy != null)
        {
            if (affectedEnemies.TryGetValue(enemy, out AffectedEnemy affectedEnemy) &&
                affectedEnemy.ControlApplied &&
                !IsGenericControlImmune(enemy))
            {
                enemy.FreezeTime(false);
            }

            if (enemy.rb != null)
                enemy.rb.velocity = Vector2.zero;
        }

        affectedEnemies.Remove(enemy);
    }

    private void ReleaseAffectedEnemies()
    {
        foreach (Enemy enemy in affectedEnemies.Keys)
        {
            if (enemy != null)
            {
                if (affectedEnemies.TryGetValue(enemy, out AffectedEnemy affectedEnemy) &&
                    affectedEnemy.ControlApplied &&
                    !IsGenericControlImmune(enemy))
                {
                    enemy.FreezeTime(false);
                }

                if (enemy.rb != null)
                    enemy.rb.velocity = Vector2.zero;
            }
        }

        affectedEnemies.Clear();
    }

    private float GetSpriteDiameter()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return 1f;

        Vector3 spriteSize = spriteRenderer.sprite.bounds.size;
        return Mathf.Max(spriteSize.x, spriteSize.y, 0.1f);
    }

    private float CalculatePullRadius()
    {
        if (useCameraVisiblePullArea && hasCurrentCameraBounds)
            return Mathf.Max(currentCameraBounds.extents.x, currentCameraBounds.extents.y, 0.1f);

        if (!useScreenBasedPullRadius)
            return Mathf.Max(0.1f, pullRadius);

        return CalculateScreenBasedPullRadius();
    }

    private float CalculateDamageRadius()
    {
        if (!damageUsesCameraVisibleArea && spriteRenderer != null)
            return Mathf.Max(spriteRenderer.bounds.extents.x, spriteRenderer.bounds.extents.y, 0.1f);

        return Mathf.Max(0.1f, damageRadius);
    }

    private float GetDetectionRadius()
    {
        return Mathf.Max(currentPullRadius, currentDamageRadius, 0.1f);
    }

    private float CalculateScreenBasedPullRadius()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
            return fallbackPullRadius;

        float zDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        float pullRadius = 0f;
        Vector3 sunPosition = transform.position;

        pullRadius = Mathf.Max(pullRadius, Vector2.Distance(sunPosition, mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, zDistance))));
        pullRadius = Mathf.Max(pullRadius, Vector2.Distance(sunPosition, mainCamera.ViewportToWorldPoint(new Vector3(0f, 1f, zDistance))));
        pullRadius = Mathf.Max(pullRadius, Vector2.Distance(sunPosition, mainCamera.ViewportToWorldPoint(new Vector3(1f, 0f, zDistance))));
        pullRadius = Mathf.Max(pullRadius, Vector2.Distance(sunPosition, mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, zDistance))));

        return pullRadius + screenPullPadding;
    }

    private bool IsGenericControlImmune(Enemy enemy)
    {
        return enemy is IGenericControlImmuneEnemy immuneEnemy &&
               immuneEnemy.IsImmuneToGenericEnemyControl;
    }

    private void UpdateCurrentCameraBounds()
    {
        hasCurrentCameraBounds = TryCalculateCameraBounds(out currentCameraBounds);
    }

    private bool TryCalculateCameraBounds(out Bounds cameraBounds)
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            cameraBounds = new Bounds(transform.position, Vector3.one * fallbackPullRadius * 2f);
            return false;
        }

        float zDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, zDistance));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, zDistance));

        Vector3 min = Vector3.Min(bottomLeft, topRight);
        Vector3 max = Vector3.Max(bottomLeft, topRight);
        min.z = transform.position.z - 0.5f;
        max.z = transform.position.z + 0.5f;

        cameraBounds = new Bounds();
        cameraBounds.SetMinMax(min, max);

        if (cameraAreaPadding > 0f)
            cameraBounds.Expand(new Vector3(cameraAreaPadding * 2f, cameraAreaPadding * 2f, 0f));

        return true;
    }

    private bool IsEnemyInPullArea(Enemy enemy)
    {
        if (enemy == null)
            return false;

        if (useCameraVisiblePullArea && hasCurrentCameraBounds)
            return currentCameraBounds.Intersects(GetEnemyBounds(enemy));

        return Vector2.Distance(enemy.transform.position, transform.position) <= currentPullRadius;
    }

    private bool IsEnemyInDamageRange(Enemy enemy)
    {
        if (enemy == null)
            return false;

        if (damageUsesCameraVisibleArea && hasCurrentCameraBounds)
            return currentCameraBounds.Intersects(GetEnemyBounds(enemy));

        return Vector2.Distance(GetEnemyBounds(enemy).center, transform.position) <= currentDamageRadius;
    }

    private Bounds GetEnemyBounds(Enemy enemy)
    {
        if (enemy == null)
            return new Bounds(transform.position, Vector3.one * 0.1f);

        Collider2D enemyCollider = enemy.GetComponentInChildren<Collider2D>();

        if (enemyCollider != null)
            return enemyCollider.bounds;

        Renderer enemyRenderer = enemy.GetComponentInChildren<Renderer>();

        if (enemyRenderer != null)
            return enemyRenderer.bounds;

        return new Bounds(enemy.transform.position, Vector3.one * 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.35f);

        if (useCameraVisiblePullArea && TryCalculateCameraBounds(out Bounds pullBounds))
            Gizmos.DrawWireCube(pullBounds.center, pullBounds.size);
        else
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? currentPullRadius : CalculatePullRadius());

        Gizmos.color = new Color(1f, 0.1f, 0.05f, 0.35f);

        if (damageUsesCameraVisibleArea && TryCalculateCameraBounds(out Bounds damageBounds))
            Gizmos.DrawWireCube(damageBounds.center, damageBounds.size);
        else
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? currentDamageRadius : CalculateDamageRadius());
    }

    private class AffectedEnemy
    {
        public readonly EnemyStats Stats;
        public float DamageTimer;
        public bool ControlApplied;

        public AffectedEnemy(EnemyStats stats)
        {
            Stats = stats;
            DamageTimer = 0f;
        }
    }
}
