using UnityEngine;

public class MainFlaskUpgradeBridge : MonoBehaviour
{
    [Header("\u6b8b\u606f\u9732\u6ef4\u5347\u7ea7")]
    [SerializeField, Min(1)] private int capacityUpgradeAmount = 1;
    [SerializeField, Range(0.01f, 1f)] private float healingUpgradeAmount = 0.1f;

    public void UpgradeCapacity()
    {
        PlayerFlaskSystem.GetOrCreate().UpgradeMainFlaskCapacity(capacityUpgradeAmount);
    }

    public void UpgradeHealing()
    {
        PlayerFlaskSystem.GetOrCreate().UpgradeMainFlaskHealing(healingUpgradeAmount);
    }

    public void UpgradeCapacityAndHealing()
    {
        PlayerFlaskSystem flask = PlayerFlaskSystem.GetOrCreate();
        flask.UpgradeMainFlaskCapacity(capacityUpgradeAmount);
        flask.UpgradeMainFlaskHealing(healingUpgradeAmount);
    }
}
