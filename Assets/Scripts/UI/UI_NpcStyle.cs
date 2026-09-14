using UnityEngine;

[CreateAssetMenu(fileName = "NpcUiStyle", menuName = "UI/NPC UI Style")]
public class UI_NpcStyle : ScriptableObject
{
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonSprite;

    public Sprite PanelSprite => panelSprite;
    public Sprite ButtonSprite => buttonSprite;
}
