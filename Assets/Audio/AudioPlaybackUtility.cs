using UnityEngine;

public static class AudioPlaybackUtility
{
    public static bool PlayCueOrClip(
        AudioEventPlayer audioEvents,
        string cueName,
        AudioClip fallbackClip,
        Transform source,
        float volume = 1f,
        float pitch = 1f,
        float spatialBlend = 0f)
    {
        if (audioEvents != null && audioEvents.PlayCue(cueName))
            return true;

        if (fallbackClip == null)
            return false;

        AudioManager manager = AudioManager.GetOrCreateInstance();
        Vector3 position = source != null ? source.position : manager.transform.position;
        manager.PlaySFX(fallbackClip, volume, pitch, position, spatialBlend);
        return true;
    }
}
