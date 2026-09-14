using System;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioEventPlayer : MonoBehaviour
{
    [Serializable]
    public class AudioCue
    {
        public string cueName;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.05f, 3f)] public float minPitch = 1f;
        [Range(0.05f, 3f)] public float maxPitch = 1f;
        [Min(0f)] public float cooldown;
        [Range(0f, 1f)] public float spatialBlend;
        public bool playAtTransformPosition;

        [NonSerialized] public float nextAllowedTime;
    }

    [Header("Audio Cues")]
    [SerializeField] private AudioCue[] cues;

    [Header("常用音效名称")]
    [SerializeField] private string pickupCueName = "Pickup";

    public AudioCue[] Cues => cues;

    public void PlayFirstCue()
    {
        if (cues == null)
            return;

        for (int i = 0; i < cues.Length; i++)
        {
            AudioCue cue = cues[i];

            if (cue != null && !string.IsNullOrWhiteSpace(cue.cueName))
            {
                PlayCue(cue.cueName);
                return;
            }
        }
    }

    public void PlayCueFromEvent(string cueName)
    {
        PlayCue(cueName);
    }

    public void PlayPickupCue()
    {
        PlayCue(pickupCueName);
    }

    public void PlayPickupCueFromEvent()
    {
        PlayPickupCue();
    }

    public bool PlayCue(string cueName)
    {
        if (string.IsNullOrWhiteSpace(cueName))
            return false;

        AudioCue cue = FindCue(cueName);

        if (cue == null || cue.clips == null || cue.clips.Length == 0)
            return false;

        if (cue.cooldown > 0f && Time.time < cue.nextAllowedTime)
            return false;

        AudioClip clip = PickClip(cue.clips);

        if (clip == null)
            return false;

        cue.nextAllowedTime = Time.time + Mathf.Max(0f, cue.cooldown);

        float minPitch = Mathf.Max(0.05f, Mathf.Min(cue.minPitch, cue.maxPitch));
        float maxPitch = Mathf.Max(minPitch, Mathf.Max(cue.minPitch, cue.maxPitch));
        float pitch = Mathf.Approximately(minPitch, maxPitch) ? minPitch : UnityEngine.Random.Range(minPitch, maxPitch);

        AudioManager manager = AudioManager.GetOrCreateInstance();
        Vector3 position = cue.playAtTransformPosition ? transform.position : manager.transform.position;

        manager.PlaySFX(clip, cue.volume, pitch, position, cue.spatialBlend);
        return true;
    }

    public bool HasCue(string cueName)
    {
        return FindCue(cueName) != null;
    }

    private AudioCue FindCue(string cueName)
    {
        if (cues == null)
            return null;

        for (int i = 0; i < cues.Length; i++)
        {
            AudioCue cue = cues[i];

            if (cue != null && string.Equals(cue.cueName, cueName, StringComparison.OrdinalIgnoreCase))
                return cue;
        }

        return null;
    }

    private AudioClip PickClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        if (clips.Length == 1)
            return clips[0];

        for (int i = 0; i < 6; i++)
        {
            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];

            if (clip != null)
                return clip;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
                return clips[i];
        }

        return null;
    }
}