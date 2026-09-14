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

    private void Start()
    {
        myTransform = GetComponent<RectTransform>();
        entity = GetComponentInParent<Entity>();
        slider = GetComponentInChildren<Slider>();
        myStats = GetComponentInParent<CharacterStats>();
        canvasGroup = GetComponent<CanvasGroup>();
        ConfigureReadOnlySlider();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        isPlayerHealthBar = myStats is PlayerStats || GetComponentInParent<Player>() != null;

        if (entity != null)
            entity.onFilpped += FlipUI;

        if (myStats != null)
            myStats.onHealthChanged += UpdateHealthUI;

        HealthBarDisplaySettings.OnSettingsChanged += ApplyVisibilitySetting;
        UpdateHealthUI();
        ApplyVisibilitySetting();
    }

    private void UpdateHealthUI()
    {
        if (slider == null || myStats == null)
            return;

        int maxHealth = Mathf.Max(1, myStats.GetMaxHealthValue());
        slider.maxValue = maxHealth;
        slider.SetValueWithoutNotify(Mathf.Clamp(myStats.currentHealth, 0, maxHealth));
    }

    private void FlipUI() => myTransform.Rotate(0, 180, 0);
    private void OnDisable()
    {
        if (entity != null)
            entity.onFilpped -= FlipUI;

        if (myStats != null)
            myStats.onHealthChanged -= UpdateHealthUI;

        HealthBarDisplaySettings.OnSettingsChanged -= ApplyVisibilitySetting;
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
