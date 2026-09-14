using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[AddComponentMenu("Dialogue/Opening Story Sequence Controller")]
public class OpeningStorySequenceController : MonoBehaviour
{
    [System.Serializable]
    public class StoryPage
    {
        public GameObject backgroundRoot;
        public GameObject dialogueBoxRoot;
        public TextMeshProUGUI monologueText;
        public OpeningMonologueController.MonologueLine[] lines;
    }

    [Header("Pages")]
    [SerializeField] private StoryPage[] pages;
    [SerializeField] private bool autoCollectPagesWhenEmpty = true;
    [SerializeField] private bool disableChildSinglePageControllers = true;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool hideAllWhenFinished = true;

    [Header("Finish")]
    [SerializeField] private bool loadNextSceneWhenFinished = true;
    [SerializeField] private string nextSceneName = "level1";
    [SerializeField, Min(0f)] private float finishFadeToBlackDuration = 0.8f;
    [SerializeField, Min(0f)] private float finishFadeFromBlackDuration = 0.8f;
    [SerializeField] private bool triggerPlayerAwakingOnNextScene = true;
    [SerializeField] private string playerAwakingSceneName = "level1";

    [Header("Skip")]
    [SerializeField] private Button skipButton;
    [SerializeField] private string skipButtonObjectName = "Skip";
    [SerializeField] private bool showSkipButtonWithDialogue = true;

    [Header("Text Reveal")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField, Min(1f)] private float charactersPerSecond = 28f;
    [SerializeField, Min(0f)] private float inputDelay = 0.12f;

    [Header("Screen Fade")]
    [SerializeField] private bool useScreenFade = true;
    [SerializeField, Min(0f)] private float fadeToBlackDuration = 0.75f;
    [SerializeField, Min(0f)] private float fadeFromBlackDuration = 0.85f;
    [SerializeField, Min(0f)] private float textBoxDelayAfterFade = 1f;

    private int currentPageIndex;
    private int currentLineIndex;
    private bool isPlaying;
    private bool isTyping;
    private bool isTransitioning;
    private bool isSkipping;
    private float inputReadyAt;
    private string currentFullText;
    private Coroutine typewriterRoutine;
    private Coroutine pageTransitionRoutine;

    private void Reset()
    {
        CollectPagesFromChildrenIfNeeded();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            CollectPagesFromChildrenIfNeeded();
    }

    private void Awake()
    {
        CollectPagesFromChildrenIfNeeded();

        if (disableChildSinglePageControllers)
            DisableChildSinglePageControllers();

        ResolveSkipButton();
        BindSkipButton();
        SetSkipButtonVisible(false);
        HideAllPages();
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    private void Update()
    {
        if (!isPlaying || isSkipping || isTransitioning || Time.unscaledTime < inputReadyAt)
            return;

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            AdvanceOrCompleteLine();
        }
    }

    [ContextMenu("Auto Collect Pages From Children")]
    public void CollectPagesFromChildren()
    {
        List<StoryPage> collectedPages = new List<StoryPage>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            OpeningMonologueController singlePageController = child.GetComponent<OpeningMonologueController>();
            TextMeshProUGUI monologueText = singlePageController != null
                ? singlePageController.MonologueText
                : child.GetComponentInChildren<TextMeshProUGUI>(true);

            if (monologueText == null && singlePageController == null)
                continue;

            GameObject dialogueBoxRoot = singlePageController != null && singlePageController.DialogueBoxRoot != null
                ? singlePageController.DialogueBoxRoot
                : ResolveDialogueBoxRoot(monologueText);

            collectedPages.Add(new StoryPage
            {
                backgroundRoot = child.gameObject,
                dialogueBoxRoot = dialogueBoxRoot,
                monologueText = monologueText,
                lines = singlePageController != null
                    ? singlePageController.GetConfiguredLinesCopy()
                    : CreateLinesFromText(monologueText)
            });
        }

        pages = collectedPages.ToArray();
    }

    public void Play()
    {
        CollectPagesFromChildrenIfNeeded();

        if (pages == null || pages.Length <= 0)
            return;

        currentPageIndex = 0;
        currentLineIndex = 0;
        isPlaying = true;
        PlayCurrentPageTransition(false);
    }

    public void Stop()
    {
        StopTypewriter();
        StopPageTransition();
        isPlaying = false;
        isTransitioning = false;
        isSkipping = false;
        SetSkipButtonVisible(false);

        if (hideAllWhenFinished)
            HideAllPages();
    }

    private void AdvanceOrCompleteLine()
    {
        if (isTyping)
        {
            CompleteCurrentLine();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex < GetCurrentPageLineCount())
        {
            inputReadyAt = Time.unscaledTime + inputDelay;
            RefreshCurrentLine();
            return;
        }

        currentPageIndex++;
        currentLineIndex = 0;

        if (currentPageIndex >= pages.Length)
        {
            FinishStory();
            return;
        }

        PlayCurrentPageTransition(true);
    }

    private void FinishStory()
    {
        StopTypewriter();
        StopPageTransition();
        SetSkipButtonVisible(false);
        isPlaying = false;
        isTransitioning = true;

        if (!loadNextSceneWhenFinished || string.IsNullOrWhiteSpace(nextSceneName))
        {
            Stop();
            return;
        }

        bool shouldTriggerPlayerAwaking = ShouldTriggerPlayerAwaking();

        if (shouldTriggerPlayerAwaking)
            PlayerOpeningAwakingRequest.Request(playerAwakingSceneName);

        if (useScreenFade)
        {
            UI_ScreenFadeTransition.Instance.LoadSceneWithFade(
                nextSceneName,
                finishFadeToBlackDuration,
                finishFadeFromBlackDuration,
                !shouldTriggerPlayerAwaking);

            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private bool ShouldTriggerPlayerAwaking()
    {
        return triggerPlayerAwakingOnNextScene &&
               !string.IsNullOrWhiteSpace(playerAwakingSceneName) &&
               string.Equals(nextSceneName, playerAwakingSceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void SkipStoryToNextScene()
    {
        if (isSkipping)
            return;

        isSkipping = true;
        FinishStory();
    }

    private void PlayCurrentPageTransition(bool fadeToBlackFirst)
    {
        StopPageTransition();
        pageTransitionRoutine = StartCoroutine(PageTransitionRoutine(fadeToBlackFirst));
    }

    private IEnumerator PageTransitionRoutine(bool fadeToBlackFirst)
    {
        isTransitioning = true;
        StopTypewriter();
        SetSkipButtonVisible(false);

        UI_ScreenFadeTransition fadeTransition = useScreenFade ? UI_ScreenFadeTransition.Instance : null;

        if (fadeTransition != null && fadeToBlackFirst)
            yield return fadeTransition.FadeTo(1f, fadeToBlackDuration);
        else if (fadeTransition != null && fadeTransition.CurrentAlpha < 0.99f)
            fadeTransition.SetAlpha(1f);

        ShowCurrentPageBackgroundOnly();

        if (fadeTransition != null)
            yield return fadeTransition.FadeTo(0f, fadeFromBlackDuration);

        if (textBoxDelayAfterFade > 0f)
            yield return new WaitForSecondsRealtime(textBoxDelayAfterFade);

        StoryPage currentPage = GetCurrentPage();

        if (currentPage != null && currentPage.dialogueBoxRoot != null)
            currentPage.dialogueBoxRoot.SetActive(true);

        SetSkipButtonVisible(showSkipButtonWithDialogue);
        RefreshCurrentLine();
        inputReadyAt = Time.unscaledTime + inputDelay;
        isTransitioning = false;
        pageTransitionRoutine = null;
    }

    private void ShowCurrentPageBackgroundOnly()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null || pages[i].backgroundRoot == null)
                continue;

            pages[i].backgroundRoot.SetActive(i == currentPageIndex);

            if (pages[i].dialogueBoxRoot != null)
                pages[i].dialogueBoxRoot.SetActive(false);
        }
    }

    private void ShowCurrentPage()
    {
        ShowCurrentPageBackgroundOnly();

        StoryPage currentPage = GetCurrentPage();

        if (currentPage == null)
            return;

        if (currentPage.dialogueBoxRoot != null)
            currentPage.dialogueBoxRoot.SetActive(true);

        SetSkipButtonVisible(showSkipButtonWithDialogue);
        RefreshCurrentLine();
    }

    private void RefreshCurrentLine()
    {
        StoryPage currentPage = GetCurrentPage();

        if (currentPage == null || currentPage.monologueText == null)
            return;

        string line = GetCurrentLineText();
        currentFullText = string.IsNullOrWhiteSpace(line) ? "..." : line.Trim();
        StartTypewriter(currentPage.monologueText, currentFullText);
    }

    private void StartTypewriter(TextMeshProUGUI targetText, string text)
    {
        StopTypewriter();

        if (!useTypewriter || charactersPerSecond <= 0f)
        {
            targetText.text = text;
            isTyping = false;
            return;
        }

        typewriterRoutine = StartCoroutine(TypewriterRoutine(targetText, text));
    }

    private IEnumerator TypewriterRoutine(TextMeshProUGUI targetText, string text)
    {
        isTyping = true;
        targetText.text = string.Empty;

        float visibleCharacters = 0f;

        while (visibleCharacters < text.Length)
        {
            visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
            int count = Mathf.Clamp(Mathf.FloorToInt(visibleCharacters), 0, text.Length);
            targetText.text = text.Substring(0, count);
            yield return null;
        }

        targetText.text = text;
        isTyping = false;
        typewriterRoutine = null;
    }

    private void CompleteCurrentLine()
    {
        StopTypewriter();

        StoryPage currentPage = GetCurrentPage();

        if (currentPage != null && currentPage.monologueText != null)
            currentPage.monologueText.text = string.IsNullOrWhiteSpace(currentFullText) ? "..." : currentFullText;
    }

    private void StopTypewriter()
    {
        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        typewriterRoutine = null;
        isTyping = false;
    }

    private void StopPageTransition()
    {
        if (pageTransitionRoutine != null)
            StopCoroutine(pageTransitionRoutine);

        pageTransitionRoutine = null;
    }

    private void HideAllPages()
    {
        if (pages == null)
            return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null)
                continue;

            if (pages[i].backgroundRoot != null)
                pages[i].backgroundRoot.SetActive(false);

            if (pages[i].dialogueBoxRoot != null)
                pages[i].dialogueBoxRoot.SetActive(false);
        }

        SetSkipButtonVisible(false);
    }

    private void ResolveSkipButton()
    {
        if (skipButton != null)
            return;

        Button[] buttons = FindObjectsOfType<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button != null && button.gameObject.name == skipButtonObjectName)
            {
                skipButton = button;
                return;
            }
        }
    }

    private void BindSkipButton()
    {
        if (skipButton == null)
            return;

        skipButton.onClick.RemoveListener(SkipStoryToNextScene);
        skipButton.onClick.AddListener(SkipStoryToNextScene);
        skipButton.interactable = true;
    }

    private void SetSkipButtonVisible(bool visible)
    {
        if (skipButton == null)
            return;

        skipButton.gameObject.SetActive(visible);
    }

    private StoryPage GetCurrentPage()
    {
        if (pages == null || currentPageIndex < 0 || currentPageIndex >= pages.Length)
            return null;

        return pages[currentPageIndex];
    }

    private int GetCurrentPageLineCount()
    {
        StoryPage currentPage = GetCurrentPage();
        return currentPage != null && currentPage.lines != null ? currentPage.lines.Length : 0;
    }

    private string GetCurrentLineText()
    {
        StoryPage currentPage = GetCurrentPage();

        if (currentPage == null ||
            currentPage.lines == null ||
            currentLineIndex < 0 ||
            currentLineIndex >= currentPage.lines.Length ||
            currentPage.lines[currentLineIndex] == null)
        {
            return string.Empty;
        }

        return currentPage.lines[currentLineIndex].text;
    }

    private void CollectPagesFromChildrenIfNeeded()
    {
        if (!autoCollectPagesWhenEmpty || (pages != null && pages.Length > 0))
            return;

        CollectPagesFromChildren();
    }

    private void DisableChildSinglePageControllers()
    {
        OpeningMonologueController[] singlePageControllers = GetComponentsInChildren<OpeningMonologueController>(true);

        for (int i = 0; i < singlePageControllers.Length; i++)
        {
            if (singlePageControllers[i] != null)
                singlePageControllers[i].enabled = false;
        }
    }

    private GameObject ResolveDialogueBoxRoot(TextMeshProUGUI text)
    {
        if (text == null)
            return null;

        return text.transform.parent != null ? text.transform.parent.gameObject : text.gameObject;
    }

    private OpeningMonologueController.MonologueLine[] CreateLinesFromText(TextMeshProUGUI text)
    {
        if (text == null || string.IsNullOrWhiteSpace(text.text))
            return new OpeningMonologueController.MonologueLine[0];

        return new[]
        {
            new OpeningMonologueController.MonologueLine { text = text.text }
        };
    }
}
