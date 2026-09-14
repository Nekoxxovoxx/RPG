using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UI_BossHealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool hideWhenTargetDies = true;

    private CharacterStats targetStats;
    private bool isSubscribed;

    private void Awake()
    {
        ResolveReferences();
        ConfigureReadOnlySlider();

        if (hideOnStart)
            Hide();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureReadOnlySlider();
        Subscribe();
        UpdateHealthUI();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        if (targetStats == null || visualRoot == null || !visualRoot.activeInHierarchy)
            return;

        UpdateHealthUI();
    }

    public void Configure(Slider targetSlider, GameObject targetVisualRoot)
    {
        if (targetSlider != null)
            slider = targetSlider;

        if (targetVisualRoot != null)
            visualRoot = targetVisualRoot;

        ResolveReferences();
        ConfigureReadOnlySlider();
        UpdateHealthUI();
    }

    public void SetHideWhenTargetDies(bool shouldHide)
    {
        hideWhenTargetDies = shouldHide;
        UpdateHealthUI();
    }

    public void Bind(CharacterStats stats)
    {
        if (targetStats == stats)
        {
            UpdateHealthUI();
            return;
        }

        Unsubscribe();
        targetStats = stats;
        Subscribe();
        UpdateHealthUI();
    }

    public void Show()
    {
        ResolveReferences();

        if (visualRoot != null)
            visualRoot.SetActive(true);

        EnsureSliderVisible();
        UpdateHealthUI();
    }

    public void Hide()
    {
        ResolveReferences();

        if (visualRoot != null)
            visualRoot.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (visualRoot == null)
            visualRoot = gameObject;

        if (slider == null)
        {
            slider = GetComponent<Slider>();

            if (slider == null)
                slider = GetComponentInChildren<Slider>(true);
        }

        if (targetStats == null)
            targetStats = GetComponentInParent<CharacterStats>();
    }

    private void Subscribe()
    {
        if (isSubscribed || targetStats == null)
            return;

        targetStats.onHealthChanged += UpdateHealthUI;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || targetStats == null)
            return;

        targetStats.onHealthChanged -= UpdateHealthUI;
        isSubscribed = false;
    }

    private void UpdateHealthUI()
    {
        if (slider == null || targetStats == null)
            return;

        EnsureFillDrawOrder();

        int maxHealth = Mathf.Max(1, targetStats.GetMaxHealthValue());
        slider.maxValue = maxHealth;
        slider.SetValueWithoutNotify(Mathf.Clamp(targetStats.currentHealth, 0, maxHealth));

        if (hideWhenTargetDies && targetStats.currentHealth <= 0)
            Hide();
    }

    private void ConfigureReadOnlySlider()
    {
        if (slider == null)
            return;

        slider.interactable = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        EnsureFillDrawOrder();

        Graphic[] graphics = slider.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }

    private void EnsureSliderVisible()
    {
        if (slider == null)
            return;

        slider.gameObject.SetActive(true);

        if (slider.fillRect == null)
            return;

        if (slider.fillRect.parent != null)
            slider.fillRect.parent.gameObject.SetActive(true);

        slider.fillRect.gameObject.SetActive(true);
        EnsureFillDrawOrder();
    }

    private void EnsureFillDrawOrder()
    {
        if (slider == null || slider.fillRect == null)
            return;

        Transform fillParent = slider.fillRect.parent;

        if (fillParent != null && fillParent != slider.transform)
            fillParent.SetAsLastSibling();
        else
            slider.fillRect.SetAsLastSibling();
    }
}
