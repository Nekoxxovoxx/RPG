using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_NpcInteractionMenu : MonoBehaviour
{
    public class MenuOption
    {
        public readonly string Label;
        public readonly Action OnSelected;

        public MenuOption(string label, Action onSelected)
        {
            Label = label;
            OnSelected = onSelected;
        }
    }

    private static UI_NpcInteractionMenu instance;
    private static int lastClosedFrame = -1;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Selection Feedback")]
    [SerializeField] private Color normalOptionColor = new Color(0.25f, 0.035f, 0.035f, 0.95f);
    [SerializeField] private Color selectedOptionColor = new Color(0.78f, 0.14f, 0.04f, 0.95f);
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color selectedTextColor = new Color(1f, 0.92f, 0.62f, 1f);
    [SerializeField] private Color selectedFrameColor = new Color(1f, 0.78f, 0.28f, 1f);
    [SerializeField] private Vector2 selectedFrameDistance = new Vector2(3f, -3f);

    [Header("World Anchor")]
    [SerializeField] private bool followNpcLeftSide = true;
    [SerializeField] private bool pinToFixedWorldPosition = true;
    [SerializeField] private bool useNpcRendererBounds = true;
    [SerializeField] private float npcLeftGap = 0.08f;
    [SerializeField] private Vector3 npcWorldOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private Vector2 screenPadding = new Vector2(24f, 24f);

    [Header("Audio")]
    [SerializeField] private AudioEventPlayer audioEvents;
    [SerializeField] private string openCueName = "NpcMenuOpen";
    [SerializeField] private string closeCueName = "NpcMenuClose";
    [SerializeField] private string moveCueName = "NpcMenuMove";
    [SerializeField] private string selectCueName = "NpcMenuSelect";
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private AudioClip selectClip;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.8f;

    private readonly List<MenuOption> options = new List<MenuOption>();
    private readonly List<Button> optionButtons = new List<Button>();
    private readonly List<Graphic> optionBackgrounds = new List<Graphic>();
    private readonly List<TextMeshProUGUI> optionTexts = new List<TextMeshProUGUI>();
    private readonly List<Outline> optionFrames = new List<Outline>();

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Transform followAnchor;
    private int selectedIndex;
    private float inputReadyAt;
    private bool initialized;
    private bool showRequested;
    private bool hasFixedWorldPosition;
    private Vector3 fixedWorldPosition;

    public static UI_NpcInteractionMenu Instance => instance;
    public static bool IsOpen => instance != null && instance.gameObject.activeInHierarchy;
    public static bool WasClosedThisFrame => lastClosedFrame == Time.frameCount;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        Initialize();

        if (!showRequested)
            Hide(false, false);
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || Time.unscaledTime < inputReadyAt)
            return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            SelectCurrent();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    private void LateUpdate()
    {
        if (!gameObject.activeInHierarchy || !followNpcLeftSide)
            return;

        UpdateAnchoredPosition();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static UI_NpcInteractionMenu GetOrCreate()
    {
        if (instance != null)
            return instance;

        Transform designedRoot = UI_NpcPanelUtility.FindNpcPanelChild(
            "\u9009\u9879_UI",
            "\u9009\u9879",
            "Options",
            "NPC\u9009\u9879_UI",
            "NPC\u9009\u9879",
            "\u5deb\u5973\u9009\u9879_UI",
            "\u5deb\u5973\u9009\u9879",
            "\u4ea4\u4e92\u9009\u9879_UI",
            "\u4ea4\u4e92\u9009\u9879",
            "NpcOptions_UI",
            "NPC_Options_UI",
            "NpcInteractionMenu_UI");

        if (designedRoot == null)
        {
            Debug.LogWarning("Could not find NPC_UI/\u9009\u9879_UI. Please keep the NPC option panel under NPC_UI.");
            return null;
        }

        UI_NpcInteractionMenu menu = designedRoot.GetComponent<UI_NpcInteractionMenu>();

        if (menu == null)
            menu = designedRoot.gameObject.AddComponent<UI_NpcInteractionMenu>();

        instance = menu;
        return menu;
    }

    public void Show(string npcName, List<MenuOption> menuOptions)
    {
        Show(npcName, menuOptions, null);
    }

    public void Show(string npcName, List<MenuOption> menuOptions, Transform anchor)
    {
        if (menuOptions == null || menuOptions.Count == 0)
            return;

        showRequested = true;
        UI_NpcPanelUtility.ActivatePanel(transform);
        gameObject.SetActive(true);
        Initialize();
        showRequested = false;

        options.Clear();
        options.AddRange(menuOptions);
        selectedIndex = 0;
        inputReadyAt = Time.unscaledTime + 0.15f;
        followAnchor = anchor;
        CaptureFixedWorldPosition();

        transform.SetAsLastSibling();
        PlayMenuSound(openCueName, openClip);
        UI_DialogueConversationSystem.Instance?.Hide();
        UI_NpcPanelUtility.HideNamedDialoguePanelOnly();
        EnsureInputSupport();
        BindDesignedLayout();

        if (optionButtons.Count <= 0)
        {
            Debug.LogWarning("No Button found under NPC_UI/\u9009\u9879_UI. Please place the option buttons inside that panel.", this);
            return;
        }

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName;

        RebuildOptions();
        RefreshSelection();
        UpdateAnchoredPosition();
    }

    public void Hide()
    {
        Hide(true, true);
    }

    private void Hide(bool recordCloseFrame)
    {
        Hide(recordCloseFrame, true);
    }

    private void Hide(bool recordCloseFrame, bool playCloseSound)
    {
        options.Clear();
        followAnchor = null;
        hasFixedWorldPosition = false;
        bool wasOpen = gameObject.activeSelf || gameObject.activeInHierarchy;

        if (gameObject.activeSelf)
            gameObject.SetActive(false);

        if (recordCloseFrame && wasOpen)
            lastClosedFrame = Time.frameCount;

        if (playCloseSound && wasOpen)
            PlayMenuSound(closeCueName, closeClip);

        UI_NpcPanelUtility.HideNpcRootIfIdle();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        rectTransform = transform as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>(true);

        if (audioEvents == null)
            audioEvents = GetComponent<AudioEventPlayer>();

        if (audioEvents == null)
            audioEvents = GetComponentInParent<AudioEventPlayer>(true);

        if (rectTransform != null && followNpcLeftSide)
            rectTransform.pivot = new Vector2(1f, 0.5f);

        BindDesignedLayout();
        initialized = true;
    }

    public void SelectIndex(int index)
    {
        if (index < 0 || index >= options.Count)
            return;

        selectedIndex = index;
        SelectCurrent();
    }

    public void SetHoveredIndex(int index)
    {
        if (index < 0 || index >= options.Count)
            return;

        if (selectedIndex == index)
            return;

        selectedIndex = index;
        PlayMenuSound(moveCueName, moveClip);
        RefreshSelection();
    }

    private void BindDesignedLayout()
    {
        optionButtons.Clear();
        optionBackgrounds.Clear();
        optionTexts.Clear();
        optionFrames.Clear();

        if (titleText == null)
            titleText = FindTextByName("Title", "\u6807\u9898", "NpcName", "NPCName", "Name", "\u540d\u5b57");

        Button[] buttons = GetComponentsInChildren<Button>(true);

        if (buttons.Length <= 0)
            buttons = BuildButtonsFromDesignedTextItems();

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            optionButtons.Add(button);
            optionBackgrounds.Add(EnsureClickableGraphic(button));
            optionFrames.Add(EnsureSelectionFrame(button));
            optionTexts.Add(button.GetComponentInChildren<TextMeshProUGUI>(true));
        }
    }

    private void RebuildOptions()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            bool active = i < options.Count;
            Button button = optionButtons[i];

            if (button == null)
                continue;

            button.gameObject.SetActive(active);

            if (!active)
                continue;

            button.interactable = true;

            if (i < optionTexts.Count && optionTexts[i] != null)
            {
                optionTexts[i].text = options[i].Label;
                optionTexts[i].raycastTarget = optionButtons[i] != null && optionButtons[i].targetGraphic == optionTexts[i];
            }

            UI_NpcMenuButtonRelay relay = button.GetComponent<UI_NpcMenuButtonRelay>();

            if (relay == null)
                relay = button.gameObject.AddComponent<UI_NpcMenuButtonRelay>();

            relay.Configure(this, i);
        }
    }

    private void MoveSelection(int direction)
    {
        if (options.Count <= 0)
            return;

        selectedIndex = (selectedIndex + direction + options.Count) % options.Count;
        PlayMenuSound(moveCueName, moveClip);
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            bool selected = i == selectedIndex;

            if (i < optionBackgrounds.Count && optionBackgrounds[i] != null)
                optionBackgrounds[i].color = selected ? selectedOptionColor : normalOptionColor;

            if (i < optionTexts.Count && optionTexts[i] != null)
                optionTexts[i].color = selected ? selectedTextColor : normalTextColor;

            if (i < optionFrames.Count && optionFrames[i] != null)
                optionFrames[i].enabled = selected;

            if (selected && optionButtons[i] != null && optionButtons[i].gameObject.activeInHierarchy)
                EventSystem.current?.SetSelectedGameObject(optionButtons[i].gameObject);
        }
    }

    private void SelectCurrent()
    {
        if (selectedIndex < 0 || selectedIndex >= options.Count)
            return;

        Action action = options[selectedIndex].OnSelected;
        PlayMenuSound(selectCueName, selectClip);
        Hide(true, false);
        action?.Invoke();
    }

    private void PlayMenuSound(string cueName, AudioClip clip)
    {
        AudioPlaybackUtility.PlayCueOrClip(audioEvents, cueName, clip, transform, audioVolume);
    }

    private Graphic EnsureClickableGraphic(Button button)
    {
        if (button == null)
            return null;

        Graphic graphic = button.targetGraphic;

        if (graphic == null)
            graphic = button.GetComponent<Graphic>();

        if (graphic == null)
        {
            Image image = button.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            graphic = image;
        }

        graphic.raycastTarget = true;
        button.targetGraphic = graphic;
        return graphic;
    }

    private Outline EnsureSelectionFrame(Button button)
    {
        if (button == null)
            return null;

        Outline frame = button.GetComponent<Outline>();

        if (frame == null)
            frame = button.gameObject.AddComponent<Outline>();

        frame.effectColor = selectedFrameColor;
        frame.effectDistance = selectedFrameDistance;
        frame.useGraphicAlpha = false;
        frame.enabled = false;
        return frame;
    }

    private Button[] BuildButtonsFromDesignedTextItems()
    {
        List<Button> builtButtons = new List<Button>();
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];

            if (text == null || text == titleText || string.IsNullOrWhiteSpace(text.text))
                continue;

            Transform target = ResolveOptionButtonRoot(text.transform);
            Button button = target.GetComponent<Button>();

            if (button == null)
                button = target.gameObject.AddComponent<Button>();

            Graphic graphic = target.GetComponent<Graphic>();

            if (graphic == null)
                graphic = text;

            graphic.raycastTarget = true;
            button.targetGraphic = graphic;
            builtButtons.Add(button);
        }

        return builtButtons.ToArray();
    }

    private Transform ResolveOptionButtonRoot(Transform textTransform)
    {
        if (textTransform == null)
            return transform;

        Transform parent = textTransform.parent;

        if (parent != null && parent != transform && parent.GetComponent<Graphic>() != null)
            return parent;

        if (parent != null && parent != transform && parent.GetComponent<RectTransform>() != null)
            return parent;

        return textTransform;
    }

    private void EnsureInputSupport()
    {
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>(true);

        if (parentCanvas != null)
        {
            if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay && parentCanvas.worldCamera == null)
                parentCanvas.worldCamera = GetWorldCamera(parentCanvas);

            if (parentCanvas.GetComponent<GraphicRaycaster>() == null)
                parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.transform.SetAsLastSibling();
        }
    }

    private void UpdateAnchoredPosition()
    {
        if (followAnchor == null && !hasFixedWorldPosition)
            return;

        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform == null)
            return;

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>(true);

        if (parentCanvas == null)
            return;

        Camera worldCamera = GetWorldCamera(parentCanvas);
        Vector3 worldPosition = GetCurrentAnchorWorldPosition();
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
        screenPosition.x = Mathf.Clamp(screenPosition.x, screenPadding.x, Screen.width - screenPadding.x);
        screenPosition.y = Mathf.Clamp(screenPosition.y, screenPadding.y, Screen.height - screenPadding.y);

        RectTransform targetParent = rectTransform.parent as RectTransform;

        if (targetParent == null)
            return;

        Camera uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : GetWorldCamera(parentCanvas);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetParent, screenPosition, uiCamera, out Vector2 localPoint))
            rectTransform.anchoredPosition = localPoint;
    }

    private void CaptureFixedWorldPosition()
    {
        hasFixedWorldPosition = false;

        if (!pinToFixedWorldPosition || followAnchor == null)
            return;

        fixedWorldPosition = GetFollowWorldPosition();
        hasFixedWorldPosition = true;
    }

    private Vector3 GetCurrentAnchorWorldPosition()
    {
        if (pinToFixedWorldPosition && hasFixedWorldPosition)
            return fixedWorldPosition;

        return GetFollowWorldPosition();
    }

    private Vector3 GetFollowWorldPosition()
    {
        if (followAnchor == null)
            return Vector3.zero;

        if (!useNpcRendererBounds)
            return followAnchor.position + npcWorldOffset;

        Renderer[] renderers = followAnchor.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
            return followAnchor.position + npcWorldOffset;

        Vector3 position = bounds.center;
        position.x = bounds.min.x - npcLeftGap;
        position += npcWorldOffset;
        return position;
    }

    private Camera GetWorldCamera(Canvas canvas)
    {
        if (canvas != null && canvas.worldCamera != null)
            return canvas.worldCamera;

        if (Camera.main != null)
            return Camera.main;

        return FindObjectOfType<Camera>();
    }

    private TextMeshProUGUI FindTextByName(params string[] names)
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            string normalized = UI_NpcPanelUtility.NormalizeName(texts[i].name);

            for (int j = 0; j < names.Length; j++)
            {
                if (normalized.Contains(UI_NpcPanelUtility.NormalizeName(names[j])))
                    return texts[i];
            }
        }

        return null;
    }
}

public class UI_NpcMenuButtonRelay : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISubmitHandler
{
    private UI_NpcInteractionMenu menu;
    private Button button;
    private int index;
    private bool bound;

    public void Configure(UI_NpcInteractionMenu owner, int optionIndex)
    {
        menu = owner;
        index = optionIndex;

        if (button == null)
            button = GetComponent<Button>();

        if (button == null)
            return;

        button.interactable = true;

        if (button.targetGraphic != null)
            button.targetGraphic.raycastTarget = true;

        if (bound)
            button.onClick.RemoveListener(HandleClick);

        button.onClick.AddListener(HandleClick);
        bound = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        menu?.SetHoveredIndex(index);
        EventSystem.current?.SetSelectedGameObject(gameObject);
    }

    private void HandleClick()
    {
        menu?.SelectIndex(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        HandleClick();
    }

    private void OnDestroy()
    {
        if (bound && button != null)
            button.onClick.RemoveListener(HandleClick);
    }
}

internal static class UI_NpcPanelUtility
{
    private static readonly string[] NpcRootNames =
    {
        "NPC_UI",
        "Npc_UI",
        "NpcUI",
        "NPCUI"
    };

    public static Transform FindNpcPanelChild(params string[] childNames)
    {
        Transform root = FindNpcRoot();

        if (root != null)
        {
            Transform child = FindChildRecursive(root, childNames);

            if (child != null)
                return child;
        }

        return null;
    }

    public static void ActivatePanel(Transform panel)
    {
        if (panel == null)
            return;

        List<GameObject> chain = new List<GameObject>();

        for (Transform current = panel; current != null; current = current.parent)
            chain.Add(current.gameObject);

        for (int i = chain.Count - 1; i >= 0; i--)
            chain[i].SetActive(true);
    }

    public static void HideNpcRootIfIdle()
    {
        Transform root = FindNpcRoot();

        if (root == null)
            return;

        UI_NpcInteractionMenu[] menus = root.GetComponentsInChildren<UI_NpcInteractionMenu>(true);

        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i] != null && menus[i].gameObject.activeSelf)
                return;
        }

        if (UI_DialogueConversationSystem.IsOpen)
            return;

        root.gameObject.SetActive(false);
    }

    public static void HideNamedDialoguePanelOnly()
    {
        Transform dialoguePanel = FindNpcPanelChild(
            "\u5deb\u5973Dialogue_UI",
            "Dialogue_UI",
            "NpcDialogue_UI",
            "\u5bf9\u8bdd_UI",
            "\u5bf9\u8bdd",
            "Dialogue",
            "NpcDialogue");

        if (dialoguePanel != null)
            dialoguePanel.gameObject.SetActive(false);

        Transform playerDialoguePanel = FindNpcPanelChild(
            "Player Dialogue_UI",
            "PlayerDialogue_UI",
            "Player Dialogue",
            "PlayerDialogue",
            "\u73a9\u5bb6Dialogue_UI",
            "\u73a9\u5bb6\u5bf9\u8bdd_UI",
            "\u73a9\u5bb6\u5bf9\u8bdd");

        if (playerDialoguePanel != null)
            playerDialoguePanel.gameObject.SetActive(false);
    }

    public static void HideNamedOptionPanelOnly()
    {
        Transform optionPanel = FindNpcPanelChild(
            "\u9009\u9879_UI",
            "\u9009\u9879",
            "Options",
            "NPC\u9009\u9879_UI",
            "NPC\u9009\u9879",
            "\u5deb\u5973\u9009\u9879_UI",
            "\u5deb\u5973\u9009\u9879",
            "\u4ea4\u4e92\u9009\u9879_UI",
            "\u4ea4\u4e92\u9009\u9879",
            "NpcOptions_UI",
            "NPC_Options_UI",
            "NpcInteractionMenu_UI");

        if (optionPanel != null)
            optionPanel.gameObject.SetActive(false);
    }

    public static string NormalizeName(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .ToLowerInvariant();
    }

    private static Transform FindNpcRoot()
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];

            if (!IsValidSceneTransform(target))
                continue;

            for (int j = 0; j < NpcRootNames.Length; j++)
            {
                if (NormalizeName(target.name) == NormalizeName(NpcRootNames[j]))
                    return target;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, params string[] names)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            for (int j = 0; j < names.Length; j++)
            {
                if (NormalizeName(child.name) == NormalizeName(names[j]))
                    return child;
            }

            Transform nested = FindChildRecursive(child, names);

            if (nested != null)
                return nested;
        }

        return null;
    }

    private static bool IsValidSceneTransform(Transform target)
    {
        return target != null &&
               target.hideFlags == HideFlags.None &&
               target.gameObject.scene.IsValid();
    }
}
