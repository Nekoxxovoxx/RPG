using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CraftWindow : MonoBehaviour
{
    [Header("Material State Colors")]
    [SerializeField] private Color enoughMaterialColor = Color.white;
    [SerializeField] private Color missingMaterialColor = new Color(1f, 0.35f, 0.25f, 1f);

    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Button craftButton;

    [SerializeField] private Image[] materialImage;

    private ItemData_Equipment currentCraftData;
    private int lastCraftFrame = -1;

    private void Awake()
    {
        BindCraftButton();
    }

    private void OnDestroy()
    {
        if (craftButton != null)
            craftButton.onClick.RemoveListener(HandleCraftButtonClicked);
    }

    public void SetupCraftWindow(ItemData_Equipment _data)
    {
        currentCraftData = _data;
        BindCraftButton();

        if (_data == null)
        {
            ClearCraftWindow();
            return;
        }

        for (int i = 0; i < materialImage.Length; i++)
        {
            materialImage[i].color = Color.clear;
            TextMeshProUGUI materialText = materialImage[i].GetComponentInChildren<TextMeshProUGUI>();

            if (materialText != null)
            {
                materialText.text = "";
                materialText.color = Color.clear;
            }
        }

        if (_data.craftingMaterials.Count > materialImage.Length)
            Debug.LogWarning("You have more materials amount than you have material slots in craft window");

        int visibleMaterialCount = Mathf.Min(_data.craftingMaterials.Count, materialImage.Length);

        for (int i = 0; i < visibleMaterialCount; i++)
        {
            InventoryItem requiredMaterial = _data.craftingMaterials[i];

            if (requiredMaterial == null || requiredMaterial.data == null)
                continue;

            materialImage[i].sprite = requiredMaterial.data.itemicon;
            materialImage[i].color = Color.white;

            TextMeshProUGUI materialSlotText = materialImage[i].GetComponentInChildren<TextMeshProUGUI>();

            if (materialSlotText != null)
            {
                int requiredAmount = Mathf.Max(1, requiredMaterial.stackSize);
                int ownedAmount = Inventory.instance != null
                    ? Inventory.instance.CountItem(requiredMaterial.data, false, true)
                    : 0;

                materialSlotText.text = requiredAmount.ToString();
                materialSlotText.color = ownedAmount >= requiredAmount ? enoughMaterialColor : missingMaterialColor;
            }
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = _data.itemicon;
            itemIcon.color = Color.white;
        }

        if (itemName != null)
            itemName.text = _data.itemName;

        if (itemDescription != null)
            itemDescription.text = _data.GetCraftDescription();
    }

    private void BindCraftButton()
    {
        if (craftButton == null)
            return;

        craftButton.onClick = new Button.ButtonClickedEvent();
        craftButton.onClick.AddListener(HandleCraftButtonClicked);
    }

    private void HandleCraftButtonClicked()
    {
        if (lastCraftFrame == Time.frameCount)
            return;

        lastCraftFrame = Time.frameCount;

        if (currentCraftData == null || Inventory.instance == null)
            return;

        Inventory.instance.CanCraft(currentCraftData, currentCraftData.craftingMaterials);
        SetupCraftWindow(currentCraftData);
    }

    private void ClearCraftWindow()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.clear;
        }

        if (itemName != null)
            itemName.text = "";

        if (itemDescription != null)
            itemDescription.text = "";

        for (int i = 0; i < materialImage.Length; i++)
        {
            if (materialImage[i] == null)
                continue;

            materialImage[i].sprite = null;
            materialImage[i].color = Color.clear;

            TextMeshProUGUI materialText = materialImage[i].GetComponentInChildren<TextMeshProUGUI>();

            if (materialText != null)
            {
                materialText.text = "";
                materialText.color = Color.clear;
            }
        }
    }
}
