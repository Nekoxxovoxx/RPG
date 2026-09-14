using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_UpgradeConfirmPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action confirmAction;
    private Action cancelAction;

    public static UI_UpgradeConfirmPopup GetOrCreate(Transform parent, TMP_FontAsset font)
    {
        if (parent == null)
            return null;

        UI_UpgradeConfirmPopup existing = parent.GetComponentInChildren<UI_UpgradeConfirmPopup>(true);

        if (existing != null)
        {
            existing.ApplyFont(font);
            return existing;
        }

        GameObject popupObject = new GameObject("UpgradeConfirmPopup", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(UI_UpgradeConfirmPopup));
        popupObject.transform.SetParent(parent, false);

        RectTransform rectTransform = popupObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(440f, 220f);

        Image background = popupObject.GetComponent<Image>();
        background.color = new Color(0.055f, 0.025f, 0.025f, 0.96f);

        Outline outline = popupObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.08f, 0.04f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);

        UI_UpgradeConfirmPopup popup = popupObject.GetComponent<UI_UpgradeConfirmPopup>();
        popup.messageText = CreateText(popupObject.transform, "MessageText", font, "是否确认重铸？", new Vector2(0f, 48f), new Vector2(380f, 72f), 28f);
        popup.confirmButton = CreateButton(popupObject.transform, "ConfirmButton", font, "是", new Vector2(-82f, -58f));
        popup.cancelButton = CreateButton(popupObject.transform, "CancelButton", font, "否", new Vector2(82f, -58f));
        popup.BindButtons();
        popup.Hide();

        return popup;
    }

    public void Show(string message, Action onConfirm, Action onCancel = null)
    {
        confirmAction = onConfirm;
        cancelAction = onCancel;

        if (messageText != null)
            messageText.text = message;

        BindButtons();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void BindButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(HandleCancel);
        }
    }

    private void HandleConfirm()
    {
        Action action = confirmAction;
        ClearActions();
        Hide();
        action?.Invoke();
    }

    private void HandleCancel()
    {
        Action action = cancelAction;
        ClearActions();
        Hide();
        action?.Invoke();
    }

    private void ClearActions()
    {
        confirmAction = null;
        cancelAction = null;
    }

    private void ApplyFont(TMP_FontAsset font)
    {
        if (font == null)
            return;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
            texts[i].font = font;
    }

    private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, string text, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = font;
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        textComponent.enableWordWrapping = true;
        textComponent.raycastTarget = false;

        return textComponent;
    }

    private static Button CreateButton(Transform parent, string name, TMP_FontAsset font, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(112f, 48f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.33f, 0.045f, 0.045f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.33f, 0.045f, 0.045f, 0.95f);
        colors.highlightedColor = new Color(0.55f, 0.075f, 0.06f, 1f);
        colors.pressedColor = new Color(0.2f, 0.02f, 0.02f, 1f);
        button.colors = colors;

        CreateText(buttonObject.transform, "Text", font, label, Vector2.zero, new Vector2(112f, 48f), 24f);

        return button;
    }
}
