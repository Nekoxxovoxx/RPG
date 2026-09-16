using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class RepairValidation
{
    private static int assertions;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void RunCore()
    {
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CheckHealth();
            CheckAttribution();
            CheckEquipment();
            CheckHealthBar();
            CheckAssets();
            Debug.Log("REPAIR_VALIDATION_PASS assertions=" + assertions);
            Type sync = typeof(Editor).Assembly.GetType("UnityEditor.SyncVS");
            sync?.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static T Stats<T>(string name) where T : CharacterStats
    {
        T stats = new GameObject(name).AddComponent<T>();
        stats.EnsureCoreStatObjects();
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            Stat stat = stats.GetStat(type);
            stat.SetDefaultValue(0);
            stat.modifiers?.Clear();
        }
        stats.maxHealth.SetDefaultValue(100);
        stats.currentHealth = 100;
        return stats;
    }

    private static void CheckHealth()
    {
        CharacterStats stats = Stats<CharacterStats>("Health test");
        int notifications = 0;
        stats.onHealthChanged += () => notifications++;
        stats.TakeDamage(0);
        stats.TakeDamage(-5);
        Require(stats.currentHealth == 100 && notifications == 0, "Zero/negative damage must have no health or hit side effects");
        stats.TakeDamage(200);
        Require(stats.isDead && stats.currentHealth == 0, "Lethal damage must clamp to zero");
        stats.IncreaseHealthBy(100);
        stats.TakeDamage(5);
        Require(stats.currentHealth == 0, "Dead characters cannot heal or receive repeated damage");
        stats.isIgnited = true;
        stats.ReviveWithHealth(-50);
        Require(!stats.isDead && stats.currentHealth == 1 && !stats.isIgnited, "Revive must clamp health and clear burning");
        stats.ReviveWithHealth(int.MaxValue);
        Require(stats.currentHealth == 100, "Saved health above maximum must clamp");
        stats.IncreaseHealthBy(int.MaxValue);
        Require(stats.currentHealth == 100, "Healing must not overflow");
        stats.maxHealth.SetDefaultValue(0);
        stats.vitality.SetDefaultValue(-5);
        Require(stats.GetMaxHealthValue() == 1, "Maximum health must always be positive");
        Object.DestroyImmediate(stats.gameObject);
    }

    private static void CheckAttribution()
    {
        try { DamageAttribution.RunAsPlayerDamage(() => throw new InvalidOperationException()); }
        catch (InvalidOperationException) { }
        Require(!DamageAttribution.IsPlayerDamage, "Damage context must restore after exceptions");
        PlayerStats player = Stats<PlayerStats>("Player attribution test");
        player.damage.SetDefaultValue(150);
        EnemyStats enemy = Stats<EnemyStats>("Player kill target");
        player.DoDamage(enemy);
        Require(enemy.isDead && enemy.WasKilledByPlayer, "Normal player attack must credit the kill");
        Object.DestroyImmediate(enemy.gameObject);
        enemy = Stats<EnemyStats>("Environment kill target");
        DamageAttribution.RunAsPlayerDamage(() => enemy.TakeDamage(10));
        enemy.TakeDamage(150);
        Require(enemy.isDead && !enemy.WasKilledByPlayer, "Earlier player damage must not claim a later environment kill");
        Object.DestroyImmediate(enemy.gameObject);
        Object.DestroyImmediate(player.gameObject);
    }

    private static void CheckEquipment()
    {
        PlayerStats player = Stats<PlayerStats>("Equipment test");
        player.damage.SetDefaultValue(100);
        BlazingSun_Effect sun = ScriptableObject.CreateInstance<BlazingSun_Effect>();
        sun.OnEquip(null, player);
        Require(player.damage.GetValue() == 110, "Blazing Sun must add ten percent");
        sun.OnUnequip(null, player);
        Require(player.damage.GetValue() == 100, "Unequip must remove all Blazing Sun bonuses");
        int health = player.currentHealth;
        sun.OnEquip(null, player);
        sun.OnUnequip(null, player);
        Require(player.currentHealth == health, "Equip cycling must not heal");
        SilverOathKnight_Effect silver = ScriptableObject.CreateInstance<SilverOathKnight_Effect>();
        silver.OnEquip(null, player);
        player.TakeDamage(95);
        Require(player.currentHealth == 50, "Silver oath must restore health to fifty percent");
        silver.OnUnequip(null, player);
        silver.OnEquip(null, player);
        player.TakeDamage(45);
        Require(player.currentHealth == 5, "Re-equipping cannot reset the once-per-rest rescue");
        silver.OnUnequip(null, player);
        MoonShadowLastHomeland_Effect moon = ScriptableObject.CreateInstance<MoonShadowLastHomeland_Effect>();
        moon.OnEquip(null, player);
        player.TakeDamage(50);
        var runtime = player.GetComponent<MoonShadowLastHomelandRuntimeEffect>();
        Require(!player.isDead && player.currentHealth == 0 && runtime.IsDefyingDeath, "Moon Shadow must start its survival window");
        EnemyStats target = Stats<EnemyStats>("Uncredited kill");
        target.TakeDamage(100);
        Require(runtime.IsDefyingDeath, "Environment kills cannot rescue Moon Shadow");
        player.IncreaseHealthBy(100);
        Require(player.currentHealth == 0, "Healing cannot bypass the kill requirement");
        Object.DestroyImmediate(target.gameObject);
        target = Stats<EnemyStats>("Credited skill kill");
        DamageAttribution.RunAsPlayerDamage(() => target.TakeDamage(100));
        Require(!runtime.IsDefyingDeath && !player.isDead && player.currentHealth == 10, "Player skill kills must rescue Moon Shadow at ten percent");
        player.TakeDamage(100);
        player.ReviveToFullHealth();
        Require(!runtime.IsDefyingDeath && player.currentHealth == 100, "Rest must cancel the delayed death window");
        player.TakeDamage(100);
        typeof(MoonShadowLastHomelandRuntimeEffect).GetMethod("ForceDelayedDeath", Private).Invoke(runtime, null);
        Require(player.isDead && player.currentHealth == 0, "An expired window must kill exactly once");
        Object.DestroyImmediate(target.gameObject);
        Object.DestroyImmediate(player.gameObject);
        Object.DestroyImmediate(moon);
        Object.DestroyImmediate(silver);
        Object.DestroyImmediate(sun);
    }

    private static void CheckHealthBar()
    {
        CharacterStats stats = Stats<CharacterStats>("Health bar target");
        GameObject bar = new GameObject("Bar", typeof(RectTransform), typeof(CanvasGroup));
        bar.transform.SetParent(stats.transform);
        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(bar.transform);
        var presenter = bar.AddComponent<UI_HealthBar>();
        typeof(UI_HealthBar).GetMethod("OnEnable", Private).Invoke(presenter, null);
        Slider slider = sliderObject.GetComponent<Slider>();
        int subscribers = stats.onHealthChanged?.GetInvocationList().Length ?? 0;
        for (int i = 0; i < 3; i++)
        {
            bar.SetActive(false);
            typeof(UI_HealthBar).GetMethod("OnDisable", Private).Invoke(presenter, null);
            bar.SetActive(true);
            typeof(UI_HealthBar).GetMethod("OnEnable", Private).Invoke(presenter, null);
            stats.TakeDamage(5);
            Require(slider.value == stats.currentHealth, "Health bar must update after re-enable");
            Require((stats.onHealthChanged?.GetInvocationList().Length ?? 0) == subscribers, "Health bar must not duplicate event subscriptions");
        }
        typeof(UI_HealthBar).GetMethod("OnDisable", Private).Invoke(presenter, null);
        Object.DestroyImmediate(stats.gameObject);
    }

    private static void CheckAssets()
    {
        string[] paths = { "Weapon/烈阳", "Weapon/月影·失乡", "Weapon/落樱", "Weapon/霜鸣·断雪", "Armor/银誓骑士" };
        foreach (string path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemData_Equipment>("Assets/Data/Equipment/" + path + ".asset");
            Require(asset != null && asset.itemEffects != null && asset.itemEffects.Any(e => e != null), "Equipment effect must be bound: " + path);
            Require(asset.itemicon != null, "Equipment icon must resolve: " + path);
        }
        foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled))
            Require(File.Exists(scene.path), "Enabled build scene must exist: " + scene.path);
    }
}
