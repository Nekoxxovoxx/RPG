using System.Collections;
using TMPro;
using UnityEngine;

[AddComponentMenu("Dialogue/Opening Monologue Controller")]
public class OpeningMonologueController : MonoBehaviour
{
    [System.Serializable]
    public class MonologueLine
    {
        [TextArea(2, 5)]
        public string text;
    }

    [Header("UI")]
    [SerializeField] private GameObject dialogueBoxRoot;
    [SerializeField] private TextMeshProUGUI monologueText;

    [Header("Monologue")]
    [SerializeField] private MonologueLine[] monologueLines;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool hideWhenFinished = true;

    [Header("Text Reveal")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField, Min(1f)] private float charactersPerSecond = 28f;
    [SerializeField, Min(0f)] private float inputDelay = 0.12f;

    private int currentIndex;
    private bool isPlaying;
    private bool isTyping;
    private float inputReadyAt;
    private string currentFullText;
    private Coroutine typewriterRoutine;

    public GameObject DialogueBoxRoot
    {
        get
        {
            ResolveReferences();
            return dialogueBoxRoot;
        }
    }

    public TextMeshProUGUI MonologueText
    {
        get
        {
            ResolveReferences();
            return monologueText;
        }
    }

    public MonologueLine[] GetConfiguredLinesCopy()
    {
        EnsureLinesFromExistingText();

        if (monologueLines == null)
            return new MonologueLine[0];

        MonologueLine[] copy = new MonologueLine[monologueLines.Length];

        for (int i = 0; i < monologueLines.Length; i++)
        {
            copy[i] = new MonologueLine
            {
                text = monologueLines[i] != null ? monologueLines[i].text : string.Empty
            };
        }

        return copy;
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    private void Update()
    {
        if (!isPlaying || Time.unscaledTime < inputReadyAt)
            return;

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            AdvanceOrCompleteLine();
        }
    }

    public void Play()
    {
        ResolveReferences();

        if (monologueText == null)
            return;

        if (dialogueBoxRoot != null)
            dialogueBoxRoot.SetActive(true);

        EnsureLinesFromExistingText();

        currentIndex = 0;
        isPlaying = true;
        inputReadyAt = Time.unscaledTime + inputDelay;
        RefreshCurrentLine();
    }

    public void Stop()
    {
        StopTypewriter();
        isPlaying = false;

        if (hideWhenFinished && dialogueBoxRoot != null)
            dialogueBoxRoot.SetActive(false);
    }

    private void AdvanceOrCompleteLine()
    {
        if (isTyping)
        {
            CompleteCurrentLine();
            return;
        }

        currentIndex++;

        if (currentIndex >= GetLineCount())
        {
            Stop();
            return;
        }

        inputReadyAt = Time.unscaledTime + inputDelay;
        RefreshCurrentLine();
    }

    private void RefreshCurrentLine()
    {
        string line = GetLine(currentIndex);
        currentFullText = string.IsNullOrWhiteSpace(line) ? "..." : line.Trim();
        StartTypewriter(currentFullText);
    }

    private void StartTypewriter(string text)
    {
        StopTypewriter();

        if (!useTypewriter || charactersPerSecond <= 0f)
        {
            monologueText.text = text;
            isTyping = false;
            return;
        }

        typewriterRoutine = StartCoroutine(TypewriterRoutine(text));
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        isTyping = true;
        monologueText.text = string.Empty;

        float visibleCharacters = 0f;

        while (visibleCharacters < text.Length)
        {
            visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
            int count = Mathf.Clamp(Mathf.FloorToInt(visibleCharacters), 0, text.Length);
            monologueText.text = text.Substring(0, count);
            yield return null;
        }

        monologueText.text = text;
        isTyping = false;
        typewriterRoutine = null;
    }

    private void CompleteCurrentLine()
    {
        StopTypewriter();
        monologueText.text = string.IsNullOrWhiteSpace(currentFullText) ? "..." : currentFullText;
    }

    private void StopTypewriter()
    {
        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        typewriterRoutine = null;
        isTyping = false;
    }

    private int GetLineCount()
    {
        return monologueLines != null ? monologueLines.Length : 0;
    }

    private string GetLine(int index)
    {
        if (monologueLines == null || index < 0 || index >= monologueLines.Length || monologueLines[index] == null)
            return string.Empty;

        return monologueLines[index].text;
    }

    private void EnsureLinesFromExistingText()
    {
        if (GetLineCount() > 0 || monologueText == null || string.IsNullOrWhiteSpace(monologueText.text))
            return;

        monologueLines = new[]
        {
            new MonologueLine { text = monologueText.text }
        };
    }

    private void ResolveReferences()
    {
        if (monologueText == null)
            monologueText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (dialogueBoxRoot == null && monologueText != null)
            dialogueBoxRoot = monologueText.transform.parent != null
                ? monologueText.transform.parent.gameObject
                : monologueText.gameObject;
    }
}
