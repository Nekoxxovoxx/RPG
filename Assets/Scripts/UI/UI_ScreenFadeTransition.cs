using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class UI_ScreenFadeTransition : MonoBehaviour
{
    private static UI_ScreenFadeTransition instance;

    [SerializeField] private Canvas canvas;
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0f)] private float defaultFadeDuration = 0.75f;
    [SerializeField] private int sortingOrder = 32767;

    private Coroutine fadeRoutine;
    private Coroutine sceneLoadRoutine;

    public static UI_ScreenFadeTransition Instance => GetOrCreate();
    public bool IsFading { get; private set; }
    public float CurrentAlpha => fadeImage != null ? fadeImage.color.a : 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GetOrCreate();
    }

    public static UI_ScreenFadeTransition GetOrCreate()
    {
        if (instance != null)
            return instance;

        UI_ScreenFadeTransition existing = FindObjectOfType<UI_ScreenFadeTransition>();
        if (existing != null)
        {
            instance = existing;
            instance.Initialize();
            DontDestroyOnLoad(instance.gameObject);
            return instance;
        }

        GameObject root = new GameObject("Screen Fade Transition");
        instance = root.AddComponent<UI_ScreenFadeTransition>();
        instance.Initialize();
        DontDestroyOnLoad(root);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Initialize();
        DontDestroyOnLoad(gameObject);
    }

    public void LoadSceneWithFade(
        string sceneName,
        float fadeToBlackDuration,
        float fadeFromBlackDuration,
        bool fadeInAfterLoad = true)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (sceneLoadRoutine != null)
            StopCoroutine(sceneLoadRoutine);

        sceneLoadRoutine = StartCoroutine(LoadSceneRoutine(
            sceneName,
            fadeToBlackDuration,
            fadeFromBlackDuration,
            fadeInAfterLoad));
    }

    public Coroutine FadeToBlack(float duration = -1f)
    {
        return StartFade(1f, duration);
    }

    public Coroutine FadeFromBlack(float duration = -1f)
    {
        return StartFade(0f, duration);
    }

    public Coroutine StartFade(float targetAlpha, float duration = -1f)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
        return fadeRoutine;
    }

    public IEnumerator FadeTo(float targetAlpha, float duration = -1f)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        yield return FadeRoutine(targetAlpha, duration);
    }

    public void SetAlpha(float alpha)
    {
        Initialize();

        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
        fadeImage.raycastTarget = color.a > 0.01f;
    }

    private IEnumerator LoadSceneRoutine(
        string sceneName,
        float fadeToBlackDuration,
        float fadeFromBlackDuration,
        bool fadeInAfterLoad)
    {
        yield return FadeTo(1f, fadeToBlackDuration);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (operation != null && !operation.isDone)
            yield return null;

        yield return null;

        if (fadeInAfterLoad)
            yield return FadeTo(0f, fadeFromBlackDuration);

        sceneLoadRoutine = null;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        Initialize();

        IsFading = true;
        float resolvedDuration = duration >= 0f ? duration : defaultFadeDuration;
        float startAlpha = fadeImage.color.a;
        targetAlpha = Mathf.Clamp01(targetAlpha);

        if (resolvedDuration <= 0f)
        {
            SetAlpha(targetAlpha);
            IsFading = false;
            fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < resolvedDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / resolvedDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
        IsFading = false;
        fadeRoutine = null;
    }

    private void Initialize()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        if (GetComponent<CanvasScaler>() == null)
            gameObject.AddComponent<CanvasScaler>();

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        bool createdFadeImage = false;

        if (fadeImage == null)
        {
            Transform existing = transform.Find("Black Overlay");

            if (existing != null)
                fadeImage = existing.GetComponent<Image>();

            if (fadeImage == null)
            {
                GameObject overlay = new GameObject("Black Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                overlay.transform.SetParent(transform, false);
                fadeImage = overlay.GetComponent<Image>();
                createdFadeImage = true;
            }
        }

        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        float alpha = createdFadeImage ? 0f : fadeImage.color.a;
        fadeImage.color = new Color(0f, 0f, 0f, alpha);
        fadeImage.raycastTarget = fadeImage.color.a > 0.01f;
    }
}
