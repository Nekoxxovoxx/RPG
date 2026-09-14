using UnityEngine;
using TMPro;

public abstract class Interactable : MonoBehaviour, IInteractable
{
    [Header("交互")]
    [SerializeField] private string interactionPrompt = "按 E 交互";
    [SerializeField] private bool canInteract = true;

    [Header("交互提示")]
    [SerializeField] private TextMeshPro promptText;

    public virtual string InteractionPrompt => interactionPrompt;
    public virtual Transform InteractionTransform => transform;

    protected virtual void Awake()
    {
        SetPromptVisible(false);
    }

    public virtual bool CanInteract(Player player)
    {
        return canInteract && isActiveAndEnabled;
    }

    public void Interact(Player player)
    {
        if (!CanInteract(player))
            return;

        OnInteract(player);
    }

    protected void SetCanInteract(bool value)
    {
        canInteract = value;
    }

    public void SetPromptVisible(bool visible)
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    protected abstract void OnInteract(Player player);
}
