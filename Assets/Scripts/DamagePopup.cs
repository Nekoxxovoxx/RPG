using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float moveUpSpeed = 2f;
    private float fadeDuration = 0.8f;
    private float disappearTimer = 0.6f;

    private Color textColor;
    private float timer;
    private bool isSetup;

    public static DamagePopup Create(Vector3 position, string text, Color color)
    {
        GameObject popupObject = new GameObject("DamagePopup");
        popupObject.transform.position = position;

        TextMeshPro tmp = popupObject.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 3;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = 10;

        DamagePopup popup = popupObject.AddComponent<DamagePopup>();
        popup.textMesh = tmp;
        popup.textColor = color;
        popup.timer = popup.fadeDuration;
        popup.isSetup = true;

        return popup;
    }

    public void Setup(string text, Color color)
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshPro>();

        if (textMesh == null)
            return;

        textMesh.text = text;
        textMesh.color = color;
        textColor = color;
        timer = fadeDuration;
        isSetup = true;
    }

    public void Setup(int damage, Color color)
    {
        Setup(damage.ToString(), color);
    }

    private void Update()
    {
        if (!isSetup)
            return;

        transform.position += Vector3.up * moveUpSpeed * Time.deltaTime;

        timer -= Time.deltaTime;

        if (timer < disappearTimer)
        {
            float alpha = timer / disappearTimer;
            textColor.a = alpha;
            textMesh.color = textColor;
        }

        if (timer <= 0)
            Destroy(gameObject);
    }
}
