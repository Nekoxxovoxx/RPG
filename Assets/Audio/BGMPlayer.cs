using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    private static AudioClip lastRegularBgmClip;
    private static bool lastRegularBgmLoop = true;
    private static float lastRegularBgmVolume = 1f;

    [Header("Current Level BGM")]
    public AudioClip bgmClip;

    [Header("Loop")]
    public bool loop = true;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private PortalInteractable portalTrigger;
    [SerializeField] private bool playOnPortalTeleported;

    public float BgmVolume => Mathf.Clamp01(bgmVolume);

    private void OnEnable()
    {
        SubscribeToPortal();
    }

    private void OnDisable()
    {
        UnsubscribeFromPortal();
    }

    private void Start()
    {
        if (!playOnStart)
            return;

        RegisterAsRegularBGM();
        PlayConfiguredBGM();
    }

    public void PlayConfiguredBGM()
    {
        if (!playOnPortalTeleported)
            RegisterAsRegularBGM();

        AudioManager.GetOrCreateInstance().PlayBGM(bgmClip, loop, BgmVolume);
    }

    public static bool PlayLastRegularBGM()
    {
        if (lastRegularBgmClip == null)
            TryFindRegularBGMInLoadedObjects();

        if (lastRegularBgmClip == null)
            return false;

        AudioManager.GetOrCreateInstance().PlayBGM(lastRegularBgmClip, lastRegularBgmLoop, lastRegularBgmVolume);
        return true;
    }

    public void SetPortalTrigger(PortalInteractable portal)
    {
        if (portalTrigger == portal)
            return;

        UnsubscribeFromPortal();
        portalTrigger = portal;
        SubscribeToPortal();
    }

    private void SubscribeToPortal()
    {
        if (!playOnPortalTeleported || portalTrigger == null)
            return;

        portalTrigger.OnPlayerTeleported += HandlePlayerTeleported;
    }

    private void UnsubscribeFromPortal()
    {
        if (portalTrigger == null)
            return;

        portalTrigger.OnPlayerTeleported -= HandlePlayerTeleported;
    }

    private void HandlePlayerTeleported(Player player, PortalInteractable portal, Vector3 destination)
    {
        _ = player;
        _ = portal;
        _ = destination;

        PlayConfiguredBGM();
    }

    private void RegisterAsRegularBGM()
    {
        if (bgmClip == null || playOnPortalTeleported)
            return;

        lastRegularBgmClip = bgmClip;
        lastRegularBgmLoop = loop;
        lastRegularBgmVolume = BgmVolume;
    }

    private static void TryFindRegularBGMInLoadedObjects()
    {
        BGMPlayer[] players = FindObjectsOfType<BGMPlayer>(true);

        for (int i = 0; i < players.Length; i++)
        {
            BGMPlayer player = players[i];

            if (player == null || player.bgmClip == null || player.playOnPortalTeleported)
                continue;

            lastRegularBgmClip = player.bgmClip;
            lastRegularBgmLoop = player.loop;
            lastRegularBgmVolume = player.BgmVolume;
            return;
        }
    }
}
