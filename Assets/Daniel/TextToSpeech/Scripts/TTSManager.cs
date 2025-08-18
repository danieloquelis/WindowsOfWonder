using System;
using System.Collections;
using UnityEngine;
using System.ComponentModel;
using UnityEditor;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class TTSManager : MonoBehaviour
{
    private enum Model
    {
        [Description("tts-1")]
        tts_1,
        [Description("tts-1-hd")]
        tts_1_hd,
        [Description("gpt-4o-mini-tts")]
        gpt_4o_mini_tts
    }

    private enum Voice
    {
        alloy,
        echo,
        fable,
        onyx,
        nova,
        shimmer
    }

    [SerializeField] private Model model = Model.tts_1;
    [SerializeField] private Voice voice = Voice.alloy;
    [SerializeField] private string instructions;

    [Header("Test only (Optional)")]
    [SerializeField] private string testText = "Hey there";
    [SerializeField] private OVRInput.RawButton actionButton = OVRInput.RawButton.A;
    
    [Header("Events")]
    public UnityEvent onStartProcessing;
    public UnityEvent onProcessingFinished;
    
    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (OVRInput.GetUp(actionButton) && !_audioSource.isPlaying)
        {
            TestSpeech();
        }
    }

    public void Speak(string text, Action onComplete = null)
    {
        StartCoroutine(SpeakRoutine(text, onComplete));
    }

    private IEnumerator SpeakRoutine(string text, Action onComplete)
    {
        var content = string.IsNullOrWhiteSpace(text) ? testText : text;

        // If something is playing, WAIT instead of silently returning.
        if (_audioSource.isPlaying)
            yield return new WaitWhile(() => _audioSource.isPlaying);

        Debug.Log($"[TTS] Starting speech. isPlaying={_audioSource.isPlaying}, text='{content}'");

        onStartProcessing?.Invoke();

        // Kick off TTS fetch
        StartCoroutine(OpenAIManager.TTSCoroutine(
            content,
            model.GetDescription(),
            voice.ToString(),
            instructions,
            audioClip =>
            {
                onProcessingFinished?.Invoke();

                if (audioClip)
                {
                    Debug.Log($"[TTS] Inference done. clipLength={audioClip.length:F2}s");
                    _audioSource.PlayOneShot(audioClip);
                    // Wait for ACTUAL playback to finish (more reliable than WaitForSeconds)
                    StartCoroutine(WaitForAudioToFinish(onComplete));
                }
                else
                {
                    Debug.LogWarning("[TTS] No AudioClip returned (empty text? API error?).");
                    onComplete?.Invoke();
                }
            }));
    }

    private IEnumerator WaitForAudioToFinish(Action onComplete)
    {
        yield return new WaitWhile(() => _audioSource.isPlaying);
        Debug.Log("[TTS] Playback finished.");
        onComplete?.Invoke();
    }
    
    
    /**
     * DO NOT Use this
     * ONLY for testing purposes.
     */
    public void TestSpeech()
    {
        Speak(testText);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TTSManager))]
public class TTSManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUILayout.Space(10);
        base.OnInspectorGUI();
        var manager = (TTSManager) target;

        GUILayout.Space(10);
        if (GUILayout.Button("Speech"))
        {
            manager.TestSpeech();
        }
    }
}
#endif

public static class EnumExtensions
{
    public static string GetDescription(this System.Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = (DescriptionAttribute)System.Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attribute == null ? value.ToString() : attribute.Description;
    }
}