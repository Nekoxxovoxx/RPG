using System.Text;
using UnityEngine;

public enum ItemType
{
    Material = 0,
    Equipment = 1,
    Item = 2,
    Ember = 3
}

[CreateAssetMenu(fileName = "New Item Data", menuName = "Data/Item")]
public class ItemData : ScriptableObject
{
    public ItemType itemType;
    public string itemName;
    public Sprite itemicon;

    [Header("物品描述")]
    [SerializeField, TextArea(2, 5), InspectorName("物品描述")]
    private string itemDescription;

    [Range(0, 100)]
    public float dropChance;

    protected StringBuilder sb = new StringBuilder();

    public string ItemDescription => itemDescription;

    public virtual string GetDescription()
    {
        sb.Length = 0;

        if (!string.IsNullOrWhiteSpace(itemDescription))
        {
            sb.AppendLine("物品描述：");
            sb.Append(itemDescription.Trim());
        }

        return sb.ToString();
    }
}
