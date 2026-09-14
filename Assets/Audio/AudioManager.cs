using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";

    public static AudioManager Instance;

    [Header("BGM Source")]
    public AudioSource bgmSource;

    [Header("SFX Source")]
    public AudioSource sfxSource;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioClip currentBGM;
    private float currentBgmVolumeScale = 1f;
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private readonly Dictionary<AudioSource, float> sfxVolumeScales = new Dictionary<AudioSource, float>();

    public float MusicVolume => Mathf.Clamp01(musicVolume);
    public float SfxVolume => Mathf.Clamp01(sfxVolume);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadVolumeSettings();
        EnsureAudioSources();
    }

    public static AudioManager GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        AudioManager foundManager = FindObjectOfType<AudioManager>();

        if (foundManager != null)
        {
            Instance = foundManager;
            Instance.LoadVolumeSettings();
            Instance.EnsureAudioSources();
            return Instance;
        }

        GameObject managerObject = new GameObject("AudioManager");
        Instance = managerObject.AddComponent<AudioManager>();
        Instance.EnsureAudioSources();
        return Instance;
    }

    public void PlayBGM(AudioClip clip, bool loop = true, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        currentBgmVolumeScale = Mathf.Clamp01(volumeScale);
        EnsureAudioSources();

        if (bgmSource == null)
            return;

        if (currentBGM == clip && bgmSource.isPlaying)
        {
            bgmSource.loop = loop;
            ApplyBgmVolume();
            return;
        }

        currentBGM = clip;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        ApplyBgmVolume();
        bgmSource.Play();
    }

    public void StopBGM()
    {
        EnsureAudioSources();

        if (bgmSource == null)
            return;

        bgmSource.Stop();
        currentBGM = null;
        currentBgmVolumeScale = 1f;
    }

    public void PlaySFX(AudioClip clip)
    {
        PlaySFX(clip, 1f, 1f, transform.position, 0f);
    }

    public void PlaySFX(AudioClip clip, float volumeScale, float pitch, Vector3 position, float spatialBlend = 0f)
    {
        if (clip == null)
            return;

        EnsureAudioSources();

        if (sfxSource == null)
            return;

        AudioSource source = GetAvailableSfxSource();

        source.transform.position = position;
        source.clip = clip;
        source.loop = false;
        source.playOnAwake = false;
        sfxVolumeScales[source] = Mathf.Clamp01(volumeScale);
        source.volume = Mathf.Clamp01(sfxVolume) * sfxVolumeScales[source];
        source.pitch = Mathf.Max(0.01f, pitch);
        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.Play();
    }

    public void SetMusicVolume(float volume, bool save = true)
    {
        musicVolume = Mathf.Clamp01(volume);

        EnsureAudioSources();

        ApplyBgmVolume();

        if (save)
            SaveVolumeSettings();
    }

    public void SetSfxVolume(float volume, bool save = true)
    {
        sfxVolume = Mathf.Clamp01(volume);

        for (int i = sfxSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = sfxSources[i];

            if (source == null)
            {
                sfxSources.RemoveAt(i);
                continue;
            }

            float scale = sfxVolumeScales.TryGetValue(source, out float storedScale) ? storedScale : 1f;
            source.volume = sfxVolume * scale;
        }

        if (save)
            SaveVolumeSettings();
    }

    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(musicVolume));
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(sfxVolume));
        PlayerPrefs.Save();
    }

    private void LoadVolumeSettings()
    {
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume));
    }

    private void EnsureAudioSources()
    {
        if (bgmSource == null)
            bgmSource = CreateChildSource("BGM Source");

        if (sfxSource == null)
            sfxSource = CreateChildSource("SFX Source");

        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            ApplyBgmVolume();
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            if (!sfxSources.Contains(sfxSource))
                sfxSources.Add(sfxSource);

            if (!sfxVolumeScales.ContainsKey(sfxSource))
                sfxVolumeScales.Add(sfxSource, 1f);
        }
    }

    private AudioSource CreateChildSource(string sourceName)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        return sourceObject.AddComponent<AudioSource>();
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource != null)
            bgmSource.volume = Mathf.Clamp01(musicVolume) * Mathf.Clamp01(currentBgmVolumeScale);
    }

    private AudioSource GetAvailableSfxSource()
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            AudioSource source = sfxSources[i];

            if (source != null && !source.isPlaying)
                return source;
        }

        AudioSource newSource = CreateChildSource("SFX One Shot");
        sfxSources.Add(newSource);
        sfxVolumeScales[newSource] = 1f;
        return newSource;
    }
}
