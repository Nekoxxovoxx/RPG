using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WolfDashValidation
{
    private const string SessionKey = "WolfDashValidation.Active";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int assertions;
    private static string failure;
    private static Enemy_ChaosWolfLord wolf;
    private static Player player;

    static WolfDashValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(SessionKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (SessionState.GetBool(SessionKey, false) &&
            (type == LogType.Error || type == LogType.Exception)) failure = failure ?? message;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
            EditorApplication.delayCall += Check;
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(SessionKey, false);
            bool passed = failure == null && assertions > 100;
            if (passed) Debug.Log("WOLF_DASH_VALIDATION_PASS assertions=" + assertions);
            else Debug.LogError("WOLF_DASH_VALIDATION_FAIL " + failure);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    private static void Check()
    {
        SimulationMode2D oldMode = Physics2D.simulationMode;
        float oldStep = Time.fixedDeltaTime;
        try
        {
            // Manual physics steps keep fixtures inside one callback: no Player
            // Start, manager or save lifecycle is run against the user's progress.
            var playerHost = new GameObject("Dash target");
            playerHost.SetActive(false);
            var stats = playerHost.AddComponent<PlayerStats>();
            Set(stats, "saveLevelProgress", false);
            player = playerHost.AddComponent<Player>();
            playerHost.SetActive(true);
            stats.currentHealth = 100;
            var wolfHost = new GameObject("Dash wolf", typeof(SpriteRenderer));
            wolf = wolfHost.AddComponent<Enemy_ChaosWolfLord>();
            Call(wolf, "Start");
            wolf.rb.gravityScale = 0;
            wolf.rb.interpolation = RigidbodyInterpolation2D.None;
            wolf.stats.EnsureCoreStatObjects();
            wolf.stats.currentHealth = 100;
            Set(wolf, "player", player.transform);
            Set(wolf, "bossCombatStarted", true);
            Set(wolf, "chaseMoveSpeed", 9.2f);
            Physics2D.simulationMode = SimulationMode2D.Script;

            Reset(6);
            Require((bool)Call(wolf, "CanStartDashChase"), "Mid-range target must allow a burst.");
            player.transform.position = new Vector3(3, 0);
            Require(!(bool)Call(wolf, "CanStartDashChase"), "Close combat must not start a burst.");
            player.transform.position = new Vector3(11, 0);
            Require(!(bool)Call(wolf, "CanStartDashChase"), "Distant targets must use normal pursuit first.");
            player.transform.position = new Vector3(6, 4);
            Require(!(bool)Call(wolf, "CanStartDashChase"), "Different-height targets must not waste a burst.");
            player.transform.position = new Vector3(6, 0);
            Set(wolf, "lastDashTime", Time.time - 4.9f);
            Require(!(bool)Call(wolf, "CanStartDashChase"), "Five-second cooldown must be preserved.");
            Set(wolf, "lastDashTime", Time.time - 5.01f);
            Require((bool)Call(wolf, "CanStartDashChase"), "Burst must become ready after cooldown.");

            float previousDistance = -1;
            foreach (float step in new[] { 0.01f, 0.02f, 0.04f })
            {
                Time.fixedDeltaTime = step;
                Reset(6);
                float startHealth = stats.currentHealth;
                RunBurst(null);
                Require(wolf.transform.position.x > 2 && wolf.transform.position.x <= 3.501f,
                    "Burst must close the gap without crossing its stopping distance.");
                Require(stats.currentHealth == startHealth, "Movement burst must not deal contact damage.");
                if (previousDistance >= 0)
                    Require(Mathf.Abs(wolf.transform.position.x - previousDistance) < 0.35f,
                        "Travel must remain stable across physics step sizes.");
                previousDistance = wolf.transform.position.x;

                Reset(-6);
                RunBurst(null);
                Require(wolf.transform.position.x < -2 && wolf.transform.position.x >= -3.501f,
                    "Leftward burst must mirror rightward stopping behavior.");
            }

            Time.fixedDeltaTime = 0.02f;
            Reset(6);
            RunBurst(i => { if (i == 2) player.transform.position = new Vector3(6, 6); });
            Require(wolf.transform.position.x <= 3.501f, "Jumping target must not defeat horizontal braking.");
            Reset(6);
            RunBurst(i => { if (i == 2) player.transform.position = new Vector3(-1, 0); });
            Require(wolf.facingDir == 1 && wolf.transform.position.x >= 0,
                "Crossing behind the wolf must not produce a homing turnaround.");

            Reset(9);
            RunBurst(null);
            float normalTravel = wolf.transform.position.x;
            Reset(9);
            RunBurst(i =>
            {
                if (i != 3) return;
                wolf.SetPreciseDodgeTimeStop(true);
            }, true);
            Require(Mathf.Abs(wolf.transform.position.x - normalTravel) < 0.02f,
                "Time stop must pause, not consume, the burst motion timeline.");

            Reset(6);
            RunBurst(i => { if (i == 2) typeof(CharacterStats).GetProperty("isDead").SetValue(stats, true); });
            Require(wolf.rb.velocity == Vector2.zero, "Losing the target must stop burst motion.");
            typeof(CharacterStats).GetProperty("isDead").SetValue(stats, false);
            Reset(6);
            Call(wolf, "StartDashChase");
            wolf.DamageImpact();
            Require(Get(wolf, "currentState").ToString() == "Hit" && wolf.rb.velocity == Vector2.zero,
                "A valid hit reaction must still interrupt a burst.");
            wolf.SetIntroPresentationLocked(true);
        }
        catch (Exception ex) { failure = ex.ToString(); }
        finally
        {
            Physics2D.simulationMode = oldMode;
            Time.fixedDeltaTime = oldStep;
            if (wolf != null) { wolf.gameObject.SetActive(false); Object.Destroy(wolf.gameObject); }
            if (player != null) { player.gameObject.SetActive(false); Object.Destroy(player.gameObject); }
            EditorApplication.ExitPlaymode();
        }
    }

    private static void Reset(float targetX)
    {
        wolf.SetPreciseDodgeTimeStop(false);
        wolf.rb.position = Vector2.zero;
        wolf.transform.position = Vector3.zero;
        player.transform.position = new Vector3(targetX, 0);
        wolf.rb.velocity = new Vector2(Mathf.Sign(targetX) * 9.2f, 0);
        Set(wolf, "lastDashTime", -999f);
        Physics2D.SyncTransforms();
    }

    private static void RunBurst(Action<int> beforeStep, bool testPause = false)
    {
        IEnumerator routine = (IEnumerator)Call(wolf, "DashChaseRoutine");
        int steps = 0;
        bool recovered = false;
        bool paused = false;
        float peak = 0;
        float lastSpeed = 0;
        while (routine.MoveNext())
        {
            Require(++steps < 100, "Burst must finish within a bounded number of physics steps.");
            if (routine.Current is IEnumerator)
            {
                Require(wolf.rb.velocity == Vector2.zero && Get(wolf, "currentState").ToString() == "Dash",
                    "Recovery must stop movement and retain the non-attacking Dash state.");
                recovered = true;
                continue;
            }
            if (testPause && !paused && (bool)Get(wolf, "preciseDodgeTimeStopped"))
            {
                Vector3 frozenPosition = wolf.transform.position;
                for (int i = 0; i < 8; i++)
                {
                    Require(routine.MoveNext(), "Time stop must not finish the burst.");
                    Physics2D.Simulate(Time.fixedDeltaTime);
                    Require(Vector3.Distance(frozenPosition, wolf.transform.position) < 0.001f,
                        "Time-stopped burst must not move.");
                }
                wolf.SetPreciseDodgeTimeStop(false);
                paused = true;
                // Resume the same paused step before advancing physics.
                Require(routine.MoveNext(), "Burst must resume after time stop.");
            }
            float speed = Mathf.Abs(wolf.rb.velocity.x);
            if (steps == 1)
                Require(Mathf.Abs(speed - 9.2f) < 0.001f, "Burst must start at current run speed, without a forced stop.");
            Require(speed <= 14.001f, "Burst speed must stay within the configured peak.");
            peak = Mathf.Max(peak, speed);
            lastSpeed = speed;
            Physics2D.Simulate(Time.fixedDeltaTime);
            beforeStep?.Invoke(steps);
        }
        Require(recovered && Get(wolf, "currentState").ToString() == "Idle", "Burst must complete recovery and return control to combat AI.");
        Require(wolf.rb.velocity == Vector2.zero, "Burst must leave no residual velocity.");
        if (beforeStep == null)
            Require(peak > 9.2f && lastSpeed < peak, "Normal burst must accelerate and then brake.");
    }

    private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static object Call(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
