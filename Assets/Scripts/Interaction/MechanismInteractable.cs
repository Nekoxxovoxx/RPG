using UnityEngine;
using UnityEngine.Events;

public class MechanismInteractable : Interactable
{
    [Header("机关")]
    [SerializeField] private bool oneShot;
    [SerializeField] private UnityEvent onInteract;

    private bool used;

    public override bool CanInteract(Player player)
    {
        return base.CanInteract(player) && (!oneShot || !used);
    }

    protected override void OnInteract(Player player)
    {
        used = true;
        onInteract?.Invoke();

        Debug.Log("触发机关: " + gameObject.name);

        if (oneShot)
            SetCanInteract(false);
    }
}
