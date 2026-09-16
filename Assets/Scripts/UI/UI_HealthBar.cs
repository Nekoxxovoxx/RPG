using UnityEngine;
using UnityEngine.UI;

public class UI_HealthBar : MonoBehaviour
{
    private Entity entity;
    private CharacterStats myStats;
    private RectTransform myTransform;
    private Slider slider;
    private CanvasGroup canvasGroup;
    private bool isPlayerHealthBar;
    private bool initialized;
    private bool subscribedToEntity;
    private bool subscribedToStats;
    private bool subscribedToSettings;

    private void OnEnable()
    {
        Initialize();
        Subscribe();
        UpdateHealthUI();
        ApplyVisibilitySetting();
    }

    private void Start()
    {
        Initialize();
        Subscribe();
        UpdateHealthUI();
        ApplyVisibilitySetting();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        myTransform = GetComponent<RectTransform>();
        entity = GetComponentInParent<Entity>();
        slider = GetComponentInChildren<Slider>();
        myStats = GetComponentInParent<CharacterStats>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        isPlayerHealthBar = myStats is PlayerStats || GetComponentInParent<Player>() != null;
        ConfigureReadOnlySlider();
        initialized = true;
    }

    private void Subscribe()
    {
        if (entity != null && !subscribedToEntity)
        {
            entity.onFilpped += FlipUI;
            subscribedToEntity = true;
        }

        if (myStats != null && !subscribedToStats)
        {
            myStats.onHealthChanged += UpdateHealthUI;
            subscribedToStats = true;
        }

        if (!subscribedToSettings)
        {
            HealthBarDisplaySettings.OnSettingsChanged += ApplyVisibilitySetting;
            subscribedToSettings = true;
        }
    }

    private void UpdateHealthUI()
    {
        if (slider == null || myStats == null)
            return;

        int maxHealth = Mathf.Max(1, myStats.GetMaxHealthValue());
        slider.maxValue = maxHealth;
        slider.SetValueWithoutNotify(Mathf.Clamp(myStats.currentHealth, 0, maxHealth));
    }

    private void FlipUI()
    {
        if (myTransform != null)
            myTransform.Rotate(0, 180, 0);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (entity != null && subscribedToEntity)
        {
            entity.onFilpped -= FlipUI;
            subscribedToEntity = false;
        }

        if (myStats != null && subscribedToStats)
        {
            myStats.onHealthChanged -= UpdateHealthUI;
            subscribedToStats = false;
        }

        if (subscribedToSettings)
        {
            HealthBarDisplaySettings.OnSettingsChanged -= ApplyVisibilitySetting;
            subscribedToSettings = false;
        }
    }

    private void ApplyVisibilitySetting()
    {
        if (canvasGroup == null)
            return;

        bool visible = HealthBarDisplaySettings.ShouldShow(isPlayerHealthBar);
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void ConfigureReadOnlySlider()
    {
        if (slider == null)
            return;

        slider.interactable = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };

        Graphic[] graphics = slider.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }
}
