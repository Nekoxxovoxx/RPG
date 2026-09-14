using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-999)]
public class UI_EyeBlinkTransition : MonoBehaviour
{
    private static UI_EyeBlinkTransition instance;

    [SerializeField] private Canvas canvas;
    [SerializeField] private Image topLid;
    [SerializeField] private Image bottomLid;
    [SerializeField] private int sortingOrder = 32768;
    [SerializeField] private Color lidColor = Color.black;

    public static UI_EyeBlinkTransition Instance => GetOrCreate();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GetOrCreate();
    }

    public static UI_EyeBlinkTransition GetOrCreate()
    {
        if (instance != null)
            return instance;

        UI_EyeBlinkTransition existing = FindObjectOfType<UI_EyeBlinkTransition>();
        if (existing != null)
        {
            instance = existing;
            instance.Initialize();
            DontDestroyOnLoad(instance.gameObject);
            return instance;
        }

        GameObject root = new GameObject("Eye Blink Transition");
        instance = root.AddComponent<UI_EyeBlinkTransition>();
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

    public IEnumerator PlayOpeningBlink(float firstOpenDuration, float closeDuration, float finalOpenDuration)
    {
        Initialize();
        gameObject.SetActive(true);

        SetLidColor(lidColor);
        SetClosedAmount(1f);

        yield return AnimateClosedAmount(1f, 0.25f, firstOpenDuration);
        yield return AnimateClosedAmount(0.25f, 0.68f, closeDuration);
        yield return AnimateClosedAmount(0.68f, 0f, finalOpenDuration);

        SetClosedAmount(0f);
        gameObject.SetActive(false);
    }

    public void SetClosed()
    {
        Initialize();
        gameObject.SetActive(true);
        SetClosedAmount(1f);
    }

    public void SetOpenAndHide()
    {
        Initialize();
        SetClosedAmount(0f);
        gameObject.SetActive(false);
    }

    private IEnumerator AnimateClosedAmount(float start, float end, float duration)
    {
        if (duration <= 0f)
        {
            SetClosedAmount(end);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            SetClosedAmount(Mathf.Lerp(start, end, t));
            yield return null;
        }

        SetClosedAmount(end);
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

        topLid = ResolveLid(topLid, "Top Eyelid", true);
        bottomLid = ResolveLid(bottomLid, "Bottom Eyelid", false);
        SetLidColor(lidColor);
        SetClosedAmount(0f);
    }

    private Image ResolveLid(Image lid, string objectName, bool isTop)
    {
        if (lid == null)
        {
            Transform existing = transform.Find(objectName);

            if (existing != null)
                lid = existing.GetComponent<Image>();
        }

        if (lid == null)
        {
            GameObject lidObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lidObject.transform.SetParent(transform, false);
            lid = lidObject.GetComponent<Image>();
        }

        lid.raycastTarget = false;

        RectTransform rect = lid.GetComponent<RectTransform>();
        rect.anchorMin = isTop ? new Vector2(0f, 1f) : Vector2.zero;
        rect.anchorMax = isTop ? Vector2.one : new Vector2(1f, 0f);
        rect.pivot = isTop ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return lid;
    }

    private void SetClosedAmount(float amount)
    {
        amount = Mathf.Clamp01(amount);
        float lidHeight = Screen.height * 0.5f * amount;

        SetLidHeight(topLid, lidHeight);
        SetLidHeight(bottomLid, lidHeight);
    }

    private void SetLidHeight(Image lid, float height)
    {
        if (lid == null)
            return;

        RectTransform rect = lid.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, height);
    }

    private void SetLidColor(Color color)
    {
        if (topLid != null)
            topLid.color = color;

        if (bottomLid != null)
            bottomLid.color = color;
    }
}
