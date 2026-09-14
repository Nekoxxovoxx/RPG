using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string startSceneName = "level1";

    [Header("Start Transition")]
    [SerializeField, Min(0f)] private float startFadeToBlackDuration = 0.8f;
    [SerializeField, Min(0f)] private float startFadeFromBlackDuration = 0.8f;
    [SerializeField] private bool openingSceneControlsFadeIn = true;

    [Header("Panels")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Buttons")]
    [SerializeField] private GameObject startButtonRoot;
    [SerializeField] private GameObject continueButtonRoot;
    [SerializeField] private GameObject newGameButtonRoot;

    private const string StartGameText = "\u5f00\u59cb\u6e38\u620f";
    private const string ContinueGameText = "\u7ee7\u7eed\u6e38\u620f";
    private const string NewGameText = "\u65b0\u6e38\u620f";

    private void Awake()
    {
        ResolveMainButtons();
        BindMainButtons();
        RefreshStartButtons();
    }

    private void OnEnable()
    {
        ResolveMainButtons();
        BindMainButtons();
        RefreshStartButtons();
    }

    public void StartGame()
    {
        if (HasExistingProgress())
        {
            ContinueGame();
            return;
        }

        WorldRestManager.MarkGameStarted();
        RefreshStartButtons();
        LoadNewGameScene();
    }

    public void ContinueGame()
    {
        WorldRestManager.MarkGameStarted();
        RefreshStartButtons();

        if (!WorldRestManager.HasAutoSaveSnapshot() && !WorldRestManager.HasSavedRestPoint())
        {
            LoadNewGameScene();
            return;
        }

        string savedSceneName = WorldRestManager.GetContinueSceneName(startSceneName);
        WorldRestManager.RequestContinueFromAutoSaveAfterNextSceneLoad();

        UI_ScreenFadeTransition.Instance.LoadSceneWithFade(
            savedSceneName,
            startFadeToBlackDuration,
            startFadeFromBlackDuration,
            true);
    }

    public void NewGame()
    {
        WorldRestManager.ClearGameplayProgress();
        RefreshStartButtons();
    }

    public void OpenCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        WorldRestManager.AutoSaveCurrentGame();
        Debug.Log("Quit game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadNewGameScene()
    {
        bool isOpeningScene = openingSceneControlsFadeIn &&
                              string.Equals(startSceneName, "kaimujuqing", System.StringComparison.OrdinalIgnoreCase);

        UI_ScreenFadeTransition.Instance.LoadSceneWithFade(
            startSceneName,
            startFadeToBlackDuration,
            startFadeFromBlackDuration,
            !isOpeningScene);
    }

    private void RefreshStartButtons()
    {
        bool hasStartedGame = HasExistingProgress();

        if (startButtonRoot != null)
            startButtonRoot.SetActive(!hasStartedGame);

        if (continueButtonRoot != null)
            continueButtonRoot.SetActive(hasStartedGame);

        if (newGameButtonRoot != null)
            newGameButtonRoot.SetActive(hasStartedGame);
    }

    private bool HasExistingProgress()
    {
        return WorldRestManager.HasStartedGame() ||
               WorldRestManager.HasSavedRestPoint() ||
               WorldRestManager.HasAutoSaveSnapshot();
    }

    private void BindMainButtons()
    {
        BindButton(startButtonRoot, StartGame);
        BindButton(continueButtonRoot, ContinueGame);
        BindButton(newGameButtonRoot, NewGame);
    }

    private void BindButton(GameObject root, UnityEngine.Events.UnityAction action)
    {
        if (root == null)
            return;

        Button button = root.GetComponent<Button>();

        if (button == null)
            button = root.GetComponentInChildren<Button>(true);

        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void ResolveMainButtons()
    {
        if (startButtonRoot != null && continueButtonRoot != null && newGameButtonRoot != null)
            return;

        Button[] buttons = FindObjectsOfType<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null || IsInsidePanel(button.transform, settingsPanel) || IsInsidePanel(button.transform, creditsPanel))
                continue;

            string searchText = GetSearchText(button.transform);

            if (startButtonRoot == null && ContainsMenuText(searchText, StartGameText))
                startButtonRoot = button.gameObject;

            if (continueButtonRoot == null && ContainsMenuText(searchText, ContinueGameText))
                continueButtonRoot = button.gameObject;

            if (newGameButtonRoot == null && ContainsMenuText(searchText, NewGameText))
                newGameButtonRoot = button.gameObject;
        }
    }

    private bool IsInsidePanel(Transform target, GameObject panel)
    {
        if (target == null || panel == null)
            return false;

        return target == panel.transform || target.IsChildOf(panel.transform);
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

    private bool ContainsMenuText(string searchText, string expectedText)
    {
        if (string.IsNullOrWhiteSpace(searchText) || string.IsNullOrWhiteSpace(expectedText))
            return false;

        return searchText.IndexOf(expectedText, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
