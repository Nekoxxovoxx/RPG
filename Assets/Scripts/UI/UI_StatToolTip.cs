using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_StatToolTip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Vector2 mouseOffset = new Vector2(150, 150);
    [SerializeField] private Vector2 screenPadding = new Vector2(12, 12);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        MakeTooltipIgnoreRaycasts();
    }

    private void Update()
    {
        UpdateToolTipPosition();
    }

    public void ShowStatToolTip(string _text)
    {
        MakeTooltipIgnoreRaycasts();

        if (description != null)
            description.text = _text;

        gameObject.SetActive(true);

        if (rectTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        UpdateToolTipPosition();
    }

    public void HideStatToolTip()
    {
        if (description != null)
            description.text = "";

        gameObject.SetActive(false);
    }

    public void UpdateToolTipPosition()
    {
        Vector2 mousePosition = Input.mousePosition;

        float xOffset = mousePosition.x > Screen.width * .5f ? -mouseOffset.x : mouseOffset.x;
        float yOffset = mousePosition.y > Screen.height * .5f ? -mouseOffset.y : mouseOffset.y;

        Vector2 tooltipPosition = new Vector2(mousePosition.x + xOffset, mousePosition.y + yOffset);

        if (rectTransform != null)
        {
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;

            float minX = screenPadding.x + size.x * pivot.x;
            float maxX = Screen.width - screenPadding.x - size.x * (1f - pivot.x);
            float minY = screenPadding.y + size.y * pivot.y;
            float maxY = Screen.height - screenPadding.y - size.y * (1f - pivot.y);

            tooltipPosition.x = Mathf.Clamp(tooltipPosition.x, minX, maxX);
            tooltipPosition.y = Mathf.Clamp(tooltipPosition.y, minY, maxY);
        }

        transform.position = tooltipPosition;
    }

    private void MakeTooltipIgnoreRaycasts()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }
}
