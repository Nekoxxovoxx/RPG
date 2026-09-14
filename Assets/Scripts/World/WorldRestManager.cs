using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldRestManager : MonoBehaviour
{
    private const string LastSaveSceneKey = "LastSaveScene";
    private const string LastSavePositionXKey = "LastSavePositionX";
    private const string LastSavePositionYKey = "LastSavePositionY";
    private const string LastSavePositionZKey = "LastSavePositionZ";
    private const string LastSaveHealthKey = "LastSaveHealth";
    private const string AutoSaveSceneKey = "AutoSaveScene";
    private const string AutoSavePositionXKey = "AutoSavePositionX";
    private const string AutoSavePositionYKey = "AutoSavePositionY";
    private const string AutoSavePositionZKey = "AutoSavePositionZ";
    private const string AutoSaveHealthKey = "AutoSaveHealth";
    private const string GameStartedKey = "GameStarted";
    private const string PlayerEmbersKey = "PlayerEmbers";
    private const string PlayerLevelKey = "PlayerLevel";
    private const string PlayerLevelStrengthKey = "PlayerLevelStrength";
    private const string PlayerLevelAgilityKey = "PlayerLevelAgility";
    private const string PlayerLevelIntelligenceKey = "PlayerLevelIntelligence";
    private const string PlayerLevelVitalityKey = "PlayerLevelVitality";
    private const string PlayerLevelSpentEmbersKey = "PlayerLevelSpentEmbers";
    private const string DewFlaskMaxChargesKey = "DewFlaskMaxCharges";
    private const string DewFlaskCurrentChargesKey = "DewFlaskCurrentCharges";
    private const string DewFlaskHealPercentKey = "DewFlaskHealPercent";
    private const string DewFlaskCapacityLevelKey = "DewFlaskCapacityLevel";
    private const string DewFlaskHealingLevelKey = "DewFlaskHealingLevel";

    private class EnemySnapshot
    {
        public string name;
        public GameObject instance;
        public GameObject template;
        public Transform parent;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    public static WorldRestManager instance;

    private static bool returnToLastRestAfterSceneLoad;
    private static bool continueFromAutoSaveAfterSceneLoad;
    private static bool subscribedToSceneLoaded;

    [SerializeField] private bool captureEnemiesOnStart = true;
    [SerializeField] private bool refillPlayerHealth = true;
    [SerializeField] private bool refillFlasks = true;
    [SerializeField] private bool respawnEnemies = true;

    private readonly List<EnemySnapshot> enemySnapshots = new List<EnemySnapshot>();
    private Transform templateRoot;
    private bool captured;
    private bool hasInitialPlayerSpawn;
    private Vector3 initialPlayerSpawnPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeSceneLoaded()
    {
        if (subscribedToSceneLoaded)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        subscribedToSceneLoaded = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SubscribeSceneLoaded();
        WorldRestManager manager = GetOrCreate();
        manager.StartCoroutine(manager.CaptureAfterSceneSettles());
        manager.StartCoroutine(manager.HandlePendingSceneRestoreAfterSceneSettles());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        WorldRestManager manager = GetOrCreate();
        manager.captured = false;
        manager.hasInitialPlayerSpawn = false;
        manager.StartCoroutine(manager.CaptureAfterSceneSettles());
        manager.StartCoroutine(manager.HandlePendingSceneRestoreAfterSceneSettles());
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        RecordInitialPlayerSpawn(PlayerManager.instance != null ? PlayerManager.instance.player : null);

        if (captureEnemiesOnStart)
            StartCoroutine(CaptureAfterSceneSettles());
    }

    public static WorldRestManager GetOrCreate()
    {
        if (instance != null)
            return instance;

        WorldRestManager existing = FindObjectOfType<WorldRestManager>(true);

        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject managerObject = new GameObject("World Rest Manager");
        return managerObject.AddComponent<WorldRestManager>();
    }

    public static bool HasStartedGame()
    {
        return PlayerPrefs.GetInt(GameStartedKey, 0) == 1;
    }

    public static void MarkGameStarted()
    {
        PlayerPrefs.SetInt(GameStartedKey, 1);
        PlayerPrefs.Save();
    }

    public static bool HasSavedRestPoint()
    {
        return PlayerPrefs.HasKey(LastSavePositionXKey) &&
               PlayerPrefs.HasKey(LastSavePositionYKey) &&
               !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(LastSaveSceneKey, string.Empty));
    }

    public static bool HasAutoSaveSnapshot()
    {
        return PlayerPrefs.HasKey(AutoSavePositionXKey) &&
               PlayerPrefs.HasKey(AutoSavePositionYKey) &&
               !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(AutoSaveSceneKey, string.Empty));
    }

    public static string GetSavedRestSceneName(string fallbackSceneName)
    {
        string savedSceneName = PlayerPrefs.GetString(LastSaveSceneKey, string.Empty);
        return string.IsNullOrWhiteSpace(savedSceneName) ? fallbackSceneName : savedSceneName;
    }

    public static string GetContinueSceneName(string fallbackSceneName)
    {
        string autoSaveSceneName = PlayerPrefs.GetString(AutoSaveSceneKey, string.Empty);

        if (!string.IsNullOrWhiteSpace(autoSaveSceneName))
            return autoSaveSceneName;

        return GetSavedRestSceneName(fallbackSceneName);
    }

    public static void RequestReturnToLastRestAfterNextSceneLoad()
    {
        returnToLastRestAfterSceneLoad = true;
        continueFromAutoSaveAfterSceneLoad = false;
    }

    public static void RequestContinueFromAutoSaveAfterNextSceneLoad()
    {
        continueFromAutoSaveAfterSceneLoad = true;
        returnToLastRestAfterSceneLoad = false;
    }

    public static void AutoSaveCurrentGame()
    {
        GetOrCreate().SaveAutoSnapshot(PlayerManager.instance != null ? PlayerManager.instance.player : null);
    }

    public static void ClearGameplayProgress()
    {
        string[] keys =
        {
            LastSaveSceneKey,
            LastSavePositionXKey,
            LastSavePositionYKey,
            LastSavePositionZKey,
            LastSaveHealthKey,
            AutoSaveSceneKey,
            AutoSavePositionXKey,
            AutoSavePositionYKey,
            AutoSavePositionZKey,
            AutoSaveHealthKey,
            GameStartedKey,
            PlayerEmbersKey,
            PlayerLevelKey,
            PlayerLevelStrengthKey,
            PlayerLevelAgilityKey,
            PlayerLevelIntelligenceKey,
            PlayerLevelVitalityKey,
            PlayerLevelSpentEmbersKey,
            DewFlaskMaxChargesKey,
            DewFlaskCurrentChargesKey,
            DewFlaskHealPercentKey,
            DewFlaskCapacityLevelKey,
            DewFlaskHealingLevelKey
        };

        for (int i = 0; i < keys.Length; i++)
            PlayerPrefs.DeleteKey(keys[i]);

        SkillManager.ResetSavedUnlocksToDefaults();
        PlayerPrefs.Save();

        returnToLastRestAfterSceneLoad = false;
        continueFromAutoSaveAfterSceneLoad = false;
    }

    public void Rest(Player player)
    {
        if (player == null && PlayerManager.instance != null)
            player = PlayerManager.instance.player;

        RecordInitialPlayerSpawn(player);

        if (refillPlayerHealth && player != null && player.stats != null)
            player.stats.IncreaseHealthBy(player.stats.GetMaxHealthValue());

        if (refillFlasks)
            PlayerFlaskSystem.GetOrCreate().RefillMainFlask();

        if (respawnEnemies)
            RespawnEnemies();

        SavePlayerPosition(player);
        SaveAutoSnapshot(player);
    }

    public bool ReturnPlayerToLastRest(Player player)
    {
        if (player == null && PlayerManager.instance != null)
            player = PlayerManager.instance.player;

        if (player == null)
            return false;

        RecordInitialPlayerSpawn(player);

        Vector3 respawnPosition = GetLastRestPosition(player.transform.position);
        player.RespawnAt(respawnPosition);

        if (refillFlasks)
            PlayerFlaskSystem.GetOrCreate().RefillMainFlask();

        if (respawnEnemies)
            RespawnEnemies();

        SavePlayerPosition(player);
        SaveAutoSnapshot(player);
        return true;
    }

    public void CaptureSceneEnemies()
    {
        if (captured)
            return;

        captured = true;
        EnsureTemplateRoot();
        enemySnapshots.Clear();

        EnemyStats[] enemies = FindObjectsOfType<EnemyStats>(false);

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyStats enemyStats = enemies[i];

            if (enemyStats == null || enemyStats.GetComponentInParent<EnemyRespawnTemplateMarker>() != null)
                continue;

            if (ShouldExcludeFromRestRespawn(enemyStats))
                continue;

            GameObject enemyObject = enemyStats.gameObject;
            GameObject template = Instantiate(enemyObject, templateRoot);
            template.name = enemyObject.name + "_RespawnTemplate";
            template.SetActive(false);

            if (template.GetComponent<EnemyRespawnTemplateMarker>() == null)
                template.AddComponent<EnemyRespawnTemplateMarker>();

            enemySnapshots.Add(new EnemySnapshot
            {
                name = enemyObject.name,
                instance = enemyObject,
                template = template,
                parent = enemyObject.transform.parent,
                position = enemyObject.transform.position,
                rotation = enemyObject.transform.rotation,
                localScale = enemyObject.transform.localScale
            });
        }
    }

    private bool ShouldExcludeFromRestRespawn(EnemyStats enemyStats)
    {
        if (enemyStats == null)
            return true;

        return enemyStats.GetComponentInParent<IBossIntroPresentationTarget>() != null ||
               enemyStats.GetComponentInParent<IBossCombatActivationTarget>() != null ||
               enemyStats.GetComponentInChildren<IBossIntroPresentationTarget>(true) != null ||
               enemyStats.GetComponentInChildren<IBossCombatActivationTarget>(true) != null;
    }

    public void RespawnEnemies()
    {
        if (!captured)
            CaptureSceneEnemies();

        for (int i = 0; i < enemySnapshots.Count; i++)
        {
            EnemySnapshot snapshot = enemySnapshots[i];

            if (snapshot == null || snapshot.template == null)
                continue;

            if (snapshot.instance != null)
            {
                snapshot.instance.SetActive(false);
                Destroy(snapshot.instance);
            }

            GameObject spawnedEnemy = Instantiate(snapshot.template, snapshot.parent);
            spawnedEnemy.name = snapshot.name;
            spawnedEnemy.transform.SetPositionAndRotation(snapshot.position, snapshot.rotation);
            spawnedEnemy.transform.localScale = snapshot.localScale;

            EnemyRespawnTemplateMarker marker = spawnedEnemy.GetComponent<EnemyRespawnTemplateMarker>();

            if (marker != null)
                Destroy(marker);

            spawnedEnemy.SetActive(true);
            snapshot.instance = spawnedEnemy;
        }
    }

    private IEnumerator CaptureAfterSceneSettles()
    {
        yield return null;
        RecordInitialPlayerSpawn(PlayerManager.instance != null ? PlayerManager.instance.player : null);
        CaptureSceneEnemies();
    }

    private IEnumerator HandlePendingSceneRestoreAfterSceneSettles()
    {
        if (!returnToLastRestAfterSceneLoad && !continueFromAutoSaveAfterSceneLoad)
            yield break;

        yield return null;
        yield return null;

        if (continueFromAutoSaveAfterSceneLoad)
        {
            continueFromAutoSaveAfterSceneLoad = false;
            RestorePlayerFromContinueSnapshot(PlayerManager.instance != null ? PlayerManager.instance.player : null);
            yield break;
        }

        if (returnToLastRestAfterSceneLoad)
        {
            returnToLastRestAfterSceneLoad = false;
            ReturnPlayerToLastRest(PlayerManager.instance != null ? PlayerManager.instance.player : null);
        }
    }

    private void EnsureTemplateRoot()
    {
        if (templateRoot != null)
            return;

        GameObject root = new GameObject("Enemy Respawn Templates");
        root.hideFlags = HideFlags.HideInHierarchy;
        templateRoot = root.transform;
    }

    private void SavePlayerPosition(Player player)
    {
        if (player == null)
            return;

        MarkGameStarted();

        Vector3 position = player.transform.position;
        PlayerPrefs.SetString(LastSaveSceneKey, SceneManager.GetActiveScene().name);
        PlayerPrefs.SetFloat(LastSavePositionXKey, position.x);
        PlayerPrefs.SetFloat(LastSavePositionYKey, position.y);
        PlayerPrefs.SetFloat(LastSavePositionZKey, position.z);

        if (player.stats != null)
            PlayerPrefs.SetInt(LastSaveHealthKey, player.stats.currentHealth);

        PlayerPrefs.Save();
    }

    private void SaveAutoSnapshot(Player player)
    {
        if (player == null)
            return;

        MarkGameStarted();

        Vector3 position = player.transform.position;
        PlayerPrefs.SetString(AutoSaveSceneKey, SceneManager.GetActiveScene().name);
        PlayerPrefs.SetFloat(AutoSavePositionXKey, position.x);
        PlayerPrefs.SetFloat(AutoSavePositionYKey, position.y);
        PlayerPrefs.SetFloat(AutoSavePositionZKey, position.z);

        if (player.stats != null)
            PlayerPrefs.SetInt(AutoSaveHealthKey, Mathf.Max(1, player.stats.currentHealth));

        PlayerFlaskSystem flaskSystem = PlayerFlaskSystem.instance;

        if (flaskSystem != null)
            flaskSystem.Save();

        PlayerPrefs.Save();
    }

    private bool RestorePlayerFromContinueSnapshot(Player player)
    {
        if (player == null && PlayerManager.instance != null)
            player = PlayerManager.instance.player;

        if (player == null)
            return false;

        RecordInitialPlayerSpawn(player);

        Vector3 position = GetContinuePosition(player.transform.position);
        int health = PlayerPrefs.GetInt(AutoSaveHealthKey, player.stats != null ? player.stats.GetMaxHealthValue() : 1);
        player.RestoreAtSavedState(position, health);
        return true;
    }

    private Vector3 GetContinuePosition(Vector3 fallback)
    {
        if (HasAutoSaveSnapshot())
        {
            string savedScene = PlayerPrefs.GetString(AutoSaveSceneKey, SceneManager.GetActiveScene().name);

            if (string.IsNullOrWhiteSpace(savedScene) || savedScene == SceneManager.GetActiveScene().name)
            {
                return new Vector3(
                    PlayerPrefs.GetFloat(AutoSavePositionXKey),
                    PlayerPrefs.GetFloat(AutoSavePositionYKey),
                    PlayerPrefs.GetFloat(AutoSavePositionZKey, fallback.z));
            }
        }

        return GetLastRestPosition(fallback);
    }

    private Vector3 GetLastRestPosition(Vector3 fallback)
    {
        if (PlayerPrefs.HasKey(LastSavePositionXKey) && PlayerPrefs.HasKey(LastSavePositionYKey))
        {
            string savedScene = PlayerPrefs.GetString(LastSaveSceneKey, SceneManager.GetActiveScene().name);

            if (!string.IsNullOrWhiteSpace(savedScene) && savedScene != SceneManager.GetActiveScene().name)
                Debug.LogWarning("Last rest point belongs to another scene. Respawning in current scene fallback position.");
            else
                return new Vector3(
                    PlayerPrefs.GetFloat(LastSavePositionXKey),
                    PlayerPrefs.GetFloat(LastSavePositionYKey),
                    PlayerPrefs.GetFloat(LastSavePositionZKey, fallback.z));
        }

        return hasInitialPlayerSpawn ? initialPlayerSpawnPosition : fallback;
    }

    private void RecordInitialPlayerSpawn(Player player)
    {
        if (hasInitialPlayerSpawn || player == null)
            return;

        initialPlayerSpawnPosition = player.transform.position;
        hasInitialPlayerSpawn = true;
    }

    private void OnApplicationQuit()
    {
        SaveAutoSnapshot(PlayerManager.instance != null ? PlayerManager.instance.player : null);
    }
}

public class EnemyRespawnTemplateMarker : MonoBehaviour
{
}
