using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float interactionRadius = 1.8f;
    [SerializeField] private Vector2 interactionOffset = Vector2.zero;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private bool ignoreWhenPlayerDead = true;
    [SerializeField] private bool drawGizmos = true;

    private Player player;
    private IInteractable currentInteractable;
    private IInteractable previousInteractable;
    private UI_InteractionPrompt interactionPromptUI;

    public IInteractable CurrentInteractable => currentInteractable;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {
        interactionPromptUI = UI_InteractionPrompt.GetOrCreate();
    }

    private void Update()
    {
        if (UI_NpcInteractionMenu.IsOpen || UI_NpcInteractionMenu.WasClosedThisFrame ||
            UI_DialogueConversationSystem.IsOpen || UI_DialogueConversationSystem.WasClosedThisFrame)
        {
            ClearCurrentInteractable();
            return;
        }

        if (!CanReadInteractionInput())
        {
            ClearCurrentInteractable();
            return;
        }

        RefreshCurrentInteractable();

        if (Input.GetKeyDown(interactionKey))
            TryInteract();
    }

    public bool TryInteract()
    {
        RefreshCurrentInteractable();

        if (currentInteractable == null)
            return false;

        currentInteractable.Interact(player);
        RefreshCurrentInteractable();
        return true;
    }

    private bool CanReadInteractionInput()
    {
        if (player == null)
            return false;

        if (ignoreWhenPlayerDead && player.stats != null && player.stats.isDead)
            return false;

        return true;
    }

    private void RefreshCurrentInteractable()
    {
        previousInteractable = currentInteractable;
        currentInteractable = FindClosestInteractable();
        UpdateInteractionPrompt();
    }

    private void UpdateInteractionPrompt()
    {
        if (previousInteractable == currentInteractable)
            return;

        if (currentInteractable != null)
            ShowInteractionPrompt(currentInteractable);
        else
            HideInteractionPrompt();
    }

    private void ShowInteractionPrompt(IInteractable interactable)
    {
        if (interactionPromptUI == null)
            interactionPromptUI = UI_InteractionPrompt.GetOrCreate();

        interactionPromptUI?.Show(interactable, interactionKey);
    }

    private void HideInteractionPrompt()
    {
        if (interactionPromptUI == null)
            interactionPromptUI = UI_InteractionPrompt.Instance;

        interactionPromptUI?.Hide();
    }

    private void ClearCurrentInteractable()
    {
        HideInteractionPrompt();
        previousInteractable = null;
        currentInteractable = null;
    }

    private void OnDisable()
    {
        ClearCurrentInteractable();
    }

    private IInteractable FindClosestInteractable()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(GetInteractionCenter(), interactionRadius, interactableLayers);

        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            IInteractable interactable = hits[i].GetComponentInParent<IInteractable>();

            if (interactable == null)
                interactable = hits[i].GetComponent<IInteractable>();

            if (interactable == null || !interactable.CanInteract(player))
                continue;

            Transform targetTransform = interactable.InteractionTransform;
            Vector2 targetPosition = targetTransform != null ? targetTransform.position : hits[i].transform.position;
            float distance = ((Vector2)transform.position - targetPosition).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        return closestInteractable;
    }

    private Vector2 GetInteractionCenter()
    {
        int facingDirection = player != null ? player.facingDir : 1;
        Vector2 facingOffset = new Vector2(interactionOffset.x * facingDirection, interactionOffset.y);
        return (Vector2)transform.position + facingOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Player gizmoPlayer = player != null ? player : GetComponent<Player>();
        int facingDirection = gizmoPlayer != null ? gizmoPlayer.facingDir : 1;
        Vector2 facingOffset = new Vector2(interactionOffset.x * facingDirection, interactionOffset.y);

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere((Vector2)transform.position + facingOffset, interactionRadius);
    }
}
