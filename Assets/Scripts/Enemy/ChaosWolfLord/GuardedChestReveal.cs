using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GuardedChestReveal : MonoBehaviour
{
    [Header("Guard")]
    [SerializeField] private Enemy_ChaosWolfLord wolfLord;

    [Header("Chest")]
    [SerializeField] private ChestInteractable targetChest;
    [SerializeField] private Transform chestFocusTarget;
    [SerializeField] private bool hideChestOnStart = true;
    [SerializeField, Min(0f)] private float autoBindChestMaxDistance = 28f;

    [Header("Dialogue")]
    [SerializeField] private string npcName = "混沌狼主";
    [SerializeField] private string playerSpeakerName = "你";
    [SerializeField] private DialogueLine[] deathDialogueLines;

    [Header("Camera")]
    [SerializeField, Min(0f)] private float panToWolfDuration = 0.7f;
    [SerializeField, Min(0.5f)] private float chestFocusOrthographicSize = 4.2f;
    [SerializeField, Min(0f)] private float chestFocusDuration = 0.7f;
    [SerializeField, Min(0f)] private float chestFocusHoldDuration = 0.8f;
    [SerializeField, Min(0f)] private float cameraRestoreDuration = 0.55f;

    [Header("Player")]
    [SerializeField] private bool lockPlayerDuringReveal = true;

    private BossPhaseTransitionDirector cameraDirector;
    private Coroutine revealRoutine;
    private Player lockedPlayer;
    private bool revealFinished;

    private void Awake()
    {
        ResolveReferences();
        ApplyInitialChestState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (wolfLord != null)
            wolfLord.OnDeathStarted += HandleWolfDeathStarted;
    }

    private void OnDisable()
    {
        if (wolfLord != null)
            wolfLord.OnDeathStarted -= HandleWolfDeathStarted;

        if (!revealFinished)
            UnlockPlayer();
    }

    private void ResolveReferences()
    {
        if (wolfLord == null)
            wolfLord = GetComponent<Enemy_ChaosWolfLord>();

        if (cameraDirector == null)
        {
            cameraDirector = GetComponent<BossPhaseTransitionDirector>();

            if (cameraDirector == null)
                cameraDirector = gameObject.AddComponent<BossPhaseTransitionDirector>();
        }

        if (targetChest == null)
            targetChest = FindNearestGuardedChest();
    }

    private void ApplyInitialChestState()
    {
        if (targetChest == null)
            return;

        SetChestInteractable(false);

        if (hideChestOnStart)
            SetChestVisible(false);
    }

    private void HandleWolfDeathStarted(Enemy_ChaosWolfLord defeatedWolf)
    {
        if (revealRoutine != null)
            return;

        revealRoutine = StartCoroutine(RevealRoutine(defeatedWolf));
    }

    private IEnumerator RevealRoutine(Enemy_ChaosWolfLord defeatedWolf)
    {
        revealFinished = false;
        LockPlayer();
        bool cameraSequenceStarted = false;

        if (cameraDirector != null)
        {
            cameraDirector.BeginCameraSequence();
            cameraSequenceStarted = true;

            if (defeatedWolf != null)
                yield return cameraDirector.PanHorizontallyTo(defeatedWolf.transform, panToWolfDuration);
        }

        yield return PlayDeathDialogue();

        SetChestVisible(true);
        SetChestInteractable(false);

        Transform focusTarget = ResolveChestFocusTarget();

        if (cameraDirector != null && focusTarget != null)
        {
            yield return cameraDirector.FocusOnTarget(focusTarget, chestFocusOrthographicSize, chestFocusDuration);

            if (chestFocusHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(chestFocusHoldDuration);
        }

        if (cameraSequenceStarted && cameraDirector != null)
            yield return cameraDirector.RestoreCamera(cameraRestoreDuration);

        SetChestInteractable(true);
        UnlockPlayer();
        revealFinished = true;
        revealRoutine = null;
    }

    private IEnumerator PlayDeathDialogue()
    {
        List<DialogueLine> validLines = GetValidDialogueLines();

        if (validLines.Count <= 0)
            yield break;

        UI_DialogueConversationSystem panel = UI_DialogueConversationSystem.GetOrCreate();

        if (panel == null)
            yield break;

        panel.ShowConversation(npcName, playerSpeakerName, validLines);

        yield return null;

        while (UI_DialogueConversationSystem.IsOpen)
            yield return null;
    }

    private List<DialogueLine> GetValidDialogueLines()
    {
        List<DialogueLine> validLines = new List<DialogueLine>();

        if (deathDialogueLines == null)
            return validLines;

        for (int i = 0; i < deathDialogueLines.Length; i++)
        {
            DialogueLine line = deathDialogueLines[i];

            if (line != null && !string.IsNullOrWhiteSpace(line.text))
                validLines.Add(new DialogueLine(line.speaker, line.text.Trim()));
        }

        return validLines;
    }

    private Transform ResolveChestFocusTarget()
    {
        if (chestFocusTarget != null)
            return chestFocusTarget;

        return targetChest != null ? targetChest.transform : null;
    }

    private void SetChestVisible(bool visible)
    {
        if (targetChest == null)
            return;

        if (targetChest.gameObject == gameObject)
            return;

        targetChest.gameObject.SetActive(visible);
    }

    private void SetChestInteractable(bool canInteract)
    {
        if (targetChest == null)
            return;

        targetChest.enabled = canInteract;
    }

    private ChestInteractable FindNearestGuardedChest()
    {
        ChestInteractable[] chests = Resources.FindObjectsOfTypeAll<ChestInteractable>();
        ChestInteractable nearest = null;
        float nearestSqrDistance = float.MaxValue;
        float maxSqrDistance = autoBindChestMaxDistance > 0f
            ? autoBindChestMaxDistance * autoBindChestMaxDistance
            : float.MaxValue;

        for (int i = 0; chests != null && i < chests.Length; i++)
        {
            ChestInteractable chest = chests[i];

            if (chest == null || chest.gameObject == gameObject || chest.gameObject.scene != gameObject.scene)
                continue;

            float sqrDistance = ((Vector2)chest.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (sqrDistance > maxSqrDistance || sqrDistance >= nearestSqrDistance)
                continue;

            nearest = chest;
            nearestSqrDistance = sqrDistance;
        }

        return nearest;
    }

    private void LockPlayer()
    {
        if (!lockPlayerDuringReveal)
            return;

        lockedPlayer = ResolvePlayer();
        lockedPlayer?.BeginCutsceneControlLock();
    }

    private void UnlockPlayer()
    {
        if (lockedPlayer == null)
            return;

        lockedPlayer.EndCutsceneControlLock();
        lockedPlayer = null;
    }

    private Player ResolvePlayer()
    {
        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            return PlayerManager.instance.player;

        return FindObjectOfType<Player>();
    }
}
