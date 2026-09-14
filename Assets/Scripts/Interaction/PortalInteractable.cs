using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PortalInteractable : Interactable
{
    [Header("Teleport")]
    [SerializeField] private PortalInteractable targetPortal;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Vector3 fallbackExitOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private bool requireMutualBinding;

    [Header("Cooldown")]
    [SerializeField, Min(0f)] private float cooldownDuration = 60f;
    [SerializeField] private bool shareCooldownWithTarget = true;

    [Header("Boss Lock")]
    [SerializeField] private bool lockedUntilBossDefeated;
    [SerializeField] private CharacterStats bossLockStats;

    [Header("Animation")]
    [SerializeField] private Animator portalAnimator;
    [SerializeField] private string openStateName = "open";
    [SerializeField] private string closeStateName = "close";
    [SerializeField, Range(0.01f, 0.99f)] private float holdNormalizedTime = 0.99f;
    [SerializeField] private bool closeWhenPlayerLeaves = true;

    [Header("Sprite Frame Fallback")]
    [SerializeField] private SpriteRenderer portalSpriteRenderer;
    [SerializeField] private List<Sprite> openFrames = new List<Sprite>();
    [SerializeField] private List<Sprite> closeFrames = new List<Sprite>();
    [SerializeField, Min(0.01f)] private float frameDuration = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioEventPlayer audioEvents;
    [SerializeField] private string openCueName = "PortalOpen";
    [SerializeField] private string closeCueName = "PortalClose";
    [SerializeField] private string teleportCueName = "PortalTeleport";
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip teleportClip;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f;

    private readonly HashSet<Player> nearbyPlayers = new HashSet<Player>();
    private Collider2D portalCollider;
    private Coroutine visualRoutine;
    private bool isOpenReady;
    private bool isOpening;
    private bool isCoolingDown;
    private float cooldownEndTime;
    private bool missingTargetWarned;

    public event System.Action<Player, PortalInteractable, Vector3> OnPlayerTeleported;
    public PortalInteractable TargetPortal => targetPortal;

    protected override void Awake()
    {
        base.Awake();

        portalCollider = GetComponent<Collider2D>();
        portalCollider.isTrigger = true;

        if (portalAnimator == null)
            portalAnimator = GetComponent<Animator>();

        if (portalSpriteRenderer == null)
            portalSpriteRenderer = GetComponent<SpriteRenderer>();

        if (audioEvents == null)
            audioEvents = GetComponent<AudioEventPlayer>();

        HoldClosed();
    }

    private void Update()
    {
        RemoveInvalidNearbyPlayers();

        if (isCoolingDown && Time.time >= cooldownEndTime)
        {
            isCoolingDown = false;

            if (HasNearbyPlayer())
                BeginOpening();
            else
                HoldClosed();
        }

        if (IsBlockedByAliveBoss())
        {
            if (isOpenReady || isOpening)
                BeginClosing();

            return;
        }

        if (!isCoolingDown && HasNearbyPlayer() && CanOpenPortal(false) && !isOpenReady && !isOpening)
            BeginOpening();
    }

    public override bool CanInteract(Player player)
    {
        return base.CanInteract(player)
            && player != null
            && (player.stats == null || !player.stats.isDead)
            && isOpenReady
            && !isCoolingDown
            && !IsBlockedByAliveBoss()
            && HasValidTarget()
            && nearbyPlayers.Contains(player);
    }

    protected override void OnInteract(Player player)
    {
        if (!CanInteract(player))
            return;

        Vector3 destination = targetPortal.GetDestinationPosition();
        PlayPortalSound(teleportCueName, teleportClip);
        TeleportPlayer(player, destination);
        OnPlayerTeleported?.Invoke(player, this, destination);
        BeginSharedCooldown();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player == null)
            return;

        nearbyPlayers.Add(player);
        BeginOpening();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player == null)
            return;

        nearbyPlayers.Add(player);

        if (!isCoolingDown && CanOpenPortal(false) && !isOpenReady && !isOpening)
            BeginOpening();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player == null)
            return;

        nearbyPlayers.Remove(player);

        if (!HasNearbyPlayer() && closeWhenPlayerLeaves && !isCoolingDown)
            BeginClosing();
    }

    public void SetTargetPortal(PortalInteractable target)
    {
        targetPortal = target;
        missingTargetWarned = false;
    }

    public void SetBossDefeatLock(CharacterStats bossStats, bool locked)
    {
        bossLockStats = bossStats;
        lockedUntilBossDefeated = locked && bossStats != null;

        if (IsBlockedByAliveBoss())
        {
            if (isOpenReady || isOpening)
                BeginClosing();
            else
                HoldClosed();

            return;
        }

        if (!isCoolingDown && HasNearbyPlayer() && CanOpenPortal(false) && !isOpenReady && !isOpening)
            BeginOpening();
    }

    public void ClearBossDefeatLock(bool clearCooldown = true)
    {
        lockedUntilBossDefeated = false;
        bossLockStats = null;

        if (clearCooldown)
        {
            isCoolingDown = false;
            cooldownEndTime = 0f;
        }

        if (HasNearbyPlayer() && CanOpenPortal(false) && !isOpenReady && !isOpening)
            BeginOpening();
        else if (!HasNearbyPlayer())
            HoldClosed();
    }

    public Vector3 GetDestinationPosition()
    {
        if (exitPoint != null)
            return exitPoint.position;

        return transform.position + fallbackExitOffset;
    }

    private void BeginOpening()
    {
        if (isCoolingDown || isOpenReady || isOpening || !CanOpenPortal(false))
            return;

        StopVisualRoutine();
        PlayPortalSound(openCueName, openClip);
        visualRoutine = StartCoroutine(PlayAndHoldRoutine(openStateName, true));
    }

    private void BeginClosing()
    {
        if (isCoolingDown)
            return;

        isOpenReady = false;
        isOpening = false;

        StopVisualRoutine();
        PlayPortalSound(closeCueName, closeClip);
        visualRoutine = StartCoroutine(PlayAndHoldRoutine(closeStateName, false));
    }

    private void BeginSharedCooldown()
    {
        BeginCooldown();

        if (shareCooldownWithTarget && targetPortal != null && targetPortal != this)
            targetPortal.BeginCooldown();
    }

    private void BeginCooldown()
    {
        cooldownEndTime = Time.time + cooldownDuration;
        isCoolingDown = true;
        isOpenReady = false;
        isOpening = false;

        StopVisualRoutine();
        visualRoutine = StartCoroutine(PlayAndHoldRoutine(closeStateName, false));
    }

    private IEnumerator PlayAndHoldRoutine(string stateName, bool openedWhenFinished)
    {
        isOpening = openedWhenFinished;
        isOpenReady = false;

        List<Sprite> frames = GetManualFrames(openedWhenFinished);

        if (CanPlayManualFrames(frames))
        {
            SetAnimatorEnabled(false);
            yield return PlayManualFrames(frames);
            HoldManualState(openedWhenFinished);
        }
        else
        {
            SetAnimatorEnabled(true);
            PlayState(stateName, 0f, 1f);
            yield return new WaitForSeconds(GetAnimationLength(stateName));
            HoldStateAtEnd(stateName);
        }

        isOpening = false;
        isOpenReady = openedWhenFinished && !isCoolingDown && HasNearbyPlayer() && CanOpenPortal(false);
        visualRoutine = null;

        if (!openedWhenFinished)
            isOpenReady = false;
    }

    private void PlayState(string stateName, float normalizedTime, float speed)
    {
        if (portalAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        SetAnimatorEnabled(true);
        portalAnimator.speed = speed;
        portalAnimator.Play(stateName, 0, normalizedTime);
        portalAnimator.Update(0f);
    }

    private void HoldStateAtEnd(string stateName)
    {
        if (portalAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        SetAnimatorEnabled(true);
        portalAnimator.speed = 1f;
        portalAnimator.Play(stateName, 0, holdNormalizedTime);
        portalAnimator.Update(0f);
        portalAnimator.speed = 0f;
    }

    private void HoldClosed()
    {
        isOpenReady = false;
        isOpening = false;

        if (HoldManualState(false))
            SetAnimatorEnabled(false);
        else
            HoldStateAtEnd(closeStateName);
    }

    private void StopVisualRoutine()
    {
        if (visualRoutine == null)
            return;

        StopCoroutine(visualRoutine);
        visualRoutine = null;
    }

    private float GetAnimationLength(string stateName)
    {
        if (portalAnimator == null || portalAnimator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            return 0.8f;

        AnimationClip[] clips = portalAnimator.runtimeAnimatorController.animationClips;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];

            if (clip != null && clip.name == stateName)
                return Mathf.Max(0.01f, clip.length);
        }

        return 0.8f;
    }

    private List<Sprite> GetManualFrames(bool openedWhenFinished)
    {
        return openedWhenFinished ? openFrames : closeFrames;
    }

    private bool CanPlayManualFrames(List<Sprite> frames)
    {
        return portalSpriteRenderer != null && frames != null && frames.Count > 0;
    }

    private IEnumerator PlayManualFrames(List<Sprite> frames)
    {
        for (int i = 0; i < frames.Count; i++)
        {
            Sprite frame = frames[i];

            if (frame != null)
                portalSpriteRenderer.sprite = frame;

            yield return new WaitForSeconds(frameDuration);
        }
    }

    private bool HoldManualState(bool opened)
    {
        if (portalSpriteRenderer == null)
            return false;

        List<Sprite> frames = opened ? openFrames : closeFrames;
        Sprite frame = GetLastValidFrame(frames);

        if (frame == null && !opened)
            frame = GetFirstValidFrame(openFrames);

        if (frame == null && opened)
            frame = GetFirstValidFrame(closeFrames);

        if (frame == null)
            return false;

        portalSpriteRenderer.sprite = frame;
        return true;
    }

    private Sprite GetLastValidFrame(List<Sprite> frames)
    {
        if (frames == null)
            return null;

        for (int i = frames.Count - 1; i >= 0; i--)
        {
            if (frames[i] != null)
                return frames[i];
        }

        return null;
    }

    private Sprite GetFirstValidFrame(List<Sprite> frames)
    {
        if (frames == null)
            return null;

        for (int i = 0; i < frames.Count; i++)
        {
            if (frames[i] != null)
                return frames[i];
        }

        return null;
    }

    private void SetAnimatorEnabled(bool enabled)
    {
        if (portalAnimator != null && portalAnimator.enabled != enabled)
            portalAnimator.enabled = enabled;
    }

    private bool HasValidTarget(bool logWarning = true)
    {
        bool validTarget = targetPortal != null && targetPortal != this;

        if (validTarget && requireMutualBinding)
            validTarget = targetPortal.targetPortal == this;

        if (!validTarget && logWarning && !missingTargetWarned)
        {
            Debug.LogWarning(
                requireMutualBinding
                    ? $"{name} requires a mutual target portal binding before it can teleport."
                    : $"{name} has no target portal assigned, so it cannot teleport.",
                this);
            missingTargetWarned = true;
        }

        return validTarget;
    }

    private bool CanOpenPortal(bool logWarning = true)
    {
        return HasValidTarget(logWarning) && !IsBlockedByAliveBoss();
    }

    private bool IsBlockedByAliveBoss()
    {
        return lockedUntilBossDefeated
            && bossLockStats != null
            && !bossLockStats.isDead
            && bossLockStats.currentHealth > 0;
    }

    private void PlayPortalSound(string cueName, AudioClip clip)
    {
        AudioPlaybackUtility.PlayCueOrClip(audioEvents, cueName, clip, transform, audioVolume, 1f, spatialBlend);
    }

    private bool HasNearbyPlayer()
    {
        RemoveInvalidNearbyPlayers();
        return nearbyPlayers.Count > 0;
    }

    private void RemoveInvalidNearbyPlayers()
    {
        nearbyPlayers.RemoveWhere(player => player == null || player.stats != null && player.stats.isDead);
    }

    private void TeleportPlayer(Player player, Vector3 destination)
    {
        if (player == null)
            return;

        if (player.rb != null)
        {
            player.rb.velocity = Vector2.zero;
            player.rb.position = destination;
        }

        player.transform.position = destination;
        Physics2D.SyncTransforms();
    }
}
