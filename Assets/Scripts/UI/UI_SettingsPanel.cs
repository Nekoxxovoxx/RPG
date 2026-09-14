using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_SettingsPanel : MonoBehaviour
{
    private const string ContinueGameText = "\u7EE7\u7EED\u6E38\u620F";
    private const string ReturnMainMenuText = "\u8FD4\u56DE\u4E3B\u83DC\u5355";
    private const string YesText = "\u662F";
    private const string NoText = "\u5426";
    private const string EnemyText = "\u602A\u7269";
    private const string PlayerText = "\u89D2\u8272";
    private const string HealthBarText = "\u8840\u6761";
    private const string SfxTrackName = "\u97F3\u6548\u6761";
    private const string MusicTrackName = "\u97F3\u4E50\u6761";
    private const string SfxHandleName = "\u97F3\u6548\u6761\u63A7\u5236";
    private const string MusicHandleName = "\u97F3\u4E50\u6761\u63A7\u5236";
    private const string SfxRangeStartName = "\u97F3\u6548\u8303\u56F4A_0";
    private const string SfxRangeEndName = "\u97F3\u6548\u8303\u56F4B_1";
    private const string MusicRangeStartName = "\u97F3\u4E50\u8303\u56F4A_0";
    private const string MusicRangeEndName = "\u97F3\u4E50\u8303\u56F4B_1";

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button returnMainMenuButton;

    [Header("Enemy Health Bar")]
    [SerializeField] private Toggle enemyHealthYesToggle;
    [SerializeField] private Toggle enemyHealthNoToggle;
    [SerializeField] private Button enemyHealthYesButton;
    [SerializeField] private Button enemyHealthNoButton;

    [Header("Player Health Bar")]
    [SerializeField] private Toggle playerHealthYesToggle;
    [SerializeField] private Toggle playerHealthNoToggle;
    [SerializeField] private Button playerHealthYesButton;
    [SerializeField] private Button playerHealthNoButton;

    [Header("Volume Bars")]
    [SerializeField] private Image sfxVolumeTrackImage;
    [SerializeField] private Image sfxVolumeHandleImage;
    [SerializeField] private RectTransform sfxRangeStart;
    [SerializeField] private RectTransform sfxRangeEnd;
    [SerializeField] private Image musicVolumeTrackImage;
    [SerializeField] private Image musicVolumeHandleImage;
    [SerializeField] private RectTransform musicRangeStart;
    [SerializeField] private RectTransform musicRangeEnd;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Main menu";
    [SerializeField, Min(0f)] private float fadeToMenuDuration = 0.45f;
    [SerializeField, Min(0f)] private float fadeFromMenuDuration = 0.45f;

    private readonly Vector3[] rangeCorners = new Vector3[4];
    private UI owningUI;
    private float sfxHandleLockedY;
    private float musicHandleLockedY;

    private void Awake()
    {
        ResolveReferences();
        Bind();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Bind();
        Refresh();
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SaveVolumeSettings();
    }

    public void Open(UI ui)
    {
        owningUI = ui;
        gameObject.SetActive(true);
        ResolveReferences();
        Bind();
        Refresh();
    }

    public void ContinueGame()
    {
        gameObject.SetActive(false);

        if (owningUI == null)
            owningUI = GetComponentInParent<UI>();

        owningUI?.CloseActiveMenuWithEscape();
    }

    public void ReturnToMainMenu()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SaveVolumeSettings();

        WorldRestManager.AutoSaveCurrentGame();
        Time.timeScale = 1f;

        if (UI_ScreenFadeTransition.Instance != null)
            UI_ScreenFadeTransition.Instance.LoadSceneWithFade(mainMenuSceneName, fadeToMenuDuration, fadeFromMenuDuration);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetEnemyHealthBars(bool visible)
    {
        HealthBarDisplaySettings.ShowEnemyHealthBars = visible;
        Refresh();
    }

    public void SetPlayerHealthBars(bool visible)
    {
        HealthBarDisplaySettings.ShowPlayerHealthBars = visible;
        Refresh();
    }

    private void ResolveReferences()
    {
        if (owningUI == null)
            owningUI = GetComponentInParent<UI>();

        ResolveVolumeReferences();
        ResolveHealthAndButtonReferences();
        CacheHandleYPositions();
    }

    private void ResolveVolumeReferences()
    {
        if (sfxVolumeTrackImage == null)
            sfxVolumeTrackImage = FindChildComponent<Image>(SfxTrackName);

        if (sfxVolumeHandleImage == null)
            sfxVolumeHandleImage = FindChildComponent<Image>(SfxHandleName);

        if (musicVolumeTrackImage == null)
            musicVolumeTrackImage = FindChildComponent<Image>(MusicTrackName);

        if (musicVolumeHandleImage == null)
            musicVolumeHandleImage = FindChildComponent<Image>(MusicHandleName);

        if (sfxRangeStart == null)
            sfxRangeStart = FindChildComponent<RectTransform>(SfxRangeStartName);

        if (sfxRangeEnd == null)
            sfxRangeEnd = FindChildComponent<RectTransform>(SfxRangeEndName);

        if (musicRangeStart == null)
            musicRangeStart = FindChildComponent<RectTransform>(MusicRangeStartName);

        if (musicRangeEnd == null)
            musicRangeEnd = FindChildComponent<RectTransform>(MusicRangeEndName);
    }

    private void ResolveHealthAndButtonReferences()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            string searchText = GetSearchText(button.transform);

            if (continueButton == null && searchText.Contains(ContinueGameText))
            {
                continueButton = button;
                continue;
            }

            if (returnMainMenuButton == null && searchText.Contains(ReturnMainMenuText))
            {
                returnMainMenuButton = button;
                continue;
            }

            if (searchText.Contains(YesText) || searchText.Contains(NoText))
                TryAssignHealthButton(button, searchText);
        }

        Toggle[] toggles = GetComponentsInChildren<Toggle>(true);

        for (int i = 0; i < toggles.Length; i++)
        {
            Toggle toggle = toggles[i];

            if (toggle == null)
                continue;

            TryAssignHealthToggle(toggle, GetSearchText(toggle.transform));
        }
    }

    private void CacheHandleYPositions()
    {
        if (sfxVolumeHandleImage != null)
            sfxHandleLockedY = sfxVolumeHandleImage.rectTransform.anchoredPosition.y;

        if (musicVolumeHandleImage != null)
            musicHandleLockedY = musicVolumeHandleImage.rectTransform.anchoredPosition.y;
    }

    private void Bind()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(ContinueGame);
        }

        if (returnMainMenuButton != null)
        {
            returnMainMenuButton.onClick.RemoveAllListeners();
            returnMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        BindToggle(enemyHealthYesToggle, true, SetEnemyHealthBars);
        BindToggle(enemyHealthNoToggle, false, SetEnemyHealthBars);
        BindToggle(playerHealthYesToggle, true, SetPlayerHealthBars);
        BindToggle(playerHealthNoToggle, false, SetPlayerHealthBars);

        BindButton(enemyHealthYesButton, () => SetEnemyHealthBars(true));
        BindButton(enemyHealthNoButton, () => SetEnemyHealthBars(false));
        BindButton(playerHealthYesButton, () => SetPlayerHealthBars(true));
        BindButton(playerHealthNoButton, () => SetPlayerHealthBars(false));

        BindVolumeHandle(sfxVolumeHandleImage, OnSfxVolumeDrag, SaveVolumeSettings);
        BindVolumeHandle(musicVolumeHandleImage, OnMusicVolumeDrag, SaveVolumeSettings);
    }

    private void Refresh()
    {
        bool showEnemy = HealthBarDisplaySettings.ShowEnemyHealthBars;
        bool showPlayer = HealthBarDisplaySettings.ShowPlayerHealthBars;

        SetToggleWithoutNotify(enemyHealthYesToggle, showEnemy);
        SetToggleWithoutNotify(enemyHealthNoToggle, !showEnemy);
        SetToggleWithoutNotify(playerHealthYesToggle, showPlayer);
        SetToggleWithoutNotify(playerHealthNoToggle, !showPlayer);

        SetButtonChecked(enemyHealthYesButton, showEnemy);
        SetButtonChecked(enemyHealthNoButton, !showEnemy);
        SetButtonChecked(playerHealthYesButton, showPlayer);
        SetButtonChecked(playerHealthNoButton, !showPlayer);

        RefreshVolumeHandles();
    }

    private void RefreshVolumeHandles()
    {
        AudioManager audioManager = AudioManager.GetOrCreateInstance();

        SetHandleFromValue(
            sfxVolumeHandleImage,
            sfxVolumeTrackImage,
            sfxRangeStart,
            sfxRangeEnd,
            sfxHandleLockedY,
            audioManager.SfxVolume);

        SetHandleFromValue(
            musicVolumeHandleImage,
            musicVolumeTrackImage,
            musicRangeStart,
            musicRangeEnd,
            musicHandleLockedY,
            audioManager.MusicVolume);
    }

    private void BindVolumeHandle(Image handleImage, UnityEngine.Events.UnityAction<BaseEventData> dragAction, UnityEngine.Events.UnityAction endDragAction)
    {
        if (handleImage == null)
            return;

        handleImage.raycastTarget = true;

        EventTrigger trigger = handleImage.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = handleImage.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.RemoveAll(entry =>
            entry.eventID == EventTriggerType.BeginDrag ||
            entry.eventID == EventTriggerType.Drag ||
            entry.eventID == EventTriggerType.EndDrag);

        AddEventTriggerEntry(trigger, EventTriggerType.BeginDrag, dragAction);
        AddEventTriggerEntry(trigger, EventTriggerType.Drag, dragAction);
        AddEventTriggerEntry(trigger, EventTriggerType.EndDrag, data =>
        {
            dragAction.Invoke(data);
            endDragAction.Invoke();
        });
    }

    private void AddEventTriggerEntry(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    private void OnSfxVolumeDrag(BaseEventData eventData)
    {
        float value = ApplyDragToHandle(
            eventData,
            sfxVolumeHandleImage,
            sfxVolumeTrackImage,
            sfxRangeStart,
            sfxRangeEnd,
            sfxHandleLockedY);

        AudioManager.GetOrCreateInstance().SetSfxVolume(value, false);
    }

    private void OnMusicVolumeDrag(BaseEventData eventData)
    {
        float value = ApplyDragToHandle(
            eventData,
            musicVolumeHandleImage,
            musicVolumeTrackImage,
            musicRangeStart,
            musicRangeEnd,
            musicHandleLockedY);

        AudioManager.GetOrCreateInstance().SetMusicVolume(value, false);
    }

    private float ApplyDragToHandle(
        BaseEventData baseEventData,
        Image handleImage,
        Image fallbackTrackImage,
        RectTransform rangeStart,
        RectTransform rangeEnd,
        float lockedY)
    {
        if (handleImage == null)
            return 0f;

        PointerEventData pointerEventData = baseEventData as PointerEventData;

        if (pointerEventData == null)
            return GetValueFromHandle(handleImage, fallbackTrackImage, rangeStart, rangeEnd);

        RectTransform handleTransform = handleImage.rectTransform;
        RectTransform parentRect = handleTransform.parent as RectTransform;

        if (parentRect == null)
            return 0f;

        Camera eventCamera = pointerEventData.pressEventCamera != null
            ? pointerEventData.pressEventCamera
            : pointerEventData.enterEventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, pointerEventData.position, eventCamera, out Vector2 localPoint))
            return GetValueFromHandle(handleImage, fallbackTrackImage, rangeStart, rangeEnd);

        if (!TryGetRangeInParentSpace(parentRect, fallbackTrackImage, rangeStart, rangeEnd, out float minX, out float maxX))
            return 0f;

        float clampedX = Mathf.Clamp(localPoint.x, minX, maxX);
        handleTransform.anchoredPosition = new Vector2(clampedX, lockedY);

        return Mathf.InverseLerp(minX, maxX, clampedX);
    }

    private void SetHandleFromValue(
        Image handleImage,
        Image fallbackTrackImage,
        RectTransform rangeStart,
        RectTransform rangeEnd,
        float lockedY,
        float value)
    {
        if (handleImage == null)
            return;

        RectTransform handleTransform = handleImage.rectTransform;
        RectTransform parentRect = handleTransform.parent as RectTransform;

        if (parentRect == null)
            return;

        if (!TryGetRangeInParentSpace(parentRect, fallbackTrackImage, rangeStart, rangeEnd, out float minX, out float maxX))
            return;

        float x = Mathf.Lerp(minX, maxX, Mathf.Clamp01(value));
        handleTransform.anchoredPosition = new Vector2(x, lockedY);
    }

    private float GetValueFromHandle(Image handleImage, Image fallbackTrackImage, RectTransform rangeStart, RectTransform rangeEnd)
    {
        if (handleImage == null)
            return 0f;

        RectTransform handleTransform = handleImage.rectTransform;
        RectTransform parentRect = handleTransform.parent as RectTransform;

        if (parentRect == null)
            return 0f;

        if (!TryGetRangeInParentSpace(parentRect, fallbackTrackImage, rangeStart, rangeEnd, out float minX, out float maxX))
            return 0f;

        return Mathf.InverseLerp(minX, maxX, handleTransform.anchoredPosition.x);
    }

    private bool TryGetRangeInParentSpace(
        RectTransform parentRect,
        Image fallbackTrackImage,
        RectTransform rangeStart,
        RectTransform rangeEnd,
        out float minX,
        out float maxX)
    {
        minX = 0f;
        maxX = 0f;

        if (parentRect == null)
            return false;

        if (rangeStart != null && rangeEnd != null)
        {
            float startX = parentRect.InverseTransformPoint(rangeStart.position).x;
            float endX = parentRect.InverseTransformPoint(rangeEnd.position).x;
            minX = Mathf.Min(startX, endX);
            maxX = Mathf.Max(startX, endX);
            return !Mathf.Approximately(minX, maxX);
        }

        if (fallbackTrackImage == null)
            return false;

        RectTransform trackRect = fallbackTrackImage.rectTransform;
        trackRect.GetWorldCorners(rangeCorners);

        float leftX = parentRect.InverseTransformPoint(rangeCorners[0]).x;
        float rightX = parentRect.InverseTransformPoint(rangeCorners[2]).x;
        minX = Mathf.Min(leftX, rightX);
        maxX = Mathf.Max(leftX, rightX);

        return !Mathf.Approximately(minX, maxX);
    }

    private void SaveVolumeSettings()
    {
        AudioManager.GetOrCreateInstance().SaveVolumeSettings();
    }

    private void TryAssignHealthToggle(Toggle toggle, string searchText)
    {
        bool isYes = searchText.Contains(YesText);
        bool isNo = searchText.Contains(NoText);

        if (!isYes && !isNo)
            return;

        string rowText = FindNearestRowText(toggle.transform);
        bool isEnemyRow = rowText.Contains(EnemyText);
        bool isPlayerRow = rowText.Contains(PlayerText);

        if (isEnemyRow)
        {
            if (isYes && enemyHealthYesToggle == null)
                enemyHealthYesToggle = toggle;
            else if (isNo && enemyHealthNoToggle == null)
                enemyHealthNoToggle = toggle;
        }
        else if (isPlayerRow)
        {
            if (isYes && playerHealthYesToggle == null)
                playerHealthYesToggle = toggle;
            else if (isNo && playerHealthNoToggle == null)
                playerHealthNoToggle = toggle;
        }
    }

    private void TryAssignHealthButton(Button button, string searchText)
    {
        bool isYes = searchText.Contains(YesText);
        bool isNo = searchText.Contains(NoText);

        if (!isYes && !isNo)
            return;

        string rowText = FindNearestRowText(button.transform);
        bool isEnemyRow = rowText.Contains(EnemyText);
        bool isPlayerRow = rowText.Contains(PlayerText);

        if (isEnemyRow)
        {
            if (isYes && enemyHealthYesButton == null)
                enemyHealthYesButton = button;
            else if (isNo && enemyHealthNoButton == null)
                enemyHealthNoButton = button;
        }
        else if (isPlayerRow)
        {
            if (isYes && playerHealthYesButton == null)
                playerHealthYesButton = button;
            else if (isNo && playerHealthNoButton == null)
                playerHealthNoButton = button;
        }
    }

    private void BindToggle(Toggle toggle, bool valueToApply, UnityEngine.Events.UnityAction<bool> setter)
    {
        if (toggle == null)
            return;

        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
                setter.Invoke(valueToApply);
            else
                Refresh();
        });
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void SetToggleWithoutNotify(Toggle toggle, bool isOn)
    {
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(isOn);
    }

    private void SetButtonChecked(Button button, bool selected)
    {
        if (button == null)
            return;

        Graphic graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();

        if (graphic != null)
            graphic.color = Color.white;

        GameObject checkmark = FindOrCreateCheckmark(button.transform);

        if (checkmark != null)
            checkmark.SetActive(selected);
    }

    private GameObject FindOrCreateCheckmark(Transform root)
    {
        if (root == null)
            return null;

        Transform existing = FindCheckmarkTransform(root);

        if (existing != null)
            return existing.gameObject;

        GameObject checkmarkObject = new GameObject("Auto Checkmark");
        checkmarkObject.transform.SetParent(root, false);

        RectTransform rectTransform = checkmarkObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI text = checkmarkObject.AddComponent<TextMeshProUGUI>();
        text.text = "\u221A";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 40f;
        text.raycastTarget = false;
        text.color = Color.white;

        return checkmarkObject;
    }

    private Transform FindCheckmarkTransform(Transform root)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
                continue;

            string value = (text.text ?? string.Empty) + text.gameObject.name;

            if (value.Contains("\u221A") ||
                value.Contains("Check") ||
                value.Contains("check") ||
                value.Contains("Tick") ||
                value.Contains("tick"))
                return text.transform;
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (image == null || image.transform == root)
                continue;

            string imageName = image.gameObject.name;

            if (imageName.Contains("Check") ||
                imageName.Contains("check") ||
                imageName.Contains("Tick") ||
                imageName.Contains("tick") ||
                imageName.Contains("\u9009\u4E2D"))
                return image.transform;
        }

        return null;
    }

    private string FindNearestRowText(Transform control)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        TMP_Text best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null || text.GetComponentInParent<Button>() != null || text.GetComponentInParent<Toggle>() != null)
                continue;

            string value = text.text ?? string.Empty;

            if (!value.Contains(HealthBarText) && !value.Contains(EnemyText) && !value.Contains(PlayerText))
                continue;

            float distance = Mathf.Abs(text.transform.position.y - control.position.y);

            if (distance < bestDistance)
            {
                best = text;
                bestDistance = distance;
            }
        }

        return best != null ? best.text : string.Empty;
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);

            if (found != null)
                return found;
        }

        return null;
    }

    private string GetSearchText(Transform root)
    {
        if (root == null)
            return string.Empty;

        string searchText = root.name;
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
            if (texts[i] != null)
                searchText += texts[i].text;

        return searchText;
    }

    private void OnDrawGizmosSelected()
    {
        ResolveVolumeReferences();
        DrawVolumeRangeGizmo(sfxRangeStart, sfxRangeEnd, new Color(1f, 0.55f, 0.1f, 1f));
        DrawVolumeRangeGizmo(musicRangeStart, musicRangeEnd, new Color(0.25f, 0.75f, 1f, 1f));
    }

    private void DrawVolumeRangeGizmo(RectTransform start, RectTransform end, Color color)
    {
        if (start == null || end == null)
            return;

        Gizmos.color = color;
        Gizmos.DrawLine(start.position, end.position);
        Gizmos.DrawWireSphere(start.position, 8f);
        Gizmos.DrawWireSphere(end.position, 8f);
    }
}
