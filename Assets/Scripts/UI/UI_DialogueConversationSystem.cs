using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_DialogueConversationSystem : MonoBehaviour
{
    private class DialoguePanelBinding
    {
        public Transform Root;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI BodyText;
        public bool IsValid => Root != null && BodyText != null;
    }

    private static UI_DialogueConversationSystem instance;
    private static int lastClosedFrame = -1;

    private readonly List<DialogueLine> lines = new List<DialogueLine>();
    private readonly DialoguePanelBinding npcPanel = new DialoguePanelBinding();
    private readonly DialoguePanelBinding playerPanel = new DialoguePanelBinding();

    [Header("Text Reveal")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField, Min(1f)] private float charactersPerSecond = 28f;
    [SerializeField, Min(1f)] private float fastForwardCharactersPerSecond = 180f;
    [SerializeField, Min(0.01f)] private float fastForwardAutoAdvanceDelay = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioEventPlayer audioEvents;
    [SerializeField] private string openCueName = "DialogueOpen";
    [SerializeField] private string closeCueName = "DialogueClose";
    [SerializeField] private string advanceCueName = "DialogueAdvance";
    [SerializeField] private string typeCueName = "DialogueType";
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip advanceClip;
    [SerializeField] private AudioClip typeClip;
    [SerializeField, Min(1)] private int typeSoundEveryCharacters = 2;
    [SerializeField, Min(0f)] private float typeSoundCooldown = 0.035f;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.7f;

    private string currentNpcName;
    private string currentPlayerName;
    private int currentIndex;
    private bool conversationOpen;
    private float hideAt;
    private float inputReadyAt;
    private string currentFullText;
    private DialoguePanelBinding currentPanel;
    private Coroutine typewriterRoutine;
    private bool isTyping;
    private float nextFastForwardAdvanceAt;
    private float nextTypeSoundTime;

    public static UI_DialogueConversationSystem Instance => instance;
    public static bool IsOpen => instance != null && instance.conversationOpen;
    public static bool WasClosedThisFrame => lastClosedFrame == Time.frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GetOrCreate().HideAllDialoguePanels(false);
    }

    public static UI_DialogueConversationSystem GetOrCreate()
    {
        if (instance != null)
            return instance;

        GameObject systemObject = new GameObject("Dialogue Conversation System");
        instance = systemObject.AddComponent<UI_DialogueConversationSystem>();
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
        ResolvePanels();
        ResolveAudioEvents();
        HideAllDialoguePanels(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        StopTypewriter();
        audioEvents = null;
        npcPanel.Root = null;
        npcPanel.NameText = null;
        npcPanel.BodyText = null;
        playerPanel.Root = null;
        playerPanel.NameText = null;
        playerPanel.BodyText = null;
    }

    private void Update()
    {
        if (!conversationOpen)
            return;

        if (hideAt > 0f && Time.unscaledTime >= hideAt)
        {
            Hide();
            return;
        }

        if (Time.unscaledTime < inputReadyAt)
            return;

        if (IsFastForwardHeld())
        {
            if (!isTyping && Time.unscaledTime >= nextFastForwardAdvanceAt)
            {
                nextFastForwardAdvanceAt = Time.unscaledTime + fastForwardAutoAdvanceDelay;
                ShowNextLineOrHide();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            AdvanceOrCompleteLine();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void ShowNpcMessage(string npcName, string text, float displaySeconds = 0f)
    {
        if (string.IsNullOrWhiteSpace(text))
            text = "...";

        ShowConversation(npcName, string.Empty, new[]
        {
            new DialogueLine(DialogueSpeaker.Npc, text)
        }, displaySeconds);
    }

    public void ShowNpcSequence(string npcName, IReadOnlyList<string> npcLines, float displaySeconds = 0f)
    {
        List<DialogueLine> convertedLines = new List<DialogueLine>();

        if (npcLines != null)
        {
            for (int i = 0; i < npcLines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(npcLines[i]))
                    convertedLines.Add(new DialogueLine(DialogueSpeaker.Npc, npcLines[i].Trim()));
            }
        }

        ShowConversation(npcName, string.Empty, convertedLines, displaySeconds);
    }

    public void ShowConversation(string npcName, string playerName, IReadOnlyList<DialogueLine> conversationLines, float displaySeconds = 0f)
    {
        ResolvePanels();
        UI_NpcInteractionMenu.Instance?.Hide();
        UI_NpcPanelUtility.HideNamedOptionPanelOnly();

        lines.Clear();

        if (conversationLines != null)
        {
            for (int i = 0; i < conversationLines.Count; i++)
            {
                DialogueLine line = conversationLines[i];

                if (line != null && !string.IsNullOrWhiteSpace(line.text))
                    lines.Add(new DialogueLine(line.speaker, line.text.Trim()));
            }
        }

        if (lines.Count <= 0)
            lines.Add(new DialogueLine(DialogueSpeaker.Npc, "..."));

        currentNpcName = string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName;
        currentPlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;
        currentIndex = 0;
        conversationOpen = true;
        hideAt = displaySeconds > 0f && lines.Count == 1 ? Time.unscaledTime + displaySeconds : 0f;
        inputReadyAt = Time.unscaledTime + 0.12f;
        nextFastForwardAdvanceAt = 0f;

        EnsureInputSupport();
        PlayDialogueSound(openCueName, openClip);
        RefreshCurrentLine();
    }

    public void Hide()
    {
        HideAllDialoguePanels(true);
    }

    private void HideAllDialoguePanels(bool recordCloseFrame)
    {
        ResolvePanels();

        bool wasOpen = conversationOpen ||
                       (npcPanel.Root != null && npcPanel.Root.gameObject.activeSelf) ||
                       (playerPanel.Root != null && playerPanel.Root.gameObject.activeSelf);

        conversationOpen = false;
        hideAt = 0f;
        lines.Clear();
        StopTypewriter();

        SetPanelVisible(npcPanel, false);
        SetPanelVisible(playerPanel, false);

        if (recordCloseFrame && wasOpen)
        {
            lastClosedFrame = Time.frameCount;
            PlayDialogueSound(closeCueName, closeClip);
        }

        UI_NpcPanelUtility.HideNpcRootIfIdle();
    }

    private void AdvanceOrCompleteLine()
    {
        if (isTyping)
        {
            CompleteCurrentLine();
            return;
        }

        ShowNextLineOrHide();
    }

    private void ShowNextLineOrHide()
    {
        currentIndex++;
        PlayDialogueSound(advanceCueName, advanceClip);

        if (currentIndex >= lines.Count)
        {
            Hide();
            return;
        }

        RefreshCurrentLine();
    }

    private void RefreshCurrentLine()
    {
        if (lines.Count <= 0)
        {
            Hide();
            return;
        }

        ResolvePanels(true);

        DialogueLine line = lines[Mathf.Clamp(currentIndex, 0, lines.Count - 1)];
        bool isPlayer = line.speaker == DialogueSpeaker.Player;
        DialoguePanelBinding activePanel = isPlayer ? playerPanel : npcPanel;
        DialoguePanelBinding inactivePanel = isPlayer ? npcPanel : playerPanel;

        if (!activePanel.IsValid)
        {
            activePanel = npcPanel.IsValid ? npcPanel : playerPanel;
            inactivePanel = activePanel == npcPanel ? playerPanel : npcPanel;
        }

        if (!activePanel.IsValid)
        {
            Debug.LogWarning("Dialogue panel text binding is missing. Please check NPC_UI dialogue text objects.");
            Hide();
            return;
        }

        SetPanelVisible(inactivePanel, false);
        SetPanelVisible(activePanel, true);
        PrepareTextForDisplay(activePanel.BodyText);

        if (activePanel.NameText != null && activePanel.NameText != activePanel.BodyText)
        {
            PrepareTextForDisplay(activePanel.NameText);
            activePanel.NameText.text = isPlayer ? currentPlayerName : currentNpcName;
        }

        currentPanel = activePanel;
        currentFullText = string.IsNullOrWhiteSpace(line.text) ? "..." : line.text;
        nextFastForwardAdvanceAt = Time.unscaledTime + fastForwardAutoAdvanceDelay;

        StartTypewriter(activePanel, currentFullText);

        activePanel.Root?.SetAsLastSibling();
    }

    private void SetPanelVisible(DialoguePanelBinding panel, bool visible)
    {
        if (panel.Root == null)
            return;

        if (visible)
        {
            UI_NpcPanelUtility.ActivatePanel(panel.Root);
            PreparePanelForDisplay(panel);
        }

        panel.Root.gameObject.SetActive(visible);
    }

    private void PreparePanelForDisplay(DialoguePanelBinding panel)
    {
        if (panel.Root == null)
            return;

        CanvasGroup[] groups = panel.Root.GetComponentsInChildren<CanvasGroup>(true);

        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] == null)
                continue;

            groups[i].alpha = 1f;
            groups[i].interactable = true;
            groups[i].blocksRaycasts = true;
        }
    }

    private void ResolvePanels()
    {
        ResolvePanels(false);
    }

    private void ResolvePanels(bool forceRebind)
    {
        if (npcPanel.Root == null)
        {
            npcPanel.Root = UI_NpcPanelUtility.FindNpcPanelChild(
                "\u5deb\u5973Dialogue_UI",
                "NPC Dialogue_UI",
                "Npc Dialogue_UI",
                "NPCDialogue_UI",
                "NpcDialogue_UI",
                "Dialogue_UI",
                "\u5bf9\u8bdd_UI",
                "\u5bf9\u8bdd",
                "Dialogue",
                "NpcDialogue");
        }

        if (forceRebind || !npcPanel.IsValid)
            BindPanel(npcPanel);

        if (playerPanel.Root == null)
        {
            playerPanel.Root = UI_NpcPanelUtility.FindNpcPanelChild(
                "Player Dialogue_UI",
                "PlayerDialogue_UI",
                "Player Dialogue",
                "PlayerDialogue",
                "\u73a9\u5bb6Dialogue_UI",
                "\u73a9\u5bb6\u5bf9\u8bdd_UI",
                "\u73a9\u5bb6\u5bf9\u8bdd");
        }

        if (forceRebind || !playerPanel.IsValid)
            BindPanel(playerPanel);
    }

    private void BindPanel(DialoguePanelBinding panel)
    {
        if (panel.Root == null)
            return;

        panel.NameText = null;
        panel.BodyText = null;

        TextMeshProUGUI[] texts = panel.Root.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (texts == null || texts.Length <= 0)
            return;

        panel.NameText = FindTextByName(texts, "Name", "NpcName", "PlayerName", "Title", "\u540d\u5b57", "\u6807\u9898");
        panel.BodyText = FindTextByName(texts, "Dialogue", "NpcDialogue", "PlayerDialogue", "Content", "Text", "\u5185\u5bb9", "\u5bf9\u8bdd");

        if (panel.BodyText == null)
            panel.BodyText = FindLargestText(texts, panel.NameText);

        if (panel.NameText == panel.BodyText && texts.Length > 1)
            panel.BodyText = FindLargestText(texts, panel.NameText);

        if (panel.BodyText == null && texts.Length > 0)
            panel.BodyText = texts[texts.Length - 1];
    }

    private void StartTypewriter(DialoguePanelBinding panel, string text)
    {
        StopTypewriter();

        if (panel.BodyText == null)
            return;

        if (!useTypewriter || charactersPerSecond <= 0f)
        {
            panel.BodyText.text = text;
            isTyping = false;
            return;
        }

        typewriterRoutine = StartCoroutine(TypewriterRoutine(panel.BodyText, text));
    }

    private IEnumerator TypewriterRoutine(TextMeshProUGUI targetText, string text)
    {
        isTyping = true;
        targetText.text = string.Empty;

        float visibleCharacters = 0f;
        int previousCount = 0;

        while (visibleCharacters < text.Length)
        {
            float revealSpeed = IsFastForwardHeld()
                ? Mathf.Max(charactersPerSecond, fastForwardCharactersPerSecond)
                : charactersPerSecond;

            visibleCharacters += revealSpeed * Time.unscaledDeltaTime;
            int count = Mathf.Clamp(Mathf.FloorToInt(visibleCharacters), 0, text.Length);
            targetText.text = text.Substring(0, count);

            if (count > previousCount)
            {
                PlayTypeSoundIfNeeded(count);
                previousCount = count;
            }

            yield return null;
        }

        targetText.text = text;
        isTyping = false;
        nextFastForwardAdvanceAt = Time.unscaledTime + fastForwardAutoAdvanceDelay;
        typewriterRoutine = null;
    }

    private void PlayTypeSoundIfNeeded(int visibleCharacterCount)
    {
        if (visibleCharacterCount <= 0 || typeSoundEveryCharacters <= 0)
            return;

        if (visibleCharacterCount % typeSoundEveryCharacters != 0)
            return;

        if (typeSoundCooldown > 0f && Time.unscaledTime < nextTypeSoundTime)
            return;

        nextTypeSoundTime = Time.unscaledTime + typeSoundCooldown;
        PlayDialogueSound(typeCueName, typeClip);
    }

    private bool IsFastForwardHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    private void CompleteCurrentLine()
    {
        StopTypewriter();

        if (currentPanel != null && currentPanel.BodyText != null)
        {
            PrepareTextForDisplay(currentPanel.BodyText);
            currentPanel.BodyText.text = string.IsNullOrWhiteSpace(currentFullText) ? "..." : currentFullText;
        }
    }

    private void StopTypewriter()
    {
        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        typewriterRoutine = null;
        isTyping = false;
    }

    private TextMeshProUGUI FindTextByName(TextMeshProUGUI[] texts, params string[] names)
    {
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            string normalized = UI_NpcPanelUtility.NormalizeName(texts[i].name);

            for (int j = 0; j < names.Length; j++)
            {
                if (normalized.Contains(UI_NpcPanelUtility.NormalizeName(names[j])))
                    return texts[i];
            }
        }

        return null;
    }

    private TextMeshProUGUI FindLargestText(TextMeshProUGUI[] texts, TextMeshProUGUI excluded)
    {
        TextMeshProUGUI best = null;
        float bestArea = -1f;

        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];

            if (text == null || text == excluded)
                continue;

            RectTransform rectTransform = text.rectTransform;
            float area = rectTransform != null ? Mathf.Abs(rectTransform.rect.width * rectTransform.rect.height) : 0f;

            if (area > bestArea)
            {
                best = text;
                bestArea = area;
            }
        }

        return best;
    }

    private void PrepareTextForDisplay(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        text.gameObject.SetActive(true);
        text.enabled = true;

        Color color = text.color;
        color.a = 1f;
        text.color = color;
    }

    private void EnsureInputSupport()
    {
        Canvas canvas = null;

        if (npcPanel.Root != null)
            canvas = npcPanel.Root.GetComponentInParent<Canvas>(true);

        if (canvas == null && playerPanel.Root != null)
            canvas = playerPanel.Root.GetComponentInParent<Canvas>(true);

        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.transform.SetAsLastSibling();
        }
    }

    private void ResolveAudioEvents()
    {
        if (this == null)
            return;

        if (audioEvents != null)
            return;

        audioEvents = GetComponent<AudioEventPlayer>();

        if (audioEvents == null && npcPanel.Root != null)
            audioEvents = npcPanel.Root.GetComponentInParent<AudioEventPlayer>(true);

        if (audioEvents == null && playerPanel.Root != null)
            audioEvents = playerPanel.Root.GetComponentInParent<AudioEventPlayer>(true);
    }

    private void PlayDialogueSound(string cueName, AudioClip clip)
    {
        if (this == null)
            return;

        ResolveAudioEvents();
        AudioPlaybackUtility.PlayCueOrClip(audioEvents, cueName, clip, transform, audioVolume);
    }
}
