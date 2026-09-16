using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class DemonBossDefeatCollapseDirector : MonoBehaviour, IInteractable
{
    private static readonly Dictionary<int, DemonBossDefeatCollapseDirector> primaryDirectorsByBossId =
        new Dictionary<int, DemonBossDefeatCollapseDirector>();

    [Header("Boss")]
    [SerializeField] private Enemy_DemonBoss demonBoss;
    [SerializeField] private BossPhaseTransitionDirector cameraDirector;

    [Header("Player")]
    [SerializeField] private bool lockPlayerDuringSequence = true;
    [SerializeField] private Vector2 maidenSpawnOffset = new Vector2(1.4f, 0f);

    [Header("Collapse Timing")]
    [FormerlySerializedAs("delayAfterBossDisappear")]
    [SerializeField, Min(0f)] private float lootPickupDuration = 5f;
    [SerializeField, Min(0f)] private float shakeBeforeMaidenAppearDuration = 3f;

    [Header("Camera Shake")]
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.16f;
    [SerializeField, Min(0.1f)] private float shakeFrequency = 18f;

    [Header("Maiden")]
    [SerializeField] private GameObject maidenPrefab;
    [SerializeField] private GameObject maidenObject;
    [SerializeField] private Animator maidenAnimator;
    [SerializeField] private string maidenObjectNameToken = "\u4F8D\u706B\u5DEB\u5973";
    [SerializeField] private string maidenEntranceAnimation = "death_reverse";
    [SerializeField] private string maidenDisappearAnimation = "death";
    [SerializeField] private string maidenIdleAnimation = "Idle/Move";
    [SerializeField, Min(0f)] private float maidenEntranceFallbackDuration = 0.45f;
    [SerializeField] private bool disableMaidenInteractableDuringSequence = true;
    [SerializeField] private bool alwaysSpawnFreshMaidenFromPrefab = true;

    [Header("Dialogue")]
    [SerializeField] private string npcName = "\u4F8D\u706B\u5DEB\u5973";
    [SerializeField] private string playerSpeakerName = "\u4F60";
    [SerializeField] private DialogueLine[] postDefeatDialogueLines;

    [Header("Teleport")]
    [SerializeField] private string teleportPrompt = "\u6309 E \u4F20\u9001";
    [SerializeField] private KeyCode teleportKey = KeyCode.E;
    [SerializeField] private string endSceneName = "end";
    [SerializeField, Min(0f)] private float fadeToBlackDuration = 0.75f;
    [SerializeField, Min(0f)] private float fadeFromBlackDuration = 0.75f;

    [Header("Earthquake BGM")]
    [SerializeField] private BGMPlayer earthquakeBgmPlayer;
    [SerializeField] private AudioClip earthquakeBgmClip;
    [SerializeField] private bool earthquakeBgmLoop = true;
    [SerializeField, Range(0f, 1f)] private float earthquakeBgmVolume = 1f;

    private Coroutine collapseRoutine;
    private Player lockedPlayer;
    private Player cachedPlayer;
    private UI_InteractionPrompt promptUi;
    private bool waitingForTeleportInput;
    private bool sequenceStarted;
    private bool earthquakeBgmStarted;
    private LargeSkullNpcInteractable maidenInteractable;
    private bool registeredAsPrimary;
    private int registeredBossId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPrimaryDirectors() => primaryDirectorsByBossId.Clear();

    public string InteractionPrompt => teleportPrompt;
    public Transform InteractionTransform => ResolvePromptAnchor();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RegisterAsPrimaryForBoss();
        SubscribeToBoss();
    }

    private void OnDisable()
    {
        if (collapseRoutine != null)
        {
            StopCoroutine(collapseRoutine);
            collapseRoutine = null;
        }
        UnsubscribeFromBoss();
        UnregisterPrimaryDirector();
        HideTeleportPrompt();
        StopCollapseShake();
        StopEarthquakeBgm();
        UnlockPlayer();
    }

    private void Update()
    {
        if (!waitingForTeleportInput)
            return;

        if (Input.GetKeyDown(teleportKey))
            StartTeleportToEndScene();
    }

    public void Configure(Enemy_DemonBoss boss, BossPhaseTransitionDirector director)
    {
        bool bossChanged = demonBoss != boss;

        if (bossChanged)
        {
            UnsubscribeFromBoss();
            demonBoss = boss;
        }

        BossPhaseTransitionDirector persistentDirector = ResolvePersistentCameraDirector(director);

        if (!bossChanged && cameraDirector == persistentDirector)
            return;

        cameraDirector = persistentDirector;
        ResolveReferences();
        RegisterAsPrimaryForBoss();
        SubscribeToBoss();
    }

    public bool CanInteract(Player player)
    {
        _ = player;
        return waitingForTeleportInput && isActiveAndEnabled;
    }

    public void Interact(Player player)
    {
        _ = player;

        if (waitingForTeleportInput)
            StartTeleportToEndScene();
    }

    private void ResolveReferences()
    {
        if (demonBoss == null)
            demonBoss = FindObjectOfType<Enemy_DemonBoss>(true);

        cameraDirector = ResolvePersistentCameraDirector(cameraDirector, true);

        if (maidenObject != null && !IsValidMaidenObject(maidenObject))
        {
            maidenObject = null;
            maidenAnimator = null;
            maidenInteractable = null;
        }

        if (maidenObject == null && maidenPrefab == null)
            maidenObject = FindMaidenObject();

        if (maidenAnimator == null && maidenObject != null)
            maidenAnimator = maidenObject.GetComponentInChildren<Animator>(true);

        if (maidenInteractable == null && maidenObject != null)
            maidenInteractable = maidenObject.GetComponentInChildren<LargeSkullNpcInteractable>(true);
    }

    private void SubscribeToBoss()
    {
        if (demonBoss == null)
            return;

        if (!registeredAsPrimary)
            RegisterAsPrimaryForBoss();

        if (!IsPrimaryForBoss(demonBoss))
            return;

        demonBoss.OnDeathStarted -= HandleDemonBossDeathStarted;
        demonBoss.OnDeathStarted += HandleDemonBossDeathStarted;
    }

    private void UnsubscribeFromBoss()
    {
        if (demonBoss == null)
            return;

        demonBoss.OnDeathStarted -= HandleDemonBossDeathStarted;
    }

    private void HandleDemonBossDeathStarted(Enemy_DemonBoss defeatedBoss)
    {
        if (defeatedBoss == null)
            return;

        if (demonBoss == null)
        {
            demonBoss = defeatedBoss;
            RegisterAsPrimaryForBoss();
            SubscribeToBoss();
        }

        if (defeatedBoss != demonBoss || !IsPrimaryForBoss(defeatedBoss))
            return;

        if (sequenceStarted)
            return;

        sequenceStarted = true;
        collapseRoutine = StartCoroutine(CollapseRoutine(defeatedBoss));
    }

    private IEnumerator CollapseRoutine(Enemy_DemonBoss defeatedBoss)
    {
        // Count playable time so pausing does not consume the loot pickup window.
        if (lootPickupDuration > 0f)
            yield return new WaitForSeconds(lootPickupDuration);

        LockPlayer();
        StartEarthquakeBgm();
        StartCollapseShake();

        if (shakeBeforeMaidenAppearDuration > 0f)
            yield return new WaitForSecondsRealtime(shakeBeforeMaidenAppearDuration);

        yield return ShowMaidenNearPlayer();
        yield return PlayPostDefeatDialogue();

        ShowTeleportPrompt();
        collapseRoutine = null;
    }

    private void LockPlayer()
    {
        if (!lockPlayerDuringSequence)
            return;

        lockedPlayer = ResolvePlayer();
        if (lockedPlayer != null)
            lockedPlayer.BeginCutsceneControlLock();
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
        if (cachedPlayer != null)
            return cachedPlayer;

        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            cachedPlayer = PlayerManager.instance.player;

        if (cachedPlayer == null)
            cachedPlayer = FindObjectOfType<Player>();

        return cachedPlayer;
    }

    private void StartEarthquakeBgm()
    {
        if (earthquakeBgmPlayer != null && earthquakeBgmPlayer.bgmClip != null)
        {
            earthquakeBgmPlayer.PlayConfiguredBGM();
            earthquakeBgmStarted = true;
            return;
        }

        if (earthquakeBgmClip != null)
        {
            AudioManager.GetOrCreateInstance().PlayBGM(earthquakeBgmClip, earthquakeBgmLoop, earthquakeBgmVolume);
            earthquakeBgmStarted = true;
        }
    }

    private void StopEarthquakeBgm()
    {
        if (!earthquakeBgmStarted)
            return;

        AudioManager.GetOrCreateInstance().StopBGM();
        earthquakeBgmStarted = false;
    }

    private void StartCollapseShake()
    {
        cameraDirector = ResolvePersistentCameraDirector(cameraDirector, true);

        if (cameraDirector != null)
            cameraDirector.StartCameraShake(shakeAmplitude, shakeFrequency);
    }

    private void StopCollapseShake()
    {
        cameraDirector = ResolvePersistentCameraDirector(cameraDirector, false);
        if (cameraDirector != null)
            cameraDirector.StopCameraShake();
    }

    private BossPhaseTransitionDirector ResolvePersistentCameraDirector(BossPhaseTransitionDirector suggestedDirector, bool allowCreate = true)
    {
        if (IsPersistentCameraDirector(suggestedDirector))
            return suggestedDirector;

        if (cameraDirector != null && IsPersistentCameraDirector(cameraDirector))
            return cameraDirector;

        if (!allowCreate)
            return null;

        BossPhaseTransitionDirector localDirector = GetComponent<BossPhaseTransitionDirector>();

        if (localDirector == null)
            localDirector = gameObject.AddComponent<BossPhaseTransitionDirector>();

        return localDirector;
    }

    private bool IsPersistentCameraDirector(BossPhaseTransitionDirector director)
    {
        if (director == null)
            return false;

        if (demonBoss == null)
            return true;

        return !director.transform.IsChildOf(demonBoss.transform);
    }

    private IEnumerator ShowMaidenNearPlayer()
    {
        ResolveReferences();

        Player player = ResolvePlayer();

        if (player == null)
            yield break;

        if (alwaysSpawnFreshMaidenFromPrefab && maidenPrefab != null)
            SpawnFreshMaidenObject();

        EnsureMaidenObject();

        if (maidenObject == null)
            maidenObject = new GameObject("PostDefeatMaidenAnchor");

        HideOtherMaidenObjects(maidenObject);

        maidenObject.SetActive(true);
        maidenObject.transform.position = player.transform.position + new Vector3(maidenSpawnOffset.x, maidenSpawnOffset.y, 0f);
        maidenObject.transform.rotation = Quaternion.identity;

        if (maidenObject.transform.localScale == Vector3.zero)
            maidenObject.transform.localScale = Vector3.one;

        SetMaidenRenderersVisible(true);

        if (maidenInteractable == null)
            maidenInteractable = maidenObject.GetComponentInChildren<LargeSkullNpcInteractable>(true);

        maidenInteractable?.ForceVisibleForCutscene(false);

        if (maidenAnimator == null)
            maidenAnimator = maidenObject.GetComponentInChildren<Animator>(true);

        if (maidenAnimator == null)
            yield break;

        if (IsReversedEntranceAnimation(maidenEntranceAnimation))
            yield return PlayMaidenAnimationReversed(maidenDisappearAnimation, maidenEntranceFallbackDuration);
        else
            yield return PlayMaidenAnimationForward(maidenEntranceAnimation, maidenEntranceFallbackDuration);

        PlayMaidenIdle();
        maidenInteractable?.ForceVisibleForCutscene(false);
        DisableMaidenInteractable();
    }

    private void EnsureMaidenObject()
    {
        if (maidenObject != null)
            return;

        if (maidenPrefab == null)
            return;

        SpawnFreshMaidenObject();
    }

    private void SpawnFreshMaidenObject()
    {
        if (maidenPrefab == null)
            return;

        maidenObject = Instantiate(maidenPrefab);
        maidenObject.name = maidenPrefab.name;
        maidenAnimator = maidenObject.GetComponentInChildren<Animator>(true);
        maidenInteractable = maidenObject.GetComponentInChildren<LargeSkullNpcInteractable>(true);
    }

    private void HideOtherMaidenObjects(GameObject objectToKeep)
    {
        LargeSkullNpcInteractable[] candidates = Resources.FindObjectsOfTypeAll<LargeSkullNpcInteractable>();

        if (candidates == null)
            return;

        HashSet<GameObject> hiddenObjects = new HashSet<GameObject>();

        for (int i = 0; i < candidates.Length; i++)
        {
            LargeSkullNpcInteractable candidate = candidates[i];

            if (candidate == null || !candidate.gameObject.scene.IsValid())
                continue;

            GameObject candidateObject = candidate.gameObject;

            if (objectToKeep != null &&
                (candidateObject == objectToKeep || candidateObject.transform.IsChildOf(objectToKeep.transform)))
            {
                continue;
            }

            if (!hiddenObjects.Add(candidateObject))
                continue;

            candidateObject.SetActive(false);
        }
    }

    private IEnumerator PlayPostDefeatDialogue()
    {
        UI_DialogueConversationSystem panel = UI_DialogueConversationSystem.GetOrCreate();

        if (panel == null)
            yield break;

        panel.ShowConversation(npcName, playerSpeakerName, GetValidDialogueLines());
        yield return null;

        while (UI_DialogueConversationSystem.IsOpen)
            yield return null;
    }

    private List<DialogueLine> GetValidDialogueLines()
    {
        List<DialogueLine> validLines = new List<DialogueLine>();

        if (postDefeatDialogueLines == null)
            return validLines;

        for (int i = 0; i < postDefeatDialogueLines.Length; i++)
        {
            DialogueLine line = postDefeatDialogueLines[i];

            if (line != null && !string.IsNullOrWhiteSpace(line.text))
                validLines.Add(new DialogueLine(line.speaker, line.text.Trim()));
        }

        return validLines;
    }

    private void ShowTeleportPrompt()
    {
        waitingForTeleportInput = true;
        promptUi = UI_InteractionPrompt.GetOrCreate();
        promptUi?.Show(this, teleportKey);
    }

    private void HideTeleportPrompt()
    {
        waitingForTeleportInput = false;

        if (promptUi != null)
            promptUi.Hide();

        promptUi = null;
    }

    private void StartTeleportToEndScene()
    {
        if (!waitingForTeleportInput)
            return;

        if (string.IsNullOrWhiteSpace(endSceneName) ||
            !Application.CanStreamedLevelBeLoaded(endSceneName))
        {
            Debug.LogError("Collapse destination is not enabled in Build Settings: " + endSceneName, this);
            return;
        }

        HideTeleportPrompt();
        StopCollapseShake();
        StopEarthquakeBgm();

        UI_ScreenFadeTransition.Instance.LoadSceneWithFade(
            endSceneName,
            fadeToBlackDuration,
            fadeFromBlackDuration);
    }

    private Transform ResolvePromptAnchor()
    {
        if (maidenObject != null)
            return maidenObject.transform;

        Player player = ResolvePlayer();
        return player != null ? player.transform : transform;
    }

    private void SetMaidenRenderersVisible(bool visible)
    {
        if (maidenObject == null)
            return;

        Renderer[] renderers = maidenObject.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = visible;
        }
    }

    private void DisableMaidenInteractable()
    {
        if (!disableMaidenInteractableDuringSequence)
            return;

        if (maidenInteractable == null && maidenObject != null)
            maidenInteractable = maidenObject.GetComponentInChildren<LargeSkullNpcInteractable>(true);

        if (maidenInteractable == null)
            return;

        maidenInteractable.enabled = false;
    }

    private IEnumerator PlayMaidenAnimationForward(string stateName, float fallbackDuration)
    {
        if (!TryPlayMaidenState(stateName, 0f))
            yield break;

        float duration = GetMaidenAnimationLength(stateName);

        if (duration <= 0f)
            duration = fallbackDuration;

        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);
    }

    private IEnumerator PlayMaidenAnimationReversed(string stateName, float fallbackDuration)
    {
        if (maidenAnimator == null || string.IsNullOrWhiteSpace(stateName))
            yield break;

        float duration = GetMaidenAnimationLength(stateName);

        if (duration <= 0f)
            duration = fallbackDuration;

        if (duration <= 0f)
            duration = 0.01f;

        float elapsed = 0f;
        maidenAnimator.speed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            TryPlayMaidenState(stateName, Mathf.Lerp(1f, 0f, t));
            maidenAnimator.Update(0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        TryPlayMaidenState(stateName, 0f);
        maidenAnimator.Update(0f);
        maidenAnimator.speed = 1f;
    }

    private void PlayMaidenIdle()
    {
        SetMaidenRenderersVisible(true);

        if (maidenAnimator != null)
            maidenAnimator.speed = 1f;

        if (!TryPlayMaidenState(maidenIdleAnimation, 0f) && maidenAnimator != null)
            maidenAnimator.speed = 0f;
    }

    private bool TryPlayMaidenState(string stateName, float normalizedTime)
    {
        if (maidenAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        if (TryPlayMaidenStateExact(stateName, normalizedTime))
            return true;

        string titleCaseState = char.ToUpperInvariant(stateName[0]) + stateName.Substring(1);

        if (TryPlayMaidenStateExact(titleCaseState, normalizedTime))
            return true;

        string safeStateName = GetSafeAnimationStateName(stateName);

        if (!string.IsNullOrWhiteSpace(safeStateName) && TryPlayMaidenStateExact(safeStateName, normalizedTime))
            return true;

        string matchingClipName = FindMaidenAnimationClipName(stateName);

        if (!string.IsNullOrWhiteSpace(matchingClipName) && TryPlayMaidenStateExact(matchingClipName, normalizedTime))
            return true;

        if (!string.IsNullOrWhiteSpace(matchingClipName) &&
            TryPlayMaidenStateExact(GetSafeAnimationStateName(matchingClipName), normalizedTime))
            return true;

        return false;
    }

    private bool TryPlayMaidenStateExact(string stateName, float normalizedTime)
    {
        if (maidenAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        int hash = Animator.StringToHash(stateName);

        if (maidenAnimator.HasState(0, hash))
        {
            maidenAnimator.Play(hash, 0, normalizedTime);
            return true;
        }

        string baseLayerStateName = "Base Layer." + stateName;
        int baseLayerHash = Animator.StringToHash(baseLayerStateName);

        if (maidenAnimator.HasState(0, baseLayerHash))
        {
            maidenAnimator.Play(baseLayerHash, 0, normalizedTime);
            return true;
        }

        return false;
    }

    private float GetMaidenAnimationLength(string stateName)
    {
        if (maidenAnimator == null || maidenAnimator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            return 0f;

        AnimationClip[] clips = maidenAnimator.runtimeAnimatorController.animationClips;
        string normalizedStateName = NormalizeAnimationName(stateName);

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];

            if (clip == null)
                continue;

            if (clip.name == stateName || NormalizeAnimationName(clip.name) == normalizedStateName)
                return clip.length;
        }

        return 0f;
    }

    private string FindMaidenAnimationClipName(string stateName)
    {
        if (maidenAnimator == null || maidenAnimator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            return null;

        AnimationClip[] clips = maidenAnimator.runtimeAnimatorController.animationClips;

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

    private GameObject FindMaidenObject()
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];

            if (target == null || !target.gameObject.scene.IsValid())
                continue;

            bool matchesNameToken = !string.IsNullOrWhiteSpace(maidenObjectNameToken) &&
                                    target.name.Contains(maidenObjectNameToken);

            if ((matchesNameToken || target.name.Contains("\u5DEB\u5973")) &&
                IsValidMaidenObject(target.gameObject))
            {
                return target.gameObject;
            }
        }

        return null;
    }

    private bool IsValidMaidenObject(GameObject target)
    {
        if (target == null || target.GetComponentInChildren<LargeSkullNpcInteractable>(true) == null)
            return false;

        bool matchesNameToken = !string.IsNullOrWhiteSpace(maidenObjectNameToken) &&
                                target.name.Contains(maidenObjectNameToken);
        bool matchesMaidenName = target.name.Contains("\u5DEB\u5973");
        bool matchesPrefabName = maidenPrefab != null &&
                                 target.name.StartsWith(maidenPrefab.name, System.StringComparison.Ordinal);

        return matchesNameToken || matchesMaidenName || matchesPrefabName;
    }

    private void RegisterAsPrimaryForBoss()
    {
        UnregisterPrimaryDirector();

        if (demonBoss == null)
        {
            registeredAsPrimary = true;
            return;
        }

        int bossId = demonBoss.GetInstanceID();

        if (primaryDirectorsByBossId.TryGetValue(bossId, out DemonBossDefeatCollapseDirector existing) &&
            existing != null &&
            existing != this)
        {
            if (!ShouldTakePriorityOver(existing))
            {
                registeredAsPrimary = false;
                registeredBossId = 0;
                return;
            }

            existing.UnsubscribeFromBoss();
            existing.registeredAsPrimary = false;
            existing.registeredBossId = 0;
        }

        primaryDirectorsByBossId[bossId] = this;
        registeredAsPrimary = true;
        registeredBossId = bossId;
    }

    private void UnregisterPrimaryDirector()
    {
        if (!registeredAsPrimary || registeredBossId == 0)
        {
            registeredAsPrimary = false;
            registeredBossId = 0;
            return;
        }

        if (primaryDirectorsByBossId.TryGetValue(registeredBossId, out DemonBossDefeatCollapseDirector existing) &&
            existing == this)
        {
            primaryDirectorsByBossId.Remove(registeredBossId);
        }

        registeredAsPrimary = false;
        registeredBossId = 0;
    }

    private bool IsPrimaryForBoss(Enemy_DemonBoss boss)
    {
        if (boss == null)
            return registeredAsPrimary;

        int bossId = boss.GetInstanceID();

        return primaryDirectorsByBossId.TryGetValue(bossId, out DemonBossDefeatCollapseDirector primary) &&
               primary == this;
    }

    private bool ShouldTakePriorityOver(DemonBossDefeatCollapseDirector existing)
    {
        if (existing == null)
            return true;
        if (existing.sequenceStarted)
            return false;

        int myScore = GetConfigurationScore();
        int existingScore = existing.GetConfigurationScore();

        return myScore > existingScore;
    }

    private int GetConfigurationScore()
    {
        int score = 0;

        score += GetConfiguredDialogueLineCount() * 10;

        if (maidenPrefab != null)
            score += 4;

        if (maidenObject != null)
            score += 2;

        if (earthquakeBgmPlayer != null || earthquakeBgmClip != null)
            score += 2;

        if (gameObject.scene.IsValid())
            score += 1;

        return score;
    }

    private int GetConfiguredDialogueLineCount()
    {
        if (postDefeatDialogueLines == null)
            return 0;

        int count = 0;

        for (int i = 0; i < postDefeatDialogueLines.Length; i++)
        {
            DialogueLine line = postDefeatDialogueLines[i];

            if (line != null && !string.IsNullOrWhiteSpace(line.text))
                count++;
        }

        return count;
    }

    private static bool IsReversedEntranceAnimation(string stateName)
    {
        return NormalizeAnimationName(stateName).Contains("deathreverse");
    }

    private static string NormalizeAnimationName(string animationName)
    {
        return string.IsNullOrWhiteSpace(animationName)
            ? string.Empty
            : animationName.Replace("/", string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
    }

    private static string GetSafeAnimationStateName(string animationName)
    {
        return string.IsNullOrWhiteSpace(animationName)
            ? string.Empty
            : animationName.Replace("/", string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty);
    }
}
