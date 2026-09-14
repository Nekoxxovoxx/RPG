using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[AddComponentMenu("Dialogue/Ending Story Sequence Controller")]
public class EndingStorySequenceController : MonoBehaviour
{
    [System.Serializable]
    public class EndingPage
    {
        public GameObject backgroundRoot;
        public GameObject dialogueBoxRoot;
        public TextMeshProUGUI dialogueText;
        public TextMeshProUGUI nameText;
        public DialogueLine[] lines;
    }

    [Header("Pages")]
    [SerializeField] private EndingPage[] pages;
    [SerializeField] private bool autoCollectPagesWhenEmpty = true;
    [SerializeField] private string pageNamePrefix = "end";

    [Header("Speaker Names")]
    [SerializeField] private string npcName = "\u4F8D\u706B\u5DEB\u5973";
    [SerializeField] private string playerSpeakerName = "\u4F60";

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool hideAllWhenFinished;

    [Header("Finish")]
    [SerializeField] private bool loadNextSceneWhenFinished;
    [SerializeField] private string nextSceneName = "Main menu";
    [SerializeField, Min(0f)] private float finishFadeToBlackDuration = 0.8f;
    [SerializeField, Min(0f)] private float finishFadeFromBlackDuration = 0.8f;

    [Header("Text Reveal")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField, Min(1f)] private float charactersPerSecond = 28f;
    [SerializeField, Min(0f)] private float inputDelay = 0.12f;

    [Header("Screen Fade")]
    [SerializeField] private bool useScreenFade = true;
    [SerializeField, Min(0f)] private float fadeToBlackDuration = 0.75f;
    [SerializeField, Min(0f)] private float fadeFromBlackDuration = 0.85f;
    [SerializeField, Min(0f)] private float textBoxDelayAfterFade = 0.35f;

    private int currentPageIndex;
    private int currentLineIndex;
    private bool isPlaying;
    private bool isTyping;
    private bool isTransitioning;
    private float inputReadyAt;
    private string currentFullText;
    private Coroutine typewriterRoutine;
    private Coroutine pageTransitionRoutine;

    private void Reset()
    {
        CollectPagesFromSiblingsIfNeeded();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            CollectPagesFromSiblingsIfNeeded();
    }

    private void Awake()
    {
        CollectPagesFromSiblingsIfNeeded();
        HideAllPages();
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    private void Update()
    {
        if (!isPlaying || isTransitioning || Time.unscaledTime < inputReadyAt)
            return;

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            AdvanceOrCompleteLine();
        }
    }

    [ContextMenu("Auto Collect Pages From Siblings")]
    public void CollectPagesFromSiblings()
    {
        Transform root = transform.parent != null ? transform.parent : transform;
        List<Transform> pageTransforms = new List<Transform>();

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child == null || !IsStoryPageCandidate(child))
                continue;

            pageTransforms.Add(child);
        }

        pageTransforms.Sort((left, right) => ExtractPageIndex(left.name).CompareTo(ExtractPageIndex(right.name)));

        GameObject sharedDialogueBox = null;
        TextMeshProUGUI sharedDialogueText = null;
        TextMeshProUGUI sharedNameText = null;
        ResolveDialogueReferences(root, ref sharedDialogueBox, ref sharedDialogueText, ref sharedNameText);

        List<EndingPage> collectedPages = new List<EndingPage>();

        for (int i = 0; i < pageTransforms.Count; i++)
        {
            Transform pageTransform = pageTransforms[i];
            collectedPages.Add(new EndingPage
            {
                backgroundRoot = pageTransform.gameObject,
                dialogueBoxRoot = sharedDialogueBox,
                dialogueText = sharedDialogueText,
                nameText = sharedNameText,
                lines = CreateDefaultLines(sharedDialogueText, i)
            });
        }

        pages = collectedPages.ToArray();
    }

    public void Play()
    {
        CollectPagesFromSiblingsIfNeeded();

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
        isPlaying = false;
        isTransitioning = true;

        if (!loadNextSceneWhenFinished || string.IsNullOrWhiteSpace(nextSceneName))
        {
            Stop();
            return;
        }

        if (useScreenFade)
        {
            UI_ScreenFadeTransition.Instance.LoadSceneWithFade(
                nextSceneName,
                finishFadeToBlackDuration,
                finishFadeFromBlackDuration);
            return;
        }

        SceneManager.LoadScene(nextSceneName);
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

        UI_ScreenFadeTransition fadeTransition = useScreenFade ? UI_ScreenFadeTransition.Instance : null;

        if (fadeTransition != null && fadeToBlackFirst)
            yield return fadeTransition.FadeTo(1f, fadeToBlackDuration);
        else if (fadeTransition != null && fadeTransition.CurrentAlpha < 0.99f)
            fadeTransition.SetAlpha(1f);

        ShowCurrentPageBackgroundOnly();

        if (fadeTransition != null)
            yield return fadeTransition.FadeTo(0f, fadeFromBlackDuration);

        if (CurrentPageHasDialogue())
        {
            if (textBoxDelayAfterFade > 0f)
                yield return new WaitForSecondsRealtime(textBoxDelayAfterFade);

            ShowCurrentDialogueBox();
            RefreshCurrentLine();
        }
        else
        {
            HideCurrentDialogueBox();
        }

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

            SetBackgroundVisible(pages[i], i == currentPageIndex);

            if (i != currentPageIndex && pages[i].dialogueBoxRoot != null)
                pages[i].dialogueBoxRoot.SetActive(false);
        }
    }

    private void ShowCurrentDialogueBox()
    {
        EndingPage currentPage = GetCurrentPage();

        if (currentPage == null)
            return;

        ResolvePageReferences(currentPage);

        if (currentPage.dialogueBoxRoot != null)
            currentPage.dialogueBoxRoot.SetActive(true);
    }

    private void HideCurrentDialogueBox()
    {
        EndingPage currentPage = GetCurrentPage();

        if (currentPage == null)
            return;

        ResolvePageReferences(currentPage);

        if (currentPage.dialogueBoxRoot != null)
            currentPage.dialogueBoxRoot.SetActive(false);

        if (currentPage.dialogueText != null)
            currentPage.dialogueText.text = string.Empty;

        if (currentPage.nameText != null)
            currentPage.nameText.text = string.Empty;
    }

    private void RefreshCurrentLine()
    {
        EndingPage currentPage = GetCurrentPage();

        if (currentPage == null)
            return;

        ResolvePageReferences(currentPage);

        DialogueLine line = GetCurrentLine();
        string text = line != null ? line.text : string.Empty;
        currentFullText = string.IsNullOrWhiteSpace(text) ? "..." : text.Trim();

        if (currentPage.nameText != null)
            currentPage.nameText.text = GetSpeakerName(line);

        if (currentPage.dialogueText != null)
            StartTypewriter(currentPage.dialogueText, currentFullText);
    }

    private void StartTypewriter(TextMeshProUGUI targetText, string text)
    {
        StopTypewriter();

        if (targetText == null)
            return;

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

        EndingPage currentPage = GetCurrentPage();

        if (currentPage != null && currentPage.dialogueText != null)
            currentPage.dialogueText.text = string.IsNullOrWhiteSpace(currentFullText) ? "..." : currentFullText;
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

            SetBackgroundVisible(pages[i], false);

            if (pages[i].dialogueBoxRoot != null)
                pages[i].dialogueBoxRoot.SetActive(false);
        }
    }

    private void SetBackgroundVisible(EndingPage page, bool visible)
    {
        if (page == null || page.backgroundRoot == null)
            return;

        bool containsDialogueBox = page.dialogueBoxRoot != null &&
                                   IsAncestorOrSelf(page.backgroundRoot.transform, page.dialogueBoxRoot.transform);

        if (!containsDialogueBox)
        {
            page.backgroundRoot.SetActive(visible);
            return;
        }

        page.backgroundRoot.SetActive(true);
        Graphic graphic = page.backgroundRoot.GetComponent<Graphic>();

        if (graphic != null)
            graphic.enabled = visible;
    }

    private void ResolvePageReferences(EndingPage page)
    {
        if (page == null)
            return;

        if (page.dialogueBoxRoot == null)
            page.dialogueBoxRoot = FindChildByName(transform.root, "\u5BF9\u8BDD\u6846");

        if (page.dialogueText == null && page.dialogueBoxRoot != null)
            page.dialogueText = FindTextByName(page.dialogueBoxRoot.transform, "\u5BF9\u8BDD");

        if (page.nameText == null && page.dialogueBoxRoot != null)
            page.nameText = FindTextByName(page.dialogueBoxRoot.transform, "\u540D\u79F0");
    }

    private EndingPage GetCurrentPage()
    {
        if (pages == null || currentPageIndex < 0 || currentPageIndex >= pages.Length)
            return null;

        return pages[currentPageIndex];
    }

    private int GetCurrentPageLineCount()
    {
        EndingPage currentPage = GetCurrentPage();
        return currentPage != null && currentPage.lines != null ? currentPage.lines.Length : 0;
    }

    private bool CurrentPageHasDialogue()
    {
        return GetCurrentPageLineCount() > 0;
    }

    private DialogueLine GetCurrentLine()
    {
        EndingPage currentPage = GetCurrentPage();

        if (currentPage == null ||
            currentPage.lines == null ||
            currentLineIndex < 0 ||
            currentLineIndex >= currentPage.lines.Length)
        {
            return null;
        }

        return currentPage.lines[currentLineIndex];
    }

    private string GetSpeakerName(DialogueLine line)
    {
        if (line == null)
            return npcName;

        return line.speaker == DialogueSpeaker.Player ? playerSpeakerName : npcName;
    }

    private void CollectPagesFromSiblingsIfNeeded()
    {
        if (!autoCollectPagesWhenEmpty || (pages != null && pages.Length > 0))
            return;

        CollectPagesFromSiblings();
    }

    private bool IsStoryPageCandidate(Transform candidate)
    {
        if (candidate == null || string.IsNullOrWhiteSpace(pageNamePrefix))
            return false;

        return candidate.name.StartsWith(pageNamePrefix, System.StringComparison.OrdinalIgnoreCase) &&
               candidate.GetComponent<Graphic>() != null;
    }

    private int ExtractPageIndex(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return int.MaxValue;

        int value = 0;
        bool hasDigit = false;

        for (int i = 0; i < objectName.Length; i++)
        {
            if (!char.IsDigit(objectName[i]))
                continue;

            hasDigit = true;
            value = value * 10 + objectName[i] - '0';
        }

        return hasDigit ? value : int.MaxValue;
    }

    private void ResolveDialogueReferences(
        Transform root,
        ref GameObject dialogueBoxRoot,
        ref TextMeshProUGUI dialogueText,
        ref TextMeshProUGUI nameText)
    {
        if (dialogueBoxRoot == null)
            dialogueBoxRoot = FindChildByName(root, "\u5BF9\u8BDD\u6846");

        if (dialogueBoxRoot == null)
            return;

        if (dialogueText == null)
            dialogueText = FindTextByName(dialogueBoxRoot.transform, "\u5BF9\u8BDD");

        if (nameText == null)
            nameText = FindTextByName(dialogueBoxRoot.transform, "\u540D\u79F0");
    }

    private DialogueLine[] CreateDefaultLines(TextMeshProUGUI sharedDialogueText, int pageIndex)
    {
        if (pageIndex > 0 || sharedDialogueText == null || string.IsNullOrWhiteSpace(sharedDialogueText.text))
            return new DialogueLine[0];

        return new[] { new DialogueLine(DialogueSpeaker.Npc, sharedDialogueText.text) };
    }

    private GameObject FindChildByName(Transform root, string childName)
    {
        Transform found = FindTransformByName(root, childName);
        return found != null ? found.gameObject : null;
    }

    private TextMeshProUGUI FindTextByName(Transform root, string childName)
    {
        Transform found = FindTransformByName(root, childName);
        return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
    }

    private Transform FindTransformByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransformByName(root.GetChild(i), childName);

            if (found != null)
                return found;
        }

        return null;
    }

    private bool IsAncestorOrSelf(Transform possibleAncestor, Transform child)
    {
        Transform current = child;

        while (current != null)
        {
            if (current == possibleAncestor)
                return true;

            current = current.parent;
        }

        return false;
    }
}
