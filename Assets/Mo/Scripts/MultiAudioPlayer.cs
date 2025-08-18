using UnityEngine;

public class MultiAudioPlayer : MonoBehaviour
{
    private AudioSource source1;
    private AudioSource source2;
    private AudioSource source3;

    void Awake()
    {
        // Create 3 audio sources
        source1 = gameObject.AddComponent<AudioSource>();
        source2 = gameObject.AddComponent<AudioSource>();
        source3 = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Plays the first environmental sound, first creature sound, and the music tone (if present).
    /// Uses values from GroqResponseParser safely (handles nulls/empty arrays).
    /// </summary>
    /// <param name="parser">GroqResponseParser instance (must not be null)</param>
    public void PlayAll(GroqResponseParser parser)
    {
        if (parser == null)
        {
            Debug.LogWarning("PlayAll: parser is null.");
            return;
        }

        // Get values from parser (string[] and string)
        string[] environmental = parser.GetEnvironmentalSounds();
        string[] creatures = parser.GetCreatureSounds();
        string musicTone = parser.GetMusicTone();

        // Null/empty-safe assignment (use first element where applicable)
        source1.clip = (environmental != null && environmental.Length > 0 && !string.IsNullOrEmpty(environmental[0]))
            ? LoadAudioClip(environmental[0])
            : null;

        source2.clip = (creatures != null && creatures.Length > 0 && !string.IsNullOrEmpty(creatures[0]))
            ? LoadAudioClip(creatures[0])
            : null;

        source3.clip = !string.IsNullOrEmpty(musicTone)
            ? LoadAudioClip(musicTone)
            : null;

        // Play only the clips that were successfully loaded
        if (source1.clip != null) source1.Play();
        if (source2.clip != null) source2.Play();
        if (source3.clip != null) source3.Play();
    }

    /// <summary>
    /// Stops all playing audio sources.
    /// </summary>
    public void StopPlay()
    {
        if (source1.isPlaying) source1.Stop();
        if (source2.isPlaying) source2.Stop();
        if (source3.isPlaying) source3.Stop();
    }

    /// <summary>
    /// Loads an AudioClip from Resources/AudioFiles/ (clipName without extension).
    /// </summary>
    private AudioClip LoadAudioClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return null;

        AudioClip clip = Resources.Load<AudioClip>("AudioFiles/" + clipName);
        if (clip == null)
        {
            Debug.LogWarning($"Audio clip not found: '{clipName}' in Resources/AudioFiles/");
        }
        return clip;
    }
}
