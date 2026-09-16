using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class DemonBossCombatValidation
{
    private const string DataPath = "Assets/Data/Demon fire breath hitboxes.json";
    private const string SpriteDirectory = "Assets/Graphics/boss/boss/individual sprites/09_demon_fire_breath/";
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int assertions;

    [MenuItem("Tools/Combat/Validate Demon Boss")]
    public static void Run()
    {
        try
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Run these isolated checks outside Play mode.");
            assertions = 0;
            ValidateSelection();
            ValidateFireGeometry();
            ValidateClipAndSceneBinding();
            string report = $"PASS: Demon boss combat, {assertions} assertions. " +
                "Committed melee selection, cooldowns, disabled weights, repeat limits, " +
                "21 sprite frames, transforms, body exclusion and scene/animation bindings.";
            Directory.CreateDirectory("Logs/CombatValidation");
            File.WriteAllText("Logs/CombatValidation/unity-validation.txt", report);
            Debug.Log(report);
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    private static void ValidateSelection()
    {
        GameObject host = new GameObject("DemonSelectionValidation");
        host.SetActive(false);
        GameObject target = new GameObject("DemonSelectionTarget");
        GameObject bodyHost = new GameObject("DemonSelectionBody");
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        try
        {
            Enemy_DemonBoss boss = host.AddComponent<Enemy_DemonBoss>();
            Rigidbody2D body = bodyHost.AddComponent<Rigidbody2D>();
            typeof(Entity).GetProperty("rb").SetValue(boss, body);
            target.transform.position = Vector3.right * 8f;
            Set(boss, "player", target.transform);
            Set(boss, "visualRoot", host.transform);
            Set(boss, "cleaveWeight", 1f);
            Set(boss, "smashWeight", 0f);
            Set(boss, "fireBreathWeight", 0f);
            Set(boss, "enableCastSpellAttack", false);
            Invoke(boss, "UpdateDemonPhase");
            Require((bool)Get(boss, "hasQueuedDemonAttack"), "No committed attack at medium range.");
            Require(Get(boss, "queuedDemonAttack").ToString() == "Cleave", "Wrong initial melee choice.");
            Set(boss, "fireBreathWeight", 1000f);
            UnityEngine.Random.InitState(3124);
            for (int i = 0; i < 180; i++)
            {
                Invoke(boss, "UpdateDemonPhase");
                Require(Get(boss, "queuedDemonAttack").ToString() == "Cleave", "Melee was rerolled during approach.");
            }
            Require(body.velocity.x > 0f, "Boss failed to approach its chosen melee attack.");

            Set(boss, "cleaveWeight", 0f);
            Set(boss, "fireBreathWeight", 0f);
            Require(!Choose(boss, out _), "Zero-weight attacks must stay disabled.");

            Set(boss, "cleaveWeight", 50f);
            Set(boss, "smashWeight", 40f);
            Set(boss, "fireBreathWeight", 10f);
            Set(boss, "lastCleaveTime", Time.time);
            Set(boss, "lastSmashTime", Time.time);
            Set(boss, "lastFireBreathTime", Time.time);
            Require(!Choose(boss, out _), "Cooldowns were bypassed.");
            string previous = null;
            for (int i = 0; i < 300; i++)
            {
                Set(boss, "lastCleaveTime", -999f);
                Set(boss, "lastSmashTime", -999f);
                Set(boss, "lastFireBreathTime", -999f);
                Require(Choose(boss, out object attack), "No eligible attack selected.");
                Require(attack.ToString() != previous, "Consecutive-repeat limit was ignored.");
                Require(attack.ToString() != "CastSpell", "Disabled spell was selected.");
                Invoke(boss, "RecordDemonAttackUse", attack);
                previous = attack.ToString();
            }

            Set(boss, "smashWeight", 0f);
            Set(boss, "fireBreathWeight", 0f);
            for (int i = 0; i < 3; i++)
            {
                Set(boss, "lastCleaveTime", -999f);
                Require(Choose(boss, out object attack) && attack.ToString() == "Cleave", "Single enabled attack was blocked by repeat limit.");
                Invoke(boss, "RecordDemonAttackUse", attack);
                Require(!Choose(boss, out _), "Single-attack fallback bypassed cooldown.");
            }
        }
        finally
        {
            UnityEngine.Random.state = randomState;
            UnityEngine.Object.DestroyImmediate(bodyHost);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private static bool Choose(Enemy_DemonBoss boss, out object attack)
    {
        object[] arguments = { null };
        bool chosen = (bool)typeof(Enemy_DemonBoss).GetMethod("TryChooseAttack", PrivateInstance).Invoke(boss, arguments);
        attack = arguments[0];
        return chosen;
    }

    private static void ValidateFireGeometry()
    {
        TextAsset data = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
        Require(data != null, "Missing fire data asset.");
        DemonFireBreathHitbox hitbox = new DemonFireBreathHitbox(data);
        GameObject visual = new GameObject("DemonFlameValidation");
        GameObject probe = new GameObject("DemonFlameProbe");
        try
        {
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            BoxCollider2D collider = probe.AddComponent<BoxCollider2D>();
            probe.layer = 31;
            collider.size = Vector2.one * 0.01f;
            ContactFilter2D filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << 31, useTriggers = true };
            Collider2D[] results = new Collider2D[8];
            // Far from scene content; validate sprite flips and Transform flips independently.
            visual.transform.position = new Vector3(10000f, 10000f, 0f);
            for (int configuration = 0; configuration < 8; configuration++)
            {
                renderer.flipX = (configuration & 1) != 0;
                renderer.flipY = (configuration & 2) != 0;
                visual.transform.localScale = (configuration & 4) != 0 ? new Vector3(-1.7f, 0.8f, 1f) : Vector3.one;
                visual.transform.rotation = Quaternion.Euler(0f, 0f, configuration * 11f);
                for (int frame = 1; frame <= 21; frame++)
                {
                    renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDirectory + $"demon_fire_breath_{frame}.png");
                    Require(renderer.sprite != null, "Missing breath sprite.");
                    SetProbeAtPixel(renderer, probe.transform, new Vector2(114.5f, 82.5f));
                    Require(!hitbox.Overlaps(renderer, filter, results), "Head torch is damaging.");
                    SetProbeAtPixel(renderer, probe.transform, new Vector2(145f, 126f));
                    Require(!hitbox.Overlaps(renderer, filter, results), "Boss body is damaging.");
                    if (frame == 12 || frame == 16)
                    {
                        Vector2 flame = frame == 12 ? new Vector2(60.5f, 95.5f) : new Vector2(60.5f, 140.5f);
                        SetProbeAtPixel(renderer, probe.transform, flame);
                        Require(hitbox.Overlaps(renderer, filter, results), $"Visible flame missing at frame {frame}, transform {configuration}.");
                        SetProbeAtPixel(renderer, probe.transform, frame == 12 ? new Vector2(60.5f, 140.5f) : new Vector2(60.5f, 95.5f));
                        Require(!hitbox.Overlaps(renderer, filter, results), "Swept empty area still causes damage.");
                    }
                    if (frame < 7 || frame > 17)
                    {
                        collider.size = Vector2.one * 50f;
                        probe.transform.position = visual.transform.position;
                        Physics2D.SyncTransforms();
                        Require(!hitbox.Overlaps(renderer, filter, results), "Windup/smoke frame causes damage.");
                        collider.size = Vector2.one * 0.01f;
                    }
                }
            }
            renderer.sprite = null;
            Require(!hitbox.Overlaps(renderer, filter, results), "Missing sprite fell back to a broad hitbox.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
            UnityEngine.Object.DestroyImmediate(visual);
        }
    }

    private static void SetProbeAtPixel(SpriteRenderer renderer, Transform probe, Vector2 pixel)
    {
        Sprite sprite = renderer.sprite;
        Vector2 local = new Vector2(pixel.x - sprite.pivot.x, sprite.rect.height - pixel.y - sprite.pivot.y) / sprite.pixelsPerUnit;
        if (renderer.flipX) local.x = -local.x;
        if (renderer.flipY) local.y = -local.y;
        probe.position = renderer.transform.TransformPoint(local);
        Physics2D.SyncTransforms();
    }

    private static void ValidateClipAndSceneBinding()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/Animations/Boss/Demon/d_fire_breath.anim");
        Require(clip != null && !clip.isLooping, "Breath attack must not loop.");
        bool start = false;
        bool end = false;
        foreach (AnimationEvent entry in clip.events)
        {
            if (entry.functionName == "FireBreathStart") start = Mathf.Abs(entry.time - 0.4f) < 0.001f;
            if (entry.functionName == "FireBreathEnd") end = Mathf.Abs(entry.time - 17f / 15f) < 0.001f;
        }
        Require(start && end, "Breath event window does not match the visible frames.");
        string scene = File.ReadAllText("Assets/Scenes/level1.unity");
        Require(scene.Contains("fireBreathHitboxData: {fileID: 4900000, guid: " + AssetDatabase.AssetPathToGUID(DataPath)), "level1 fire data is not assigned.");
        Require(scene.Contains("useSpriteFireBreathHitbox: 1"), "level1 still uses its legacy fire box.");
    }

    private static void Set(object target, string field, object value) => target.GetType().GetField(field, PrivateInstance).SetValue(target, value);
    private static object Get(object target, string field) => target.GetType().GetField(field, PrivateInstance).GetValue(target);
    private static object Invoke(object target, string method, params object[] arguments) => target.GetType().GetMethod(method, PrivateInstance).Invoke(target, arguments);
    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
