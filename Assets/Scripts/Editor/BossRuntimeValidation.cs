using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Runs in an empty, unsaved scene: no player, inventory or save data is changed.
[InitializeOnLoad]
public static class BossRuntimeValidation
{
    private const string SessionKey = "BossRuntimeValidation.Active";
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private static Enemy[] bosses;
    private static AudioEventPlayer.AudioCue[] cues;
    private static int stage;
    private static int assertions;
    private static double startedAt;
    private static float stageTime;
    private static string failure;

    static BossRuntimeValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(SessionKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            startedAt = EditorApplication.timeSinceStartup;
            stage = 0;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(SessionKey, false);
            bool passed = failure == null && stage == 5;
            if (passed) Debug.Log("BOSS_RUNTIME_VALIDATION_PASS assertions=" + assertions);
            else Debug.LogError("BOSS_RUNTIME_VALIDATION_FAIL " + failure);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (SessionState.GetBool(SessionKey, false) &&
            (type == LogType.Error || type == LogType.Exception))
            failure = failure ?? message;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(SessionKey, false) || !EditorApplication.isPlaying) return;
        try
        {
            RequireTimeLimit();
            if (failure != null) throw new InvalidOperationException(failure);
            if (stage == 0)
            {
                bosses = new Enemy[] { Create<Enemy_ChaosWolfLord>("Wolf runtime test"),
                    Create<Enemy_DemonBoss>("Demon runtime test") };
                cues = new AudioEventPlayer.AudioCue[2];
                for (int i = 0; i < bosses.Length; i++)
                {
                    var audio = bosses[i].gameObject.AddComponent<AudioEventPlayer>();
                    cues[i] = new AudioEventPlayer.AudioCue { cueName = "hurt",
                        clips = new[] { AudioClip.Create("Silent test cue", 64, 1, 44100, false) } };
                    Set(audio, "cues", new[] { cues[i] });
                    Set(bosses[i], "advancedSuperArmorDuration", 1f);
                    bosses[i].GetComponent<Rigidbody2D>().gravityScale = 0f;
                }
                Set(bosses[1], "useSpriteFireBreathHitbox", false);
                stageTime = Time.time;
                stage = 1;
            }
            else if (stage == 1 && Time.time > stageTime + 0.1f)
            {
                SetEnum(bosses[1], "currentPhase", "Demon");
                for (int i = 0; i < bosses.Length; i++)
                {
                    Enemy boss = bosses[i];
                    boss.stats.maxHealth.SetDefaultValue(10000);
                    boss.stats.currentHealth = 10000;
                    for (int hit = 0; hit < 3; hit++) boss.stats.TakeDamage(1);
                    Require((bool)Get(boss, "advancedSuperArmor"), "Third stagger did not arm protection.");
                    string action = i == 0 ? "actionRoutine" : "currentActionRoutine";
                    object lastStagger = Get(boss, action);
                    cues[i].nextAllowedTime = -999f;
                    boss.stats.TakeDamage(1);
                    boss.stats.TakeDamage(1);
                    Require(ReferenceEquals(lastStagger, Get(boss, action)), "Rapid hits restarted protected stagger.");
                    Require(boss.stats.currentHealth == 9995, "Armor must not prevent health damage.");
                    Require(cues[i].nextAllowedTime >= Time.time, "Hurt sound was skipped during armor.");
                    cues[i].nextAllowedTime = -999f;
                    boss.stats.TakeDamage(0);
                    Require(cues[i].nextAllowedTime == -999f, "Zero damage played a hurt sound.");
                }
                stage = 2;
            }
            else if (stage == 2 && Get(bosses[0], "currentState").ToString() == "Idle" &&
                Get(bosses[1], "currentMode").ToString() == "Idle")
            {
                foreach (Enemy boss in bosses)
                {
                    Require((bool)Get(boss, "advancedSuperArmor"), "Armor disappeared at stagger completion.");
                    ((IPreciseDodgeTimeStopTarget)boss).SetPreciseDodgeTimeStop(true);
                }
                stageTime = Time.realtimeSinceStartup;
                stage = 3;
            }
            else if (stage == 3 && Time.realtimeSinceStartup > stageTime + 1.2f)
            {
                foreach (Enemy boss in bosses)
                {
                    Require((bool)Get(boss, "advancedSuperArmor"), "Time stop consumed the armor timer.");
                    ((IPreciseDodgeTimeStopTarget)boss).SetPreciseDodgeTimeStop(false);
                }
                stageTime = Time.time;
                stage = 4;
            }
            else if (stage == 4 && Time.time > stageTime + 1.2f)
            {
                for (int i = 0; i < bosses.Length; i++)
                {
                    Require(!(bool)Get(bosses[i], "advancedSuperArmor"), "Armor failed to expire after resume.");
                    string state = i == 0 ? "currentState" : "currentMode";
                    SetEnum(bosses[i], state, "Attack");
                    cues[i].nextAllowedTime = -999f;
                    bosses[i].stats.TakeDamage(1);
                    Require(Get(bosses[i], state).ToString() == "Attack", "Attack armor allowed interruption.");
                    Require(cues[i].nextAllowedTime >= Time.time, "Attack armor suppressed hurt sound.");
                    Object.Destroy(bosses[i].gameObject);
                    Object.Destroy(cues[i].clips[0]);
                }
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

    private static T Create<T>(string name) where T : Enemy
    {
        var host = new GameObject(name);
        host.AddComponent<SpriteRenderer>();
        return host.AddComponent<T>();
    }
    private static FieldInfo Field(object target, string name) =>
        target.GetType().GetField(name, Fields) ?? throw new MissingFieldException(target.GetType().Name, name);
    private static object Get(object target, string name) => Field(target, name).GetValue(target);
    private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static void SetEnum(object target, string name, string value) =>
        Set(target, name, Enum.Parse(Field(target, name).FieldType, value));
    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void RequireTimeLimit()
    {
        if (EditorApplication.timeSinceStartup - startedAt > 20)
            throw new TimeoutException("Boss runtime validation exceeded twenty seconds.");
    }
}
