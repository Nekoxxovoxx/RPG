using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Flags]
public enum LargeSkullNpcFunction
{
    None = 0,
    Dialogue = 1,
    ReturnFire = 2,
    Upgrade = 4,
    FlaskUpgrade = 8
}

public enum LargeSkullNpcVisibilityState
{
    Hidden,
    Showing,
    Visible,
    Hiding
}

[AddComponentMenu("NPC/\u4F8D\u706B\u5DEB\u5973")]
public class LargeSkullNpcInteractable : Interactable
{
    [Header("\u51fa\u73b0/\u6d88\u5931")]
    [SerializeField, Min(0.1f)] private float revealRange = 10.5f;
    [SerializeField, Min(0.1f)] private float hideRange = 12f;
    [SerializeField, Min(0f)] private float entranceDuration = 0.45f;
    [SerializeField, Min(0f)] private float disappearDuration = 0.7f;
    [SerializeField, Min(0.1f)] private float reverseEntranceSpeedMultiplier = 1.55f;
    [SerializeField, Min(0f)] private float revealConfirmSeconds = 0.05f;
    [SerializeField, Min(0f)] private float hideConfirmSeconds = 0.25f;
    [SerializeField] private string entranceAnimation = "death_reverse";
    [SerializeField] private string idleAnimation = "Idle/Move";
    [SerializeField] private string disappearAnimation = "death";
    [SerializeField] private bool startHidden = true;

    [Header("NPC \u529f\u80fd")]
    [SerializeField] private string npcName = "\u4F8D\u706B\u5DEB\u5973";
    [SerializeField] private string playerSpeakerName = "\u4f60";
    [SerializeField] private LargeSkullNpcFunction enabledFunctions = LargeSkullNpcFunction.Dialogue;
    [SerializeField] private string dialogueOptionText = "\u5bf9\u8bdd";
    [SerializeField] private string upgradeOptionText = "\u796d\u793c";
    [SerializeField] private string returnFireOptionText = "\u5f52\u706b";
    [SerializeField] private string flaskUpgradeOptionText = "\u51dd\u9732";
    [SerializeField] private DialogueLine[] conversationLines;
    [SerializeField, Min(0f)] private float dialogueDisplaySeconds = 4f;
    [SerializeField] private string returnFireCompleteMessage = "\u5f52\u706b\u5b8c\u6210";

    [Header("\u5f52\u706b\u9ed1\u5c4f")]
    [SerializeField] private bool useReturnFireFade = true;
    [SerializeField, Min(0f)] private float returnFireFadeToBlackDuration = 0.65f;
    [SerializeField, Min(0f)] private float returnFireBlackHoldDuration = 0.18f;
    [SerializeField, Min(0f)] private float returnFireFadeFromBlackDuration = 0.75f;
    [SerializeField] private bool pauseWorldDuringReturnFireFade = true;

    [Header("\u4e8b\u4ef6")]
    [SerializeField] private UnityEvent onDialogue;
    [SerializeField] private UnityEvent onUpgrade;
    [SerializeField] private UnityEvent onReturnFire;
    [SerializeField] private UnityEvent onFlaskUpgrade;

    [Header("Audio")]
    [SerializeField] private AudioEventPlayer audioEvents;
    [SerializeField] private string appearCueName = "NpcAppear";
    [SerializeField] private string disappearCueName = "NpcDisappear";
    [SerializeField] private AudioClip appearClip;
    [SerializeField] private AudioClip disappearClip;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float audioSpatialBlend = 0f;

    private Animator animator;
    private Renderer[] renderers;
    private Player player;
    private Coroutine visibilityRoutine;
    private bool visible;
    private bool readyToInteract;
    private LargeSkullNpcVisibilityState visibilityState;
    private float revealConfirmTimer;
    private float hideConfirmTimer;
    private bool hideInterruptedByReveal;
    private bool returnFireInProgress;
    private bool returnFirePausedWorld;
    private float returnFirePreviousTimeScale = 1f;

    public override string InteractionPrompt => "\u6309 E \u5bf9\u8bdd";

    public override bool CanInteract(Player interactingPlayer)
    {
        return base.CanInteract(interactingPlayer) && visible && readyToInteract;
    }

    public void ForceVisibleForCutscene(bool allowInteraction = false)
    {
        if (visibilityRoutine != null)
        {
            StopCoroutine(visibilityRoutine);
            visibilityRoutine = null;
        }

        visible = true;
        readyToInteract = allowInteraction;
        visibilityState = LargeSkullNpcVisibilityState.Visible;
        revealConfirmTimer = 0f;
        hideConfirmTimer = 0f;
        hideInterruptedByReveal = false;
        SetVisualsVisible(true);
        PlayIdleAnimation();
    }

    protected override void Awake()
    {
        base.Awake();

        animator = GetComponentInChildren<Animator>(true);
        renderers = GetComponentsInChildren<Renderer>(true);

        if (audioEvents == null)
            audioEvents = GetComponent<AudioEventPlayer>();

        if (audioEvents == null)
            audioEvents = GetComponentInChildren<AudioEventPlayer>();

        if (hideRange < revealRange)
            hideRange = revealRange + 0.5f;

        if (startHidden)
        {
            visible = false;
            readyToInteract = false;
            visibilityState = LargeSkullNpcVisibilityState.Hidden;
            SetVisualsVisible(false);
        }
        else
        {
            visible = true;
            readyToInteract = true;
            visibilityState = LargeSkullNpcVisibilityState.Visible;
            SetVisualsVisible(true);
            PlayIdleAnimation();
        }
    }

    private void Update()
    {
        Player currentPlayer = ResolvePlayer();

        if (currentPlayer == null)
            return;

        UpdateVisibilityTimers(currentPlayer);
        UpdateVisibilityState();
    }

    private void OnDisable()
    {
        RestoreReturnFireTimeScale();
    }

    private void UpdateVisibilityTimers(Player currentPlayer)
    {
        float sqrDistance = ((Vector2)currentPlayer.transform.position - (Vector2)transform.position).sqrMagnitude;
        float deltaTime = Time.deltaTime;

        if (sqrDistance <= revealRange * revealRange)
            revealConfirmTimer += deltaTime;
        else
            revealConfirmTimer = 0f;

        if (sqrDistance >= hideRange * hideRange)
            hideConfirmTimer += deltaTime;
        else
            hideConfirmTimer = 0f;
    }

    private bool IsPlayerInsideRevealRange(Player currentPlayer)
    {
        if (currentPlayer == null)
            return false;

        float sqrDistance = ((Vector2)currentPlayer.transform.position - (Vector2)transform.position).sqrMagnitude;
        return sqrDistance <= revealRange * revealRange;
    }

    private void UpdateVisibilityState()
    {
        bool revealConfirmed = revealConfirmSeconds <= 0f
            ? revealConfirmTimer > 0f
            : revealConfirmTimer >= revealConfirmSeconds;
        bool hideConfirmed = hideConfirmSeconds <= 0f
            ? hideConfirmTimer > 0f
            : hideConfirmTimer >= hideConfirmSeconds;

        if ((visibilityState == LargeSkullNpcVisibilityState.Hidden ||
             visibilityState == LargeSkullNpcVisibilityState.Hiding) && revealConfirmed)
        {
            ShowNpc();
            return;
        }

        if ((visibilityState == LargeSkullNpcVisibilityState.Visible ||
             visibilityState == LargeSkullNpcVisibilityState.Showing) && hideConfirmed)
        {
            HideNpc();
        }
    }

    protected override void OnInteract(Player interactingPlayer)
    {
        List<UI_NpcInteractionMenu.MenuOption> options = BuildMenuOptions(interactingPlayer);

        if (options.Count <= 0)
            return;

        UI_NpcInteractionMenu menu = UI_NpcInteractionMenu.GetOrCreate();

        if (menu == null)
        {
            Debug.LogWarning(npcName + ": NPC option UI was not found.", this);
            return;
        }

        menu.Show(npcName, options, transform);
    }

    private void ShowNpc()
    {
        if (visibilityState == LargeSkullNpcVisibilityState.Visible ||
            visibilityState == LargeSkullNpcVisibilityState.Showing)
            return;

        float startNormalized = visibilityState == LargeSkullNpcVisibilityState.Hiding
            ? GetDisappearAnimationNormalized(1f)
            : 1f;

        if (visibilityRoutine != null)
            StopCoroutine(visibilityRoutine);

        hideConfirmTimer = 0f;
        visibilityState = LargeSkullNpcVisibilityState.Showing;
        PlayNpcSound(appearCueName, appearClip);
        visibilityRoutine = StartCoroutine(ShowRoutine(startNormalized));
    }

    private IEnumerator ShowRoutine(float startNormalized)
    {
        visible = true;
        readyToInteract = false;
        visibilityState = LargeSkullNpcVisibilityState.Showing;
        SetVisualsVisible(true);

        if (IsReversedEntranceAnimation(entranceAnimation))
        {
            yield return PlayAnimationReversed(disappearAnimation, disappearDuration, startNormalized);
        }
        else
        {
            PlayAnimation(entranceAnimation);

            float waitTime = GetAnimationLength(entranceAnimation);

            if (waitTime <= 0f)
                waitTime = entranceDuration;

            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);
        }

        PlayIdleAnimation();
        readyToInteract = true;
        visibilityState = LargeSkullNpcVisibilityState.Visible;
        visibilityRoutine = null;
    }

    private void HideNpc()
    {
        if (visibilityState == LargeSkullNpcVisibilityState.Hidden ||
            visibilityState == LargeSkullNpcVisibilityState.Hiding)
            return;

        float startNormalized = visibilityState == LargeSkullNpcVisibilityState.Showing
            ? GetDisappearAnimationNormalized(0f)
            : 0f;

        if (visibilityRoutine != null)
            StopCoroutine(visibilityRoutine);

        UI_NpcInteractionMenu.Instance?.Hide();
        UI_DialogueConversationSystem.Instance?.Hide();
        revealConfirmTimer = 0f;
        visibilityState = LargeSkullNpcVisibilityState.Hiding;
        PlayNpcSound(disappearCueName, disappearClip);
        visibilityRoutine = StartCoroutine(HideRoutine(startNormalized));
    }

    private List<UI_NpcInteractionMenu.MenuOption> BuildMenuOptions(Player interactingPlayer)
    {
        List<UI_NpcInteractionMenu.MenuOption> options = new List<UI_NpcInteractionMenu.MenuOption>();

        if (enabledFunctions.HasFlag(LargeSkullNpcFunction.ReturnFire))
            options.Add(new UI_NpcInteractionMenu.MenuOption(GetReturnFireOptionText(), () => ReturnFireAtNpc(interactingPlayer)));

        if (enabledFunctions.HasFlag(LargeSkullNpcFunction.Dialogue))
            options.Add(new UI_NpcInteractionMenu.MenuOption(GetOptionText(dialogueOptionText, "\u5bf9\u8bdd"), StartDialogue));

        if (enabledFunctions.HasFlag(LargeSkullNpcFunction.Upgrade))
            options.Add(new UI_NpcInteractionMenu.MenuOption(GetOptionText(upgradeOptionText, "\u796d\u793c"), StartUpgrade));

        if (enabledFunctions.HasFlag(LargeSkullNpcFunction.FlaskUpgrade))
            options.Add(new UI_NpcInteractionMenu.MenuOption(GetFlaskUpgradeOptionText(), StartFlaskUpgrade));

        return options;
    }

    private IEnumerator HideRoutine(float startNormalized)
    {
        readyToInteract = false;
        visible = true;
        visibilityState = LargeSkullNpcVisibilityState.Hiding;
        SetVisualsVisible(true);
        hideInterruptedByReveal = false;
        yield return PlayAnimationForward(disappearAnimation, disappearDuration, startNormalized, true);

        if (hideInterruptedByReveal)
        {
            visibilityState = LargeSkullNpcVisibilityState.Showing;
            visibilityRoutine = StartCoroutine(ShowRoutine(GetDisappearAnimationNormalized(1f)));
            yield break;
        }

        visible = false;
        SetVisualsVisible(false);
        FreezeAnimator();
        visibilityState = LargeSkullNpcVisibilityState.Hidden;
        visibilityRoutine = null;
    }

    private void StartDialogue()
    {
        DialogueLine[] sequence = GetConversationLines();
        UI_DialogueConversationSystem panel = UI_DialogueConversationSystem.GetOrCreate();

        if (panel != null)
            panel.ShowConversation(npcName, playerSpeakerName, sequence);
        else
            Debug.LogWarning(npcName + ": NPC option UI was not found.", this);

        onDialogue?.Invoke();
    }

    private void ReturnFireAtNpc(Player interactingPlayer)
    {
        if (returnFireInProgress)
            return;

        StartCoroutine(ReturnFireRoutine(interactingPlayer));
    }

    private IEnumerator ReturnFireRoutine(Player interactingPlayer)
    {
        returnFireInProgress = true;
        readyToInteract = false;

        UI_NpcInteractionMenu.Instance?.Hide();
        UI_DialogueConversationSystem.Instance?.Hide();

        if (pauseWorldDuringReturnFireFade)
        {
            returnFirePreviousTimeScale = Time.timeScale;
            returnFirePausedWorld = true;
            Time.timeScale = 0f;
        }

        UI_ScreenFadeTransition fadeTransition = useReturnFireFade ? UI_ScreenFadeTransition.Instance : null;

        if (fadeTransition != null)
            yield return fadeTransition.FadeTo(1f, returnFireFadeToBlackDuration);

        Player targetPlayer = interactingPlayer != null ? interactingPlayer : ResolvePlayer();
        WorldRestManager.GetOrCreate().Rest(targetPlayer);
        onReturnFire?.Invoke();

        if (returnFireBlackHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(returnFireBlackHoldDuration);

        if (fadeTransition != null)
            yield return fadeTransition.FadeTo(0f, returnFireFadeFromBlackDuration);

        RestoreReturnFireTimeScale();

        readyToInteract = visible && visibilityState == LargeSkullNpcVisibilityState.Visible;
        returnFireInProgress = false;

        Debug.Log(npcName + " return fire completed.", this);
        ShowNpcMessage(GetOptionText(returnFireCompleteMessage, "\u5f52\u706b\u5b8c\u6210"));
    }

    private void RestoreReturnFireTimeScale()
    {
        if (!returnFirePausedWorld)
            return;

        Time.timeScale = returnFirePreviousTimeScale;
        returnFirePausedWorld = false;
    }

    private void StartFlaskUpgrade()
    {
        MainFlaskUpgradeMaterialSystem upgradePanel = FindObjectOfType<MainFlaskUpgradeMaterialSystem>(true);

        if (upgradePanel == null)
        {
            Transform designedPanel = UI_NpcPanelUtility.FindNpcPanelChild(
                "FlaskUp_UI",
                "FlaskUp",
                "\u51DD\u9732_UI",
                "\u51DD\u9732",
                "\u9732\u6EF4_UI",
                "\u9732\u6EF4",
                "\u836F\u74F6\u5347\u7EA7\u6750\u6599\u7CFB\u7EDF",
                "FlaskUpgrade_UI",
                "Flask Upgrade_UI");

            if (designedPanel == null)
            {
                designedPanel = FindSceneTransformByName(
                    "FlaskUp_UI",
                    "FlaskUp",
                    "\u51DD\u9732_UI",
                    "\u51DD\u9732",
                    "\u9732\u6EF4_UI",
                    "\u9732\u6EF4",
                    "\u836F\u74F6\u5347\u7EA7\u6750\u6599\u7CFB\u7EDF",
                    "FlaskUpgrade_UI",
                    "Flask Upgrade_UI");
            }

            if (designedPanel != null)
                upgradePanel = designedPanel.GetComponent<MainFlaskUpgradeMaterialSystem>()
                    ?? designedPanel.gameObject.AddComponent<MainFlaskUpgradeMaterialSystem>();
        }

        if (upgradePanel != null)
        {
            upgradePanel.Open();
        }
        else
        {
            Debug.LogWarning(npcName + ": 未找到凝露升级界面或药瓶升级材料系统。", this);
        }

        onFlaskUpgrade?.Invoke();
    }

    private DialogueLine[] GetConversationLines()
    {
        if (conversationLines != null && conversationLines.Length > 0)
        {
            List<DialogueLine> validLines = new List<DialogueLine>();

            for (int i = 0; i < conversationLines.Length; i++)
            {
                DialogueLine line = conversationLines[i];

                if (line != null && !string.IsNullOrWhiteSpace(line.text))
                    validLines.Add(new DialogueLine(line.speaker, line.text.Trim()));
            }

            if (validLines.Count > 0)
                return validLines.ToArray();
        }

        return new[] { new DialogueLine(DialogueSpeaker.Npc, "...") };
    }

    private string GetReturnFireOptionText()
    {
        string optionText = !string.IsNullOrWhiteSpace(returnFireOptionText)
            ? returnFireOptionText
            : "\u5f52\u706b";

        return optionText;
    }

    private string GetOptionText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private string GetFlaskUpgradeOptionText()
    {
        string value = GetOptionText(flaskUpgradeOptionText, "\u51dd\u9732").Trim();

        if (string.Equals(value, "\u9732\u6ef4", StringComparison.OrdinalIgnoreCase))
            return "\u51dd\u9732";

        return value;
    }

    private Transform FindSceneTransformByName(params string[] names)
    {
        if (names == null || names.Length <= 0)
            return null;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];

            if (target == null || !target.gameObject.scene.IsValid())
                continue;

            for (int n = 0; n < names.Length; n++)
            {
                if (!string.IsNullOrWhiteSpace(names[n]) &&
                    string.Equals(target.name, names[n], StringComparison.OrdinalIgnoreCase))
                    return target;
            }
        }

        return null;
    }

    private void StartUpgrade()
    {
        UI ui = FindObjectOfType<UI>(true);

        if (ui != null)
        {
            ui.OpenLevelUp();
        }
        else
        {
            Debug.LogWarning(npcName + ": NPC option UI was not found.", this);
        }

        onUpgrade?.Invoke();
    }

    private void ShowNpcMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        UI_DialogueConversationSystem panel = UI_DialogueConversationSystem.GetOrCreate();

        if (panel != null)
            panel.ShowNpcMessage(npcName, message, dialogueDisplaySeconds);
        else
            Debug.LogWarning(npcName + ": NPC option UI was not found.", this);
    }

    private Player ResolvePlayer()
    {
        if (player != null)
            return player;

        if (PlayerManager.instance != null)
            player = PlayerManager.instance.player;

        if (player == null)
            player = FindObjectOfType<Player>();

        return player;
    }

    private void SetVisualsVisible(bool value)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = value;
        }
    }

    private IEnumerator PlayAnimationReversed(string stateName, float targetDuration, float startNormalized = 1f)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            yield break;

        float duration = targetDuration;

        if (duration <= 0f)
            duration = GetAnimationLength(stateName);

        if (duration <= 0f)
        {
            PlayAnimation(stateName, 0f);
            yield break;
        }

        startNormalized = Mathf.Clamp01(startNormalized);

        if (startNormalized <= 0.01f)
        {
            PlayAnimation(stateName, 0f, false);
            animator.Update(0f);
            yield break;
        }

        float reverseDuration = duration * startNormalized;
        float elapsedTime = 0f;
        animator.speed = 0f;

        while (elapsedTime < reverseDuration)
        {
            float progress = Mathf.Clamp01(elapsedTime / reverseDuration);
            float normalizedTime = Mathf.Lerp(startNormalized, 0f, progress);
            PlayAnimation(stateName, normalizedTime, false);
            animator.Update(0f);

            elapsedTime += Time.deltaTime * reverseEntranceSpeedMultiplier;
            yield return null;
        }

        PlayAnimation(stateName, 0f, false);
        animator.Update(0f);
    }

    private IEnumerator PlayAnimationForward(string stateName, float targetDuration, float startNormalized = 0f, bool interruptWhenPlayerReturns = false)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            yield break;

        float duration = targetDuration;

        if (duration <= 0f)
            duration = GetAnimationLength(stateName);

        if (duration <= 0f)
        {
            PlayAnimation(stateName, 1f);
            yield break;
        }

        startNormalized = Mathf.Clamp01(startNormalized);

        if (startNormalized >= 0.99f)
        {
            PlayAnimation(stateName, 1f);
            yield break;
        }

        float forwardDuration = duration * (1f - startNormalized);
        float elapsedTime = 0f;
        animator.speed = 0f;

        while (elapsedTime < forwardDuration)
        {
            if (interruptWhenPlayerReturns && IsPlayerInsideRevealRange(ResolvePlayer()))
            {
                hideInterruptedByReveal = true;
                yield break;
            }

            float progress = Mathf.Clamp01(elapsedTime / forwardDuration);
            float normalizedTime = Mathf.Lerp(startNormalized, 1f, progress);
            PlayAnimation(stateName, normalizedTime, false);
            animator.Update(0f);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (interruptWhenPlayerReturns && IsPlayerInsideRevealRange(ResolvePlayer()))
        {
            hideInterruptedByReveal = true;
            yield break;
        }

        PlayAnimation(stateName, 1f, false);
        animator.Update(0f);
    }

    private float GetDisappearAnimationNormalized(float fallback)
    {
        if (animator == null || string.IsNullOrWhiteSpace(disappearAnimation))
            return Mathf.Clamp01(fallback);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!IsCurrentAnimationState(stateInfo, disappearAnimation))
            return Mathf.Clamp01(fallback);

        return Mathf.Clamp01(stateInfo.normalizedTime);
    }

    private bool IsCurrentAnimationState(AnimatorStateInfo stateInfo, string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
            return false;

        string safeStateName = GetSafeAnimationStateName(stateName);
        string matchingClipName = FindAnimationClipName(stateName);
        string safeClipName = string.IsNullOrWhiteSpace(matchingClipName)
            ? null
            : GetSafeAnimationStateName(matchingClipName);

        return stateInfo.IsName(stateName) ||
               stateInfo.IsName("Base Layer." + stateName) ||
               (!string.IsNullOrWhiteSpace(safeStateName) && stateInfo.IsName(safeStateName)) ||
               (!string.IsNullOrWhiteSpace(safeStateName) && stateInfo.IsName("Base Layer." + safeStateName)) ||
               (!string.IsNullOrWhiteSpace(matchingClipName) && stateInfo.IsName(matchingClipName)) ||
               (!string.IsNullOrWhiteSpace(matchingClipName) && stateInfo.IsName("Base Layer." + matchingClipName)) ||
               (!string.IsNullOrWhiteSpace(safeClipName) && stateInfo.IsName(safeClipName)) ||
               (!string.IsNullOrWhiteSpace(safeClipName) && stateInfo.IsName("Base Layer." + safeClipName));
    }

    private bool PlayAnimation(string stateName, float normalizedTime = 0f, bool resetAnimatorSpeed = true)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        if (resetAnimatorSpeed)
            animator.speed = 1f;

        if (TryPlayAnimationState(stateName, normalizedTime))
            return true;

        string titleCaseState = char.ToUpperInvariant(stateName[0]) + stateName.Substring(1);

        if (TryPlayAnimationState(titleCaseState, normalizedTime))
            return true;

        string safeStateName = GetSafeAnimationStateName(stateName);

        if (!string.IsNullOrWhiteSpace(safeStateName) && TryPlayAnimationState(safeStateName, normalizedTime))
            return true;

        string matchingClipName = FindAnimationClipName(stateName);

        if (!string.IsNullOrWhiteSpace(matchingClipName) && TryPlayAnimationState(matchingClipName, normalizedTime))
            return true;

        if (!string.IsNullOrWhiteSpace(matchingClipName) && TryPlayAnimationState(GetSafeAnimationStateName(matchingClipName), normalizedTime))
            return true;

        return false;
    }

    private bool TryPlayAnimationState(string stateName, float normalizedTime)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        if (animator.HasState(0, Animator.StringToHash(stateName)))
        {
            animator.Play(stateName, 0, normalizedTime);
            return true;
        }

        string baseLayerStateName = "Base Layer." + stateName;

        if (animator.HasState(0, Animator.StringToHash(baseLayerStateName)))
        {
            animator.Play(baseLayerStateName, 0, normalizedTime);
            return true;
        }

        return false;
    }

    private void PlayNpcSound(string cueName, AudioClip clip)
    {
        AudioPlaybackUtility.PlayCueOrClip(audioEvents, cueName, clip, transform, audioVolume, 1f, audioSpatialBlend);
    }

    private string FindAnimationClipName(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            return null;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        if (clips == null)
            return null;

        string normalizedRequested = NormalizeAnimationName(stateName);

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
                continue;

            if (NormalizeAnimationName(clips[i].name) == normalizedRequested)
                return clips[i].name;
        }

        if (normalizedRequested.Contains("idle") || normalizedRequested.Contains("move"))
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                string normalizedClipName = NormalizeAnimationName(clips[i].name);

                if (normalizedClipName.Contains("idle") && normalizedClipName.Contains("move"))
                    return clips[i].name;
            }
        }

        return null;
    }

    private float GetAnimationLength(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            return 0f;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        if (clips == null)
            return 0f;

        string normalizedRequested = NormalizeAnimationName(stateName);

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
                continue;

            if (NormalizeAnimationName(clips[i].name) == normalizedRequested)
                return clips[i].length;
        }

        return 0f;
    }

    private static bool IsReversedEntranceAnimation(string stateName)
    {
        return NormalizeAnimationName(stateName).Contains("deathreverse");
    }

    private static string NormalizeAnimationName(string animationName)
    {
        return animationName.Replace("/", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }

    private static string GetSafeAnimationStateName(string animationName)
    {
        return animationName.Replace("/", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty);
    }

    private void PlayIdleAnimation()
    {
        if (!PlayAnimation(idleAnimation))
            FreezeAnimator();
    }

    private void FreezeAnimator()
    {
        if (animator != null)
            animator.speed = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, revealRange);

        Gizmos.color = new Color(0.8f, 0.15f, 0.05f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, hideRange);
    }
}
