using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    Transform InteractionTransform { get; }
    bool CanInteract(Player player);
    void Interact(Player player);
}
