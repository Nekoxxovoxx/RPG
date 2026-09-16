using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BossSceneValidation
{
    private static int assertions;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/level1.unity");
            Enemy_ChaosWolfLord wolf = Object.FindObjectOfType<Enemy_ChaosWolfLord>(true);
            Enemy_DemonBoss demon = Object.FindObjectOfType<Enemy_DemonBoss>(true);
            Require(wolf != null && demon != null, "Both scene bosses must exist.");
            Player player = Object.FindObjectOfType<Player>(true);
            Require(player != null, "Missing scene player.");
            Require(new SerializedObject(wolf).FindProperty("chaseMoveSpeed").floatValue > player.moveSpeed,
                "Wolf must be able to close distance on a running player.");
            Require((float)Invoke(demon, "GetDemonMoveSpeed") > player.moveSpeed,
                "Demon must be able to close distance on a running player.");
            CheckIntro(wolf);
            CheckIntro(demon);
            CheckHitAudio(wolf);
            CheckHitAudio(demon);
            CheckArmor(wolf, "currentState");
            CheckArmor(demon, "currentMode");

            var directors = Object.FindObjectsOfType<DemonBossDefeatCollapseDirector>(true);
            Require(directors.Length == 1, "Scene must contain one authored collapse director.");
            var collapse = directors[0];
            var data = new SerializedObject(collapse);
            Require(data.FindProperty("demonBoss").objectReferenceValue == demon, "Collapse boss binding is wrong.");
            var camera = data.FindProperty("cameraDirector").objectReferenceValue as Component;
            Require(camera != null && !camera.transform.IsChildOf(demon.transform),
                "Collapse camera must survive boss destruction.");
            Require(data.FindProperty("postDefeatDialogueLines").arraySize > 1, "Collapse dialogue is missing.");
            var prefab = data.FindProperty("maidenPrefab").objectReferenceValue as GameObject;
            Require(prefab != null && prefab.GetComponentInChildren<SpriteRenderer>(true) != null,
                "Maiden prefab/renderers are missing.");
            Require(prefab.GetComponentInChildren<Animator>(true)?.runtimeAnimatorController != null,
                "Maiden animator is missing.");
            Require(Mathf.Approximately(data.FindProperty("lootPickupDuration").floatValue, 5f),
                "Legacy scene pickup delay was not migrated.");
            Require(Mathf.Approximately(data.FindProperty("shakeBeforeMaidenAppearDuration").floatValue, 3f),
                "Maiden should appear after three seconds of shaking.");
            var routine = (IEnumerator)Invoke(collapse, "CollapseRoutine", demon);
            Require(routine.MoveNext() && routine.Current is WaitForSeconds,
                "Collapse must wait for playable loot time before locking the player.");
            Require(Get(collapse, "lockedPlayer") == null, "Player was locked before the pickup window.");
            (routine as IDisposable)?.Dispose();

            string destination = data.FindProperty("endSceneName").stringValue;
            Require(EditorBuildSettings.scenes.Any(s => s.enabled &&
                Path.GetFileNameWithoutExtension(s.path) == destination), "End scene is absent from build.");
            foreach (var root in collapse.gameObject.scene.GetRootGameObjects())
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                        "Missing script: " + child.name);
            Debug.Log("BOSS_SCENE_VALIDATION_PASS assertions=" + assertions);
            // Tests deliberately change UI visibility in memory only. Never save that scene state.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static void CheckIntro(Enemy boss)
    {
        var intro = boss.GetComponent<BossIntroSequenceDirector>();
        Require(intro != null, "Missing boss intro: " + boss.name);
        var data = new SerializedObject(intro);
        Require(data.FindProperty("bossPresentationTarget").objectReferenceValue == boss,
            "Wrong intro presentation target: " + boss.name);
        Require(data.FindProperty("bossStats").objectReferenceValue == boss.GetComponent<CharacterStats>(),
            "Wrong intro health target: " + boss.name);
        foreach (string field in new[] { "entrancePortal", "cameraFocusTarget", "bossUiRoot",
            "bossCombatUiGroup", "bossHealthBar", "bossHealthSlider", "introText" })
            Require(data.FindProperty(field).objectReferenceValue != null, "Unbound " + field + " on " + boss.name);
        GameObject group = (GameObject)data.FindProperty("bossCombatUiGroup").objectReferenceValue;
        Invoke(intro, "ActivateOnlyCurrentBossGroup");
        Require(group.activeSelf, "Current boss UI did not activate.");
        GameObject root = (GameObject)data.FindProperty("bossUiRoot").objectReferenceValue;
        foreach (Transform branch in root.transform)
            if (!group.transform.IsChildOf(branch))
                Require(!branch.gameObject.activeSelf, "Another boss UI remained visible: " + branch.name);
        Debug.Log("BOSS_BINDING " + boss.name + " -> " + group.name);
    }

    private static void CheckArmor(Enemy boss, string stateField)
    {
        if (boss is Enemy_DemonBoss)
            Set(boss, "currentPhase", Enum.Parse(Field(boss, "currentPhase").FieldType, "Demon"));
        object state = Get(boss, stateField);
        Set(boss, "advancedSuperArmor", true);
        boss.DamageImpact();
        Require(Equals(Get(boss, stateField), state), "Armor must suppress stagger.");
        var armor = (IEnumerator)Invoke(boss, "AdvancedSuperArmorRoutine");
        Require(armor.MoveNext(), "Armor has no timed lifetime.");
        armor.MoveNext();
        Require(!(bool)Get(boss, "advancedSuperArmor"), "Armor failed to expire.");
    }

    private static void CheckHitAudio(Enemy boss)
    {
        var audio = boss.GetComponent<AudioEventPlayer>() ?? boss.GetComponentInChildren<AudioEventPlayer>(true);
        Require(audio != null && audio.Cues != null && audio.Cues.Any(c =>
            c != null && string.Equals(c.cueName, "hurt", StringComparison.OrdinalIgnoreCase) &&
            c.clips != null && c.clips.Any(clip => clip != null)), "Missing boss hurt sound: " + boss.name);
    }

    public static void BuildWindows()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/RepairDemo/YJ.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        Debug.Log("REPAIR_BUILD_RESULT " + report.summary.result +
            " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings);
        foreach (var step in report.steps)
            foreach (var message in step.messages)
                if (message.type == LogType.Warning || message.type == LogType.Error)
                    Debug.Log("BUILD_DIAGNOSTIC " + message.content);
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    public static void InspectBindings()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/level1.unity");
        foreach (var intro in Object.FindObjectsOfType<BossIntroSequenceDirector>(true))
        {
            intro.AutoConfigure();
            var data = new SerializedObject(intro);
            foreach (string field in new[] { "bossHealthBar", "bossHealthSlider", "introText", "bossCombatUiGroup" })
            {
                Object value = data.FindProperty(field).objectReferenceValue;
                Debug.Log("BINDING_ID " + intro.name + " " + field + " -> " +
                    (value == null ? "null" : value.name + " " + GlobalObjectId.GetGlobalObjectIdSlow(value).targetObjectId));
            }
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static FieldInfo Field(object target, string name)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, Private);
            if (field != null) return field;
        }
        throw new MissingFieldException(target.GetType().Name, name);
    }
    private static object Get(object target, string name) => Field(target, name).GetValue(target);
    private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static object Invoke(object target, string name, params object[] args)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            var method = type.GetMethod(name, Private);
            if (method != null) return method.Invoke(target, args);
        }
        throw new MissingMethodException(target.GetType().Name, name);
    }
    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
