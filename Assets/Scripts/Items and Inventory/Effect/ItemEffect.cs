using UnityEngine;


public class ItemEffect : ScriptableObject
{
    public virtual void OnEquip(ItemData_Equipment item, PlayerStats playerStats)
    {
    }

    public virtual void OnUnequip(ItemData_Equipment item, PlayerStats playerStats)
    {
    }

    public virtual void ExecuteEffect(Transform _enemyPosition)
    {
    }
}
