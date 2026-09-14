using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemonBossRuntimeBinder
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindCurrentScene()
    {
        BindDemonBossInScene();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _ = scene;
        _ = mode;
        BindDemonBossInScene();
    }

    private static void BindDemonBossInScene()
    {
        GameObject bossRoot = ResolveDemonBossRoot();

        if (bossRoot == null)
            return;

        Enemy_DemonBoss boss = bossRoot.GetComponent<Enemy_DemonBoss>();

        if (boss == null)
            boss = bossRoot.AddComponent<Enemy_DemonBoss>();

        if (!bossRoot.TryGetComponent(out DemonBossStats stats))
            stats = bossRoot.AddComponent<DemonBossStats>();

        if (!bossRoot.TryGetComponent(out BossPhaseTransitionDirector director))
            director = bossRoot.AddComponent<BossPhaseTransitionDirector>();

        BossIntroSequenceDirector introDirector = bossRoot.GetComponent<BossIntroSequenceDirector>();
        if (introDirector == null)
            introDirector = bossRoot.AddComponent<DemonBossIntroDirector>();

        introDirector.ConfigureAutoResolveNames("\u8d64\u9ad3boss", "\u8d64\u9ad3");
        introDirector.AutoConfigure();

        if (!bossRoot.TryGetComponent(out EntityFX entityFx))
            entityFx = bossRoot.AddComponent<EntityFX>();

        _ = stats;
        _ = director;
        _ = introDirector;
        _ = entityFx;

        Transform animatorTransform = bossRoot.transform.Find("Demon/Animator");

        if (animatorTransform == null)
            animatorTransform = bossRoot.GetComponentInChildren<Animator>(true)?.transform;

        if (animatorTransform != null && !animatorTransform.TryGetComponent(out DemonBossAnimationTriggers _))
            animatorTransform.gameObject.AddComponent<DemonBossAnimationTriggers>();

        EnsureAttackHitboxes(bossRoot.transform);
        EnsureDefeatCollapseDirector(bossRoot, boss, director);
    }

    private static void EnsureDefeatCollapseDirector(GameObject bossRoot, Enemy_DemonBoss boss, BossPhaseTransitionDirector director)
    {
        GameObject sceneAudio = GameObject.Find("SceneAudio");

        if (sceneAudio == null)
            sceneAudio = new GameObject("SceneAudio");

        DemonBossDefeatCollapseDirector collapseDirector =
            sceneAudio.GetComponent<DemonBossDefeatCollapseDirector>();

        if (collapseDirector == null)
            collapseDirector = sceneAudio.AddComponent<DemonBossDefeatCollapseDirector>();

        collapseDirector.Configure(boss, director);
        _ = bossRoot;
    }

    private static void EnsureAttackHitboxes(Transform root)
    {
        Transform container = root.Find("AttackHitboxes");

        if (container == null)
        {
            GameObject containerObject = new GameObject("AttackHitboxes");
            container = containerObject.transform;
            container.SetParent(root, false);
            container.localPosition = Vector3.zero;
            container.localRotation = Quaternion.identity;
            container.localScale = Vector3.one;
        }

        EnsureHitbox(container, "Phase1_Contact", new Vector2(-0.2f, 0.25f), new Vector2(1.5f, 1.2f));
        EnsureHitbox(container, "Phase2_Cleave", new Vector2(-1.45f, 0.45f), new Vector2(3f, 1.6f));
        EnsureHitbox(container, "Phase2_Smash", new Vector2(-1f, -0.15f), new Vector2(2.4f, 1.2f));
        EnsureHitbox(container, "Phase2_FireBreath", new Vector2(-2.3f, 0.55f), new Vector2(4.6f, 1.5f));
    }

    private static void EnsureHitbox(Transform container, string hitboxName, Vector2 localPosition, Vector2 size)
    {
        Transform hitboxTransform = container.Find(hitboxName);
        bool createdTransform = false;

        if (hitboxTransform == null)
        {
            GameObject hitboxObject = new GameObject(hitboxName);
            hitboxTransform = hitboxObject.transform;
            hitboxTransform.SetParent(container, false);
            hitboxTransform.localPosition = localPosition;
            hitboxTransform.localRotation = Quaternion.identity;
            hitboxTransform.localScale = Vector3.one;
            createdTransform = true;
        }

        BoxCollider2D hitbox = hitboxTransform.GetComponent<BoxCollider2D>();
        bool createdCollider = false;

        if (hitbox == null)
        {
            hitbox = hitboxTransform.gameObject.AddComponent<BoxCollider2D>();
            createdCollider = true;
        }

        if (createdTransform || createdCollider)
        {
            hitbox.offset = Vector2.zero;
            hitbox.size = size;
        }

        hitbox.isTrigger = true;
    }

    private static GameObject ResolveDemonBossRoot()
    {
        Enemy_DemonBoss[] bosses = Object.FindObjectsOfType<Enemy_DemonBoss>(true);

        if (bosses != null && bosses.Length > 0)
        {
            for (int i = 0; i < bosses.Length; i++)
            {
                if (bosses[i] != null && bosses[i].gameObject.scene.IsValid())
                    return bosses[i].gameObject;
            }
        }

        GameObject activeBoss = GameObject.Find("Boss");

        if (activeBoss != null)
            return activeBoss;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];

            if (target == null || !target.gameObject.scene.IsValid())
                continue;

            if (target.name == "Boss")
                return target.gameObject;
        }

        return null;
    }
}
