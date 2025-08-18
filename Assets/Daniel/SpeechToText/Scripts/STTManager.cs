using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;
using Button = UnityEngine.UI.Button;
using Debug = UnityEngine.Debug;

public class STTManager : MonoBehaviour
{
    [SerializeField] private WhisperManager whisper;
    [SerializeField] private MicrophoneRecord microphoneRecord;
    [SerializeField] private bool streamSegments = true;
    [SerializeField] private bool printLanguage = true;

    [Header("UI (Testing Only)")] 
    [SerializeField] private OVRInput.RawButton actionButton = OVRInput.RawButton.A;
    public Button button;
    public TMP_Text buttonText;
    public TMP_Text outputText;
    public ScrollRect scroll;
    
    [Header("Events")]
    public UnityEvent onWhisperReady;
    public UnityEvent<string> onTranscribed;
    
    private string _buffer;
    private bool _didModelLoadNotify = false;

    private void Awake()
    {
        whisper.OnNewSegment += OnNewSegment;
        
        microphoneRecord.OnRecordStop += OnRecordStop;
        
        button.onClick.AddListener(OnButtonPressed);
    }

    private void Update()
    {
        if (whisper.IsLoading)
        {
            button.enabled = false;
            buttonText.text = "Loadig Model...";
            return;
        }

        if (!_didModelLoadNotify && whisper.IsLoaded)
        {
            button.enabled = true;
            buttonText.text = "Record";
            onWhisperReady?.Invoke();
            _didModelLoadNotify = true;
        }

        if (OVRInput.GetUp(actionButton))
        {
            OnStartRecording();
        }
    }

    private void OnButtonPressed()
    {
        OnStartRecording();
    }
    
    /**
     * OnStartRecording()
     * Starts the microhpone listening process to transcribe using whisper
     * The recording uses the local microphone device (main one)
     */
    public void OnStartRecording()
    {
        if (!microphoneRecord.IsRecording)
        {
            microphoneRecord.StartRecord();
            buttonText.text = "Stop";
        }
        else
        {
            microphoneRecord.StopRecord();
            buttonText.text = "Record";
        }
    }
    
    private static AudioClip ToAudioClip(AudioChunk chunk, string clipName = "AudioChunkClip")
    {
        if (chunk.Data == null || chunk.Data.Length == 0)
        {
            Debug.LogWarning("AudioChunk has no data.");
            return null;
        }

        // Create an empty clip with correct settings
        AudioClip clip = AudioClip.Create(
            clipName,
            chunk.Data.Length / chunk.Channels,
            chunk.Channels,
            chunk.Frequency,
            false
        );

        // Fill clip with data
        clip.SetData(chunk.Data, 0);

        return clip;
    }
    
    private async void OnRecordStop(AudioChunk recordedAudio)
    {
        buttonText.text = "Record";
        _buffer = "";
        
        var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
        if (res == null || !outputText) 
            return;

        // StartCoroutine(OpenAIManager.STTCoroutine(ToAudioClip(recordedAudio), transcription =>
        // {
        //     onTranscribed?.Invoke(transcription);
        //     outputText.text = transcription;
        //     Debug.Log("TEXT: " + transcription);
        //     UiUtils.ScrollDown(scroll);
        // }));
        
        var text = res.Result;
        if (printLanguage)
            text += $"\n\nLanguage: {res.Language}";
        
        onTranscribed?.Invoke(text);
        outputText.text = text;
        Debug.Log("TEXT: " + text);
        UiUtils.ScrollDown(scroll);
    }
    
    private void OnNewSegment(WhisperSegment segment)
    {
        if (!streamSegments || !outputText)
            return;

        _buffer += segment.Text;
        outputText.text = _buffer + "...";
        UiUtils.ScrollDown(scroll);
    }
}