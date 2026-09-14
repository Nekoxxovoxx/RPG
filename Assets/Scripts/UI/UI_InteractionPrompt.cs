using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InteractionPrompt : MonoBehaviour
{
    private static UI_InteractionPrompt instance;

    [Header("显示")]
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.12f, 0f);
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 18f);
    [SerializeField] private Vector2 screenPadding = new Vector2(24f, 24f);

    [Header("样式")]
    [SerializeField] private TMP_FontAsset promptFont;
    [SerializeField] private string promptFontResourcePath = "Fonts & Materials/xiangsuziti SDF";
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.02f, 0.02f, 0.92f);
    [SerializeField] private Color frameColor = new Color(0.78f, 0.16f, 0.04f, 0.95f);
    [SerializeField] private Color keyBackgroundColor = new Color(0.28f, 0.04f, 0.035f, 1f);
    [SerializeField] private Color textColor = Color.white;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private IInteractable targetInteractable;
    private KeyCode currentKey = KeyCode.E;

    public static UI_InteractionPrompt Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        EnsureVisuals();
        Hide();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void LateUpdate()
    {
        if (targetInteractable == null)
            return;

        UpdatePromptPosition();
    }

    public static UI_InteractionPrompt GetOrCreate(Transform preferredParent = null)
    {
        if (instance != null)
            return instance;

        UI_InteractionPrompt existingPrompt = FindObjectOfType<UI_InteractionPrompt>(true);

        if (existingPrompt != null)
        {
            instance = existingPrompt;
            return instance;
        }

        Transform parent = preferredParent != null ? preferredParent : FindDefaultParent();
        GameObject promptObject = new GameObject("InteractionPrompt_UI", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));

        if (parent != null)
            promptObject.transform.SetParent(parent, false);

        UI_InteractionPrompt prompt = promptObject.AddComponent<UI_InteractionPrompt>();
        prompt.BuildRuntimeStyle();
        return prompt;
    }

    public void Show(IInteractable interactable, KeyCode key)
    {
        if (this == null)
            return;

        if (interactable == null)
        {
            Hide();
            return;
        }

        targetInteractable = interactable;
        currentKey = key;
        gameObject.SetActive(true);
        EnsureVisuals();
        RefreshText();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (rectTransform != null && gameObject.activeInHierarchy)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        UpdatePromptPosition();
    }

    public void Hide()
    {
        if (this == null)
            return;

        targetInteractable = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private static Transform FindDefaultParent()
    {
        UI ui = FindObjectOfType<UI>(true);

        if (ui != null && ui.InteractionPromptRoot != null)
            return ui.InteractionPromptRoot;

        Canvas canvas = FindObjectOfType<Canvas>(true);

        if (canvas != null)
            return canvas.transform;

        GameObject canvasObject = new GameObject("InteractionPrompt_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas newCanvas = canvasObject.GetComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject.transform;
    }

    private void EnsureVisuals()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (keyText != null && promptText != null)
            return;

        BuildRuntimeStyle();
    }

    private void BuildRuntimeStyle()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(190f, 46f);
        }

        Image background = GetComponent<Image>();

        if (background == null)
            background = gameObject.AddComponent<Image>();

        background.color = backgroundColor;
        background.raycastTarget = false;

        Outline outline = GetComponent<Outline>();

        if (outline == null)
            outline = gameObject.AddComponent<Outline>();

        outline.effectColor = frameColor;
        outline.effectDistance = new Vector2(2f, -2f);

        HorizontalLayoutGroup layoutGroup = GetComponent<HorizontalLayoutGroup>();

        if (layoutGroup == null)
            layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

        layoutGroup.padding = new RectOffset(8, 12, 6, 6);
        layoutGroup.spacing = 8f;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = GetComponent<ContentSizeFitter>();

        if (sizeFitter == null)
            sizeFitter = gameObject.AddComponent<ContentSizeFitter>();

        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        keyText = CreateKeyText();
        promptText = CreatePromptText();
        ApplyFont();
    }

    private TextMeshProUGUI CreateKeyText()
    {
        Transform existing = transform.Find("Key");

        if (existing != null)
        {
            TextMeshProUGUI existingText = existing.GetComponentInChildren<TextMeshProUGUI>(true);

            if (existingText != null)
                return existingText;
        }

        GameObject keyObject = new GameObject("Key", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        keyObject.transform.SetParent(transform, false);

        Image keyBackground = keyObject.GetComponent<Image>();
        keyBackground.color = keyBackgroundColor;
        keyBackground.raycastTarget = false;

        LayoutElement layoutElement = keyObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 34f;
        layoutElement.preferredHeight = 30f;
        layoutElement.minWidth = 34f;
        layoutElement.minHeight = 30f;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(keyObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.font = ResolveFont();
        text.alignment = TextAlignmentOptions.Center;
        text.color = frameColor;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        text.enableWordWrapping = false;

        return text;
    }

    private TextMeshProUGUI CreatePromptText()
    {
        Transform existing = transform.Find("Text");

        if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingText))
            return existingText;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(transform, false);

        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
        layoutElement.minWidth = 84f;
        layoutElement.preferredHeight = 30f;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = ResolveFont();
        text.alignment = TextAlignmentOptions.Left;
        text.color = textColor;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        text.enableWordWrapping = false;

        return text;
    }

    private TMP_FontAsset ResolveFont()
    {
        if (promptFont != null)
            return promptFont;

        if (!string.IsNullOrWhiteSpace(promptFontResourcePath))
            promptFont = Resources.Load<TMP_FontAsset>(promptFontResourcePath);

        return promptFont;
    }

    private void ApplyFont()
    {
        TMP_FontAsset font = ResolveFont();

        if (font == null)
            return;

        if (keyText != null)
            keyText.font = font;

        if (promptText != null)
            promptText.font = font;
    }

    private void RefreshText()
    {
        if (keyText != null)
            keyText.text = currentKey.ToString();

        if (promptText != null)
            promptText.text = GetActionText(targetInteractable != null ? targetInteractable.InteractionPrompt : string.Empty);
    }

    private string GetActionText(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return "交互";

        string keyName = currentKey.ToString();
        string action = prompt.Replace("按", string.Empty).Replace(keyName, string.Empty).Trim();

        return string.IsNullOrWhiteSpace(action) ? "交互" : action;
    }

    private void UpdatePromptPosition()
    {
        if (targetInteractable == null || rectTransform == null)
            return;

        Transform targetTransform = targetInteractable.InteractionTransform;

        if (targetTransform == null)
        {
            Hide();
            return;
        }

        Vector3 screenPosition = GetTargetScreenPosition(targetTransform);
        Vector2 targetPosition = new Vector2(screenPosition.x + screenOffset.x, screenPosition.y + screenOffset.y);

        rectTransform.position = ClampToScreen(targetPosition);
    }

    private Vector3 GetTargetScreenPosition(Transform targetTransform)
    {
        Vector3 worldPosition = targetTransform.position + worldOffset;
        Renderer renderer = targetTransform.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            worldPosition = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + worldOffset;
        }

        return RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
    }

    private Vector2 ClampToScreen(Vector2 position)
    {
        if (rectTransform == null)
            return position;

        Vector2 size = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        float minX = screenPadding.x + size.x * pivot.x;
        float maxX = Screen.width - screenPadding.x - size.x * (1f - pivot.x);
        float minY = screenPadding.y + size.y * pivot.y;
        float maxY = Screen.height - screenPadding.y - size.y * (1f - pivot.y);

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}
