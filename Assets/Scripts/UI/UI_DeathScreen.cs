using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DeathScreen : MonoBehaviour
{
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup returnFireGroup;
    [SerializeField] private Button returnFireButton;
    [SerializeField, Range(0f, 1f)] private float maxBlackAlpha = 1f;
    [SerializeField, Min(0.01f)] private float fadeToBlackDuration = 2.2f;
    [SerializeField, Min(0f)] private float contentDelay = 0.3f;
    [SerializeField, Min(0.01f)] private float contentFadeDuration = 0.65f;
    [SerializeField, Min(0f)] private float clickFeedbackDelay = 0.12f;

    private CanvasGroup rootGroup;
    private Coroutine showRoutine;
    private bool initialized;
    private bool showing;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        ResolveReferences();
        BindReturnFireButton();
        ApplyReturnFireButtonFeedback();
        HideImmediate();
    }

    public void Show()
    {
        Initialize();

        if (showing)
            return;

        showing = true;
        gameObject.SetActive(true);

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowRoutine());
    }

    public void HideImmediate()
    {
        ResolveReferences();

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        showing = false;
        SetOverlayAlpha(0f);
        SetGroupAlpha(titleGroup, 0f);
        SetGroupAlpha(returnFireGroup, 0f);
        SetInteractable(false);
        gameObject.SetActive(false);
    }

    private IEnumerator ShowRoutine()
    {
        SetInteractable(false);
        SetOverlayAlpha(0f);
        SetGroupAlpha(titleGroup, 0f);
        SetGroupAlpha(returnFireGroup, 0f);

        float timer = 0f;

        while (timer < fadeToBlackDuration)
        {
            timer += Time.unscaledDeltaTime;
            SetOverlayAlpha(Mathf.Lerp(0f, maxBlackAlpha, Mathf.Clamp01(timer / fadeToBlackDuration)));
            yield return null;
        }

        SetOverlayAlpha(maxBlackAlpha);

        if (contentDelay > 0f)
            yield return new WaitForSecondsRealtime(contentDelay);

        timer = 0f;

        while (timer < contentFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(timer / contentFadeDuration);
            SetGroupAlpha(titleGroup, alpha);
            SetGroupAlpha(returnFireGroup, alpha);
            yield return null;
        }

        SetGroupAlpha(titleGroup, 1f);
        SetGroupAlpha(returnFireGroup, 1f);
        SetInteractable(true);
        showRoutine = null;
    }

    private void HandleReturnFireClicked()
    {
        if (!showing)
            return;

        SetInteractable(false);
        StartCoroutine(ReturnFireRoutine());
    }

    private IEnumerator ReturnFireRoutine()
    {
        if (clickFeedbackDelay > 0f)
            yield return new WaitForSecondsRealtime(clickFeedbackDelay);

        Player player = PlayerManager.instance != null ? PlayerManager.instance.player : FindObjectOfType<Player>();
        bool returned = WorldRestManager.GetOrCreate().ReturnPlayerToLastRest(player);
        HideImmediate();
        Time.timeScale = 1f;

        if (returned && player != null)
            player.PlayRespawnAwaking();
    }

    private void ResolveReferences()
    {
        if (rootGroup == null)
        {
            rootGroup = GetComponent<CanvasGroup>();

            if (rootGroup == null)
                rootGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (fadeOverlay == null)
            fadeOverlay = FindLargestImageOutsideButton();

        if (fadeOverlay != null)
            fadeOverlay.raycastTarget = false;

        if (returnFireButton == null)
            returnFireButton = FindReturnFireButton();

        if (returnFireGroup == null && returnFireButton != null)
            returnFireGroup = GetOrAddCanvasGroup(returnFireButton.gameObject);

        if (titleGroup == null)
            titleGroup = FindTitleGroup();
    }

    private Image FindLargestImageOutsideButton()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        Image best = null;
        float bestArea = -1f;

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (image == null || image.GetComponentInParent<Button>(true) != null)
                continue;

            RectTransform rectTransform = image.rectTransform;
            float area = Mathf.Abs(rectTransform.rect.width * rectTransform.rect.height);

            if (area > bestArea)
            {
                best = image;
                bestArea = area;
            }
        }

        return best;
    }

    private Button FindReturnFireButton()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            if (button.gameObject.name.Contains("\u5F52\u706B"))
                return button;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

            if (text != null && text.text.Contains("\u5F52\u706B"))
                return button;
        }

        return buttons.Length > 0 ? buttons[0] : null;
    }

    private CanvasGroup FindTitleGroup()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
                continue;

            if (text.gameObject.name.Contains("\u6B8B\u706B\u5C06\u7184") || text.text.Contains("\u6B8B\u706B\u5C06\u7184"))
                return GetOrAddCanvasGroup(text.transform.parent != null ? text.transform.parent.gameObject : text.gameObject);
        }

        return null;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();

        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        return group;
    }

    private void BindReturnFireButton()
    {
        if (returnFireButton == null)
            return;

        returnFireButton.onClick.RemoveListener(HandleReturnFireClicked);
        returnFireButton.onClick.AddListener(HandleReturnFireClicked);
    }

    private void ApplyReturnFireButtonFeedback()
    {
        if (returnFireButton == null)
            return;

        Graphic targetGraphic = returnFireButton.targetGraphic;

        if (targetGraphic == null)
        {
            targetGraphic = returnFireButton.GetComponent<Graphic>();

            if (targetGraphic != null)
                returnFireButton.targetGraphic = targetGraphic;
        }

        returnFireButton.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = returnFireButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5f);
        colors.fadeDuration = 0.1f;
        returnFireButton.colors = colors;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (fadeOverlay == null)
            return;

        Color color = fadeOverlay.color;
        color.a = alpha;
        fadeOverlay.color = color;
    }

    private void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group == null)
            return;

        group.alpha = alpha;
    }

    private void SetInteractable(bool interactable)
    {
        if (rootGroup != null)
        {
            rootGroup.interactable = interactable;
            rootGroup.blocksRaycasts = interactable;
        }

        if (returnFireButton != null)
            returnFireButton.interactable = interactable;
    }
}
