using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Isolated checks: inactive player/inventory fixtures never load or save progress.
[InitializeOnLoad]
public static class LifecycleValidation
{
    private const string SessionKey = "LifecycleValidation.Active";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static Enemy enemy;
    private static int stage;
    private static int assertions;
    private static float stageTime;
    private static double startedAt;
    private static string failure;

    static LifecycleValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/level1.unity");
            var flasks = Object.FindObjectsOfType<PlayerFlaskSystem>(true);
            Require(flasks.Length == 1, "Level1 must serialize exactly one flask system.");
            var item = AssetDatabase.LoadAssetAtPath<ItemData_Equipment>(
                "Assets/Data/Equipment/Flask/\u6062\u590d\u836f\u6c34.asset");
            Require(item != null && flasks[0].MainFlaskItemData == item,
                "Main flask must have an explicit build-time asset reference.");
            var inventory = Object.FindObjectOfType<Inventory>(true);
            Require(inventory != null && inventory.startingItems.Contains(item),
                "Main flask must be in starting items.");
            Require(AssetDatabase.GetDependencies("Assets/Scenes/level1.unity", true)
                .Contains(AssetDatabase.GetAssetPath(item)), "Build dependencies must include main flask.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetInt(SessionKey + ".Assertions", assertions);
            SessionState.SetBool(SessionKey, true);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            startedAt = EditorApplication.timeSinceStartup;
            assertions = SessionState.GetInt(SessionKey + ".Assertions", 0);
            stage = 0;
            failure = null;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(SessionKey, false);
            bool passed = failure == null && stage == 5;
            if (passed) Debug.Log("LIFECYCLE_VALIDATION_PASS assertions=" + assertions);
            else Debug.LogError("LIFECYCLE_VALIDATION_FAIL " + failure);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (SessionState.GetBool(SessionKey, false) &&
            (type == LogType.Error || type == LogType.Exception)) failure = failure ?? message;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(SessionKey, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (EditorApplication.timeSinceStartup - startedAt > 25)
                throw new TimeoutException("Lifecycle checks timed out.");
            if (failure != null) throw new InvalidOperationException(failure);
            if (stage == 0)
            {
                CheckFlask();
                var host = new GameObject("Control test", typeof(Animator), typeof(Rigidbody2D),
                    typeof(BoxCollider2D), typeof(EnemyStats));
                host.GetComponent<Rigidbody2D>().gravityScale = 0;
                enemy = host.AddComponent<Enemy>();
                enemy.defaultMoveSpeed = enemy.moveSpeed = 10;
                stage = 1;
            }
            else if (stage == 1 && enemy.anim != null)
            {
                enemy.SlowEntityBy(0.5f, 0.15f);
                Require(enemy.moveSpeed == 5 && enemy.anim.speed == 0.5f, "Slow must affect movement and animation.");
                enemy.FreezeTime(true);
                enemy.FreezeTimeFor(0.15f);
                stageTime = Time.time;
                stage = 2;
            }
            else if (stage == 2 && Time.time > stageTime + 0.4f)
            {
                Require(enemy.IsFrozen && enemy.moveSpeed == 0 && enemy.anim.speed == 0,
                    "Expired slow/timed freeze must not release external freeze.");
                enemy.FreezeTime(false);
                Require(!enemy.IsFrozen && enemy.moveSpeed == 10 && enemy.anim.speed == 1,
                    "Final control release must restore original speed.");
                enemy.FreezeTimeFor(0.15f);
                enemy.enabled = false;
                Require(!enemy.IsFrozen && enemy.moveSpeed == 10, "Disable must clear control state.");
                enemy.enabled = true;
                enemy.FreezeTimeFor(1f);
                stageTime = Time.time;
                stage = 3;
            }
            else if (stage == 3 && Time.time > stageTime + 0.4f)
            {
                Require(enemy.IsFrozen, "Old timer must not release a new freeze after re-enable.");
                enemy.gameObject.SetActive(false);
                enemy.gameObject.SetActive(true);
                Require(!enemy.IsFrozen && enemy.moveSpeed == 10 && enemy.anim.speed == 1,
                    "GameObject re-enable must not preserve a stopped freeze timer.");
                enemy.SlowEntityBy(0.5f, 5f);
                enemy.FreezeTimeFor(0.15f);
                stageTime = Time.time;
                stage = 4;
            }
            else if (stage == 4 && Time.time > stageTime + 0.4f)
            {
                Require(!enemy.IsFrozen && enemy.moveSpeed == 5 && enemy.anim.speed == 0.5f,
                    "Releasing freeze must retain a still-active slow.");
                CheckSun();
                stage = 5;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception ex)
        {
            failure = ex.ToString();
            EditorApplication.ExitPlaymode();
        }
    }

    private static GameObject Inactive(string name)
    {
        var host = new GameObject(name);
        host.SetActive(false);
        return host;
    }

    private static void CheckFlask()
    {
        var host = Inactive("Flask fixture");
        var flask = host.AddComponent<PlayerFlaskSystem>();
        Set(flask, "saveFlaskProgress", false);
        var item = ScriptableObject.CreateInstance<ItemData_Equipment>();
        item.equipmentType = EquipmentType.Flask;
        item.itemName = "Renamed main flask";
        Set(flask, "mainFlaskItemData", item);
        Require(flask.RegisterMainFlaskItem(item), "Explicit reference must survive renaming.");
        var stats = Inactive("Health fixture").AddComponent<PlayerStats>();
        stats.EnsureCoreStatObjects();
        stats.maxHealth.SetDefaultValue(100);
        stats.vitality.SetDefaultValue(0);
        stats.currentHealth = 50;
        Require(flask.CanUseMainFlask(stats), "Living wounded player must be able to heal.");
        typeof(CharacterStats).GetProperty("isDead").SetValue(stats, true);
        Require(!flask.CanUseMainFlask(stats), "Dead player must not consume a flask.");
        typeof(CharacterStats).GetProperty("isDead").SetValue(stats, false);
        stats.currentHealth = 0;
        Require(!flask.CanUseMainFlask(stats), "Flask must not bypass the zero-health survival window.");
        stats.currentHealth = 100;
        Require(!flask.CanUseMainFlask(stats), "Full health must not consume a flask.");
        stats.currentHealth = 50;
        Set(flask, "mainHealPercent", 0f);
        Require(!flask.CanUseMainFlask(stats), "Zero healing must not consume a flask.");
        Set(flask, "mainHealPercent", 0.3f);
        var inventory = Inactive("Inventory fixture").AddComponent<Inventory>();
        Inventory.instance = inventory;
        var routine = (IEnumerator)Call(flask, "AutoEquipMainFlaskNextFrame");
        for (int i = 0; i < 5; i++)
            Require(routine.MoveNext(), "Auto-equip must wait as long as inventory initialization takes.");
        inventory.equipment = new List<InventoryItem>();
        inventory.equipmentDictionary = new Dictionary<ItemData_Equipment, InventoryItem>();
        Set(inventory, "<IsInitialized>k__BackingField", true);
        Require(!routine.MoveNext() && inventory.equipmentDictionary.ContainsKey(item),
            "Auto-equip must complete when inventory is ready.");
        Call(flask, "AutoEquipMainFlask");
        Require(inventory.equipment.Count == 1, "Repeated auto-equip must not duplicate the flask.");
        Require(flask.MainCurrentCharges == 3 && flask.GetMainCooldownProgress() == 0,
            "Failed usage checks must not consume charges or start cooldown.");
        Inventory.instance = null;
        Object.Destroy(host);
        Object.Destroy(stats.gameObject);
        Object.Destroy(inventory.gameObject);
        Object.Destroy(item);
    }

    private static void CheckSun()
    {
        var casterHost = Inactive("Sun caster fixture");
        var caster = casterHost.AddComponent<Player>();
        var stats = casterHost.AddComponent<PlayerStats>();
        var skill = new GameObject("Sun skill fixture").AddComponent<Sun_Skill>();
        var prefab = Inactive("Sun prefab fixture");
        prefab.AddComponent<SunSkillEffect>();
        Set(skill, "sunPrefab", prefab);
        skill.SpawnSun(caster);
        skill.SpawnSun(caster);
        var suns = (List<GameObject>)Get(skill, "activeSuns");
        Require(suns.Count == 2, "All spawned suns must be tracked.");
        GameObject[] spawned = suns.ToArray();
        spawned[0].SetActive(true);
        var effect = spawned[0].GetComponent<SunSkillEffect>();
        Call(effect, "RegisterEnemyCollider", enemy.GetComponent<Collider2D>());
        var affected = (IDictionary)Get(effect, "affectedEnemies");
        Call(effect, "UpdateEnemyControl", enemy, affected[enemy], true);
        Require(enemy.IsFrozen, "Sun fixture must actually apply control.");
        var manager = Inactive("Skill manager fixture").AddComponent<SkillManager>();
        Set(manager, "<sun>k__BackingField", skill);
        manager.CancelAllActiveSkills();
        Require(suns.Count == 0 && spawned.All(s => !s.activeSelf), "Cancellation must deactivate every sun immediately.");
        Require(!enemy.IsFrozen && affected.Count == 0, "Sun cancellation must release controlled enemies.");
        Set(skill, "cooldownTimer", 12f);
        manager.ResetAllCooldowns();
        Require(skill.CooldownRemaining == 0, "Revival reset must clear Sun cooldown.");
        typeof(CharacterStats).GetProperty("isDead").SetValue(stats, true);
        skill.SpawnSun(caster);
        Require(suns.Count == 0, "Dead caster must not spawn another sun.");
        typeof(CharacterStats).GetProperty("isDead").SetValue(stats, false);
        skill.SpawnSun(caster);
        skill.enabled = false;
        Require(suns.Count == 0, "Disabling the skill must also clear live suns.");
        Object.Destroy(casterHost);
        Object.Destroy(skill.gameObject);
        Object.Destroy(prefab);
        Object.Destroy(manager.gameObject);
    }

    private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
