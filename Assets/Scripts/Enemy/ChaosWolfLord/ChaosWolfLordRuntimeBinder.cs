using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChaosWolfLordRuntimeBinder
{
    private const string WolfLordObjectName = "\u6df7\u6c8c\u72fc\u4e3b";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindCurrentScene()
    {
        BindChaosWolfLordInScene(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _ = mode;
        BindChaosWolfLordInScene(scene);
    }

    private static void BindChaosWolfLordInScene(Scene scene)
    {
        GameObject wolfRoot = FindWolfLordRoot(scene);

        if (wolfRoot == null)
            return;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            wolfRoot.layer = enemyLayer;

        if (!wolfRoot.TryGetComponent(out Enemy_ChaosWolfLord _))
            wolfRoot.AddComponent<Enemy_ChaosWolfLord>();

        if (!wolfRoot.TryGetComponent(out ChaosWolfLordStats _))
            wolfRoot.AddComponent<ChaosWolfLordStats>();

        if (!wolfRoot.TryGetComponent(out GuardedChestReveal _))
            wolfRoot.AddComponent<GuardedChestReveal>();

        if (!wolfRoot.TryGetComponent(out BossPhaseTransitionDirector _))
            wolfRoot.AddComponent<BossPhaseTransitionDirector>();

        BossIntroSequenceDirector introDirector = wolfRoot.GetComponent<BossIntroSequenceDirector>();
        if (introDirector == null)
            introDirector = wolfRoot.AddComponent<BossIntroSequenceDirector>();

        introDirector.ConfigureAutoResolveNames(WolfLordObjectName, "\u72fc\u4e3b");
        introDirector.AutoConfigure();

        if (!wolfRoot.TryGetComponent(out EntityFX _))
            wolfRoot.AddComponent<EntityFX>();

        Transform animatorTransform = wolfRoot.GetComponentInChildren<Animator>(true)?.transform;

        if (animatorTransform != null && !animatorTransform.TryGetComponent(out ChaosWolfLordAnimationTriggers _))
            animatorTransform.gameObject.AddComponent<ChaosWolfLordAnimationTriggers>();

        EnsureChildPoint(wolfRoot.transform, "AttackCheck", new Vector3(-0.35f, 0.35f, 0f));
        EnsureChildPoint(wolfRoot.transform, "GroundCheck", new Vector3(0f, -1.55f, 0f));
        EnsureChildPoint(wolfRoot.transform, "WallCheck", new Vector3(-1.25f, -0.15f, 0f));
        EnsureAttackHitboxes(wolfRoot.transform);
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        if (!scene.IsValid())
            scene = SceneManager.GetActiveScene();

        GameObject[] rootObjects = scene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform found = FindChildRecursive(rootObjects[i].transform, objectName);

            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);

            if (found != null)
                return found;
        }

        return null;
    }

    private static void EnsureChildPoint(Transform root, string childName, Vector3 localPosition)
    {
        Transform child = root.Find(childName);

        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(root, false);
        }

        child.localPosition = localPosition;
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

        EnsureHitbox(container, "Attack1_Hit1", new Vector2(-1.3f, 0.45f), new Vector2(2.6f, 1.3f));
        EnsureHitbox(container, "Attack1_Hit2", new Vector2(-1.8f, 0.35f), new Vector2(3f, 1.4f));
        EnsureHitbox(container, "Attack2_Hit1", new Vector2(-1.4f, 0.55f), new Vector2(2.8f, 1.5f));
        EnsureHitbox(container, "Attack2_Hit2", new Vector2(-2f, 0.45f), new Vector2(3.4f, 1.6f));
        EnsureHitbox(container, "Attack3_Hit1", new Vector2(-1.9f, 0.55f), new Vector2(3.6f, 1.8f));
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

    private static GameObject FindWolfLordRoot(Scene scene)
    {
        if (!scene.IsValid())
            scene = SceneManager.GetActiveScene();

        Enemy_ChaosWolfLord[] existingWolves = Object.FindObjectsOfType<Enemy_ChaosWolfLord>(true);

        for (int i = 0; i < existingWolves.Length; i++)
        {
            Enemy_ChaosWolfLord wolf = existingWolves[i];

            if (wolf == null || wolf.gameObject.scene != scene)
                continue;

            if (wolf.name == WolfLordObjectName)
                return wolf.gameObject;
        }

        for (int i = 0; i < existingWolves.Length; i++)
        {
            Enemy_ChaosWolfLord wolf = existingWolves[i];

            if (wolf != null && wolf.gameObject.scene == scene && IsLikelyWolfLordRoot(wolf.gameObject))
                return wolf.gameObject;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform found = FindLikelyWolfChildRecursive(rootObjects[i].transform);

            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindLikelyWolfChildRecursive(Transform parent)
    {
        if (parent == null)
            return null;

        if (parent.name == WolfLordObjectName && IsLikelyWolfLordRoot(parent.gameObject))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindLikelyWolfChildRecursive(parent.GetChild(i));

            if (found != null)
                return found;
        }

        return null;
    }

    private static bool IsLikelyWolfLordRoot(GameObject candidate)
    {
        if (candidate == null)
            return false;

        if (candidate.transform is RectTransform)
            return false;

        return candidate.GetComponent<Rigidbody2D>() != null ||
               candidate.GetComponent<Collider2D>() != null ||
               candidate.GetComponentInChildren<Animator>(true) != null;
    }
}
