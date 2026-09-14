using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerOpeningAwakingUiPresenter : MonoBehaviour
{
    private const string DefaultSceneName = "level1";
    private const string DefaultOracleUiName = "\u706B\u8C15_UI";

    [SerializeField] private string targetSceneName = DefaultSceneName;
    [SerializeField] private string oracleUiName = DefaultOracleUiName;
    [SerializeField] private GameObject oracleUi;
    [SerializeField] private Player player;
    [SerializeField] private bool hideWhenNotOpeningAwaking = true;

    private Player subscribedPlayer;
    private Transform originalParent;
    private int originalSiblingIndex = -1;
    private bool originalActiveSelf;
    private bool hasOriginalLayout;
    private RectTransform oracleRect;
    private RectTransformSnapshot originalRectSnapshot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _ = mode;
        BindScene(scene);
    }

    private static void BindScene(Scene scene)
    {
        if (!scene.IsValid() || !string.Equals(scene.name, DefaultSceneName, System.StringComparison.OrdinalIgnoreCase))
            return;

        PlayerOpeningAwakingUiPresenter existing = FindObjectOfType<PlayerOpeningAwakingUiPresenter>(true);

        if (existing != null)
            return;

        GameObject oracle = FindSceneGameObjectByName(scene, DefaultOracleUiName);

        if (oracle == null)
            return;

        GameObject host = new GameObject("PlayerOpeningAwakingUiPresenter");

        PlayerOpeningAwakingUiPresenter presenter = host.AddComponent<PlayerOpeningAwakingUiPresenter>();
        presenter.Configure(oracle, scene.name);
    }

    public void Configure(GameObject targetOracleUi, string sceneName)
    {
        oracleUi = targetOracleUi;

        if (!string.IsNullOrWhiteSpace(sceneName))
            targetSceneName = sceneName;

        ResolveReferences();
        SubscribeToPlayer();
        RefreshVisibility();
    }

    private void Awake()
    {
        ResolveReferences();

        if (hideWhenNotOpeningAwaking)
            SetOracleVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToPlayer();
        RefreshVisibility();
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeToPlayer();
        RefreshVisibility();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayer();

        if (hideWhenNotOpeningAwaking)
            SetOracleVisible(false);
    }

    private void ResolveReferences()
    {
        if (oracleUi == null)
            oracleUi = FindSceneGameObjectByName(gameObject.scene, oracleUiName);

        if (player == null)
            player = FindObjectOfType<Player>();
    }

    private void SubscribeToPlayer()
    {
        if (player == null || subscribedPlayer == player)
            return;

        UnsubscribeFromPlayer();
        subscribedPlayer = player;
        subscribedPlayer.OnOpeningAwakingSequenceActiveChanged += HandleOpeningAwakingChanged;
    }

    private void UnsubscribeFromPlayer()
    {
        if (subscribedPlayer == null)
            return;

        subscribedPlayer.OnOpeningAwakingSequenceActiveChanged -= HandleOpeningAwakingChanged;
        subscribedPlayer = null;
    }

    private void HandleOpeningAwakingChanged(bool active)
    {
        SetOracleVisible(active);
    }

    private void RefreshVisibility()
    {
        bool shouldShow = subscribedPlayer != null && subscribedPlayer.IsOpeningAwakingSequenceActive;
        SetOracleVisible(shouldShow);
    }

    private void SetOracleVisible(bool visible)
    {
        if (oracleUi == null)
            return;

        CaptureOriginalLayoutIfNeeded();

        if (visible)
        {
            Transform visibleParent = ResolveVisibleParent();

            if (visibleParent != null && oracleUi.transform.parent != visibleParent)
            {
                oracleUi.transform.SetParent(visibleParent, false);
                RestoreRectSnapshot();
            }

            oracleUi.transform.SetAsLastSibling();
            oracleUi.SetActive(true);
            return;
        }

        oracleUi.SetActive(false);
        RestoreOriginalParent();
    }

    private void CaptureOriginalLayoutIfNeeded()
    {
        if (hasOriginalLayout || oracleUi == null)
            return;

        Transform oracleTransform = oracleUi.transform;
        originalParent = oracleTransform.parent;
        originalSiblingIndex = oracleTransform.GetSiblingIndex();
        originalActiveSelf = oracleUi.activeSelf;
        oracleRect = oracleUi.GetComponent<RectTransform>();
        originalRectSnapshot = RectTransformSnapshot.Capture(oracleRect, oracleTransform);
        hasOriginalLayout = true;
    }

    private Transform ResolveVisibleParent()
    {
        UI ui = FindObjectOfType<UI>(true);

        if (ui != null && ui.InteractionPromptRoot != null)
        {
            ui.InteractionPromptRoot.gameObject.SetActive(true);
            return ui.InteractionPromptRoot;
        }

        Canvas canvas = oracleUi != null ? oracleUi.GetComponentInParent<Canvas>(true) : null;
        return canvas != null ? canvas.transform : originalParent;
    }

    private void RestoreOriginalParent()
    {
        if (!hasOriginalLayout || oracleUi == null)
            return;

        if (originalParent != null && oracleUi.transform.parent != originalParent)
        {
            oracleUi.transform.SetParent(originalParent, false);
            RestoreRectSnapshot();
        }

        if (originalParent != null && originalSiblingIndex >= 0 && originalSiblingIndex < originalParent.childCount)
            oracleUi.transform.SetSiblingIndex(originalSiblingIndex);

        oracleUi.SetActive(false);
    }

    private void RestoreRectSnapshot()
    {
        if (!hasOriginalLayout)
            return;

        originalRectSnapshot.Apply(oracleRect, oracleUi.transform);
    }

    private static GameObject FindSceneGameObjectByName(Scene scene, string objectName)
    {
        if (!scene.IsValid() || string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];

            if (target == null || target.gameObject.scene != scene)
                continue;

            if (string.Equals(target.name, objectName, System.StringComparison.Ordinal))
                return target.gameObject;
        }

        return null;
    }

    private struct RectTransformSnapshot
    {
        private readonly bool hasRect;
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 sizeDelta;
        private readonly Vector2 pivot;
        private readonly Vector3 localPosition;
        private readonly Quaternion localRotation;
        private readonly Vector3 localScale;

        private RectTransformSnapshot(
            bool hasRect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2 pivot,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            this.hasRect = hasRect;
            this.anchorMin = anchorMin;
            this.anchorMax = anchorMax;
            this.anchoredPosition = anchoredPosition;
            this.sizeDelta = sizeDelta;
            this.pivot = pivot;
            this.localPosition = localPosition;
            this.localRotation = localRotation;
            this.localScale = localScale;
        }

        public static RectTransformSnapshot Capture(RectTransform rect, Transform transform)
        {
            if (rect != null)
            {
                return new RectTransformSnapshot(
                    true,
                    rect.anchorMin,
                    rect.anchorMax,
                    rect.anchoredPosition,
                    rect.sizeDelta,
                    rect.pivot,
                    rect.localPosition,
                    rect.localRotation,
                    rect.localScale);
            }

            return new RectTransformSnapshot(
                false,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                transform.localPosition,
                transform.localRotation,
                transform.localScale);
        }

        public void Apply(RectTransform rect, Transform transform)
        {
            if (hasRect && rect != null)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
                rect.pivot = pivot;
                rect.localRotation = localRotation;
                rect.localScale = localScale;
                return;
            }

            transform.localPosition = localPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;
        }
    }
}
