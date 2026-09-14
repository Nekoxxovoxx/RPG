using UnityEngine;

[AddComponentMenu("Audio/Player Death BGM Controller")]
public class PlayerDeathBgmController : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private AudioClip deathBgmClip;
    [SerializeField] private bool loop = true;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool restoreRegularBgmOnRevive = true;
    [SerializeField] private bool autoResolvePlayer = true;

    private PlayerStats subscribedPlayerStats;
    private bool deathBgmActive;

    private void OnEnable()
    {
        TryResolvePlayerStats();
        Subscribe();
    }

    private void Start()
    {
        TryResolvePlayerStats();
        Subscribe();

        if (playerStats != null && playerStats.isDead)
            HandlePlayerDeath();
    }

    private void Update()
    {
        if (autoResolvePlayer && playerStats == null)
        {
            TryResolvePlayerStats();
            Subscribe();
        }

        if (!deathBgmActive)
            return;

        if (playerStats == null && autoResolvePlayer)
            TryResolvePlayerStats();

        if (playerStats != null && !playerStats.isDead)
            StopDeathBgm();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetPlayerStats(PlayerStats stats)
    {
        if (playerStats == stats)
            return;

        Unsubscribe();
        playerStats = stats;
        Subscribe();
    }

    public void PlayDeathBgm()
    {
        deathBgmActive = true;

        AudioManager audioManager = AudioManager.GetOrCreateInstance();

        if (deathBgmClip != null)
            audioManager.PlayBGM(deathBgmClip, loop, volume);
        else
            audioManager.StopBGM();
    }

    private void TryResolvePlayerStats()
    {
        if (!autoResolvePlayer || playerStats != null)
            return;

        Player player = PlayerManager.instance != null ? PlayerManager.instance.player : FindObjectOfType<Player>();

        if (player != null)
            playerStats = player.stats as PlayerStats ?? player.GetComponent<PlayerStats>();

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();
    }

    private void Subscribe()
    {
        if (subscribedPlayerStats == playerStats)
            return;

        Unsubscribe();

        if (playerStats == null)
            return;

        playerStats.OnPlayerDeath += HandlePlayerDeath;
        subscribedPlayerStats = playerStats;
    }

    private void Unsubscribe()
    {
        if (subscribedPlayerStats == null)
            return;

        subscribedPlayerStats.OnPlayerDeath -= HandlePlayerDeath;
        subscribedPlayerStats = null;
    }

    private void HandlePlayerDeath()
    {
        if (deathBgmActive)
            return;

        PlayDeathBgm();
    }

    private void StopDeathBgm()
    {
        deathBgmActive = false;

        if (restoreRegularBgmOnRevive)
        {
            if (!BGMPlayer.PlayLastRegularBGM())
                AudioManager.GetOrCreateInstance().StopBGM();

            return;
        }

        AudioManager.GetOrCreateInstance().StopBGM();
    }
}
