using UnityEngine;

[CreateAssetMenu(fileName = "New Level Currency", menuName = "Data/Level/Currency")]
public class LevelCurrencyData : ScriptableObject
{
    [SerializeField] private ItemType itemType = ItemType.Ember;
    [SerializeField] private string displayName = "余烬";
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea(2, 5)] private string description = "残留在灵魂深处的火光，可用于祭礼升级、交易购买等。";
    [SerializeField] private string saveKey = "PlayerEmbers";
    [SerializeField, Min(0)] private int startingAmount;

    public ItemType ItemType => itemType;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public string SaveKey => saveKey;
    public int StartingAmount => startingAmount;
}
