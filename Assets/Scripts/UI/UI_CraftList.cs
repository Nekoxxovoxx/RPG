using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_CraftList : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Transform craftSlotParent;
    [SerializeField] private GameObject craftSlotPrefab;

    [SerializeField] private List<ItemData_Equipment> craftEquipment;

    void Start()
    {
        transform.parent.GetChild(0).GetComponent<UI_CraftList>().SetupCraftList();
        SetupDefaultCraftWindow();
    }

    public void SetupCraftList()
    {
        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();

        for (int i = 0; i < craftSlotParent.childCount; i++)
        {
            Destroy(craftSlotParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < craftEquipment.Count; i++)
        {
            if (craftEquipment[i] == null)
                continue;

            if (flask != null && flask.IsMainFlaskItem(craftEquipment[i]))
                continue;

            GameObject newSlot = Instantiate(craftSlotPrefab, craftSlotParent);
            newSlot.GetComponent<UI_CraftSlot>().SetupCraftSlot(craftEquipment[i]);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetupCraftList();
    }

    public void SetupDefaultCraftWindow()
    {
        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();

        for (int i = 0; i < craftEquipment.Count; i++)
        {
            if (craftEquipment[i] == null)
                continue;

            if (flask != null && flask.IsMainFlaskItem(craftEquipment[i]))
                continue;

            GetComponentInParent<UI>().craftWindow.SetupCraftWindow(craftEquipment[i]);
            return;
        }

        GetComponentInParent<UI>().craftWindow.SetupCraftWindow(null);
    }
}
