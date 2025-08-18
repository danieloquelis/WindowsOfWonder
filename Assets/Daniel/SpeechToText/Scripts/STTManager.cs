using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// <summary>
/// Connects to a local STT WebSocket server, streams Base64 PCM16 chunks from MicrophoneStreamer,
/// and displays/dispatches transcription results. Uses System.Net.WebSockets.
/// </summary>
public class STTManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string serverUrl = "ws://192.168.1.100:8000"; // set your Mac IP
    [SerializeField] private bool streamSegments = false; // server returns final result on stop by default

    [Header("Dependencies")]
    [SerializeField] private MicrophoneStreamer microphoneStreamer;

    [Header("UI (Testing Only)")]
    [SerializeField] private OVRInput.RawButton actionButton = OVRInput.RawButton.A;
    public Button button;
    public TMP_Text buttonText;
    public TMP_Text outputText;

    [Header("Events")]
    public UnityEvent onWhisperReady;
    public UnityEvent<string> onTranscribed;

    private ClientWebSocket ws;
    private CancellationTokenSource cts;
    private bool _isRecording = false;
    private string _buffer = "";
    private int _chunkCount = 0;

    // Thread-safe messages from ReceiveLoop -> processed on main thread in Update()
    private readonly ConcurrentQueue<string> _incomingTexts = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<string> _incomingErrors = new ConcurrentQueue<string>();

    #region Unity Lifecycle

    private async void Start()
    {
        if (button != null) button.onClick.AddListener(OnButtonPressed);
        await ConnectWebSocket();
    }

    private void Update()
    {
        // Optional controller shortcut
        if (OVRInput.GetUp(actionButton))
            OnStartRecording();

        // Pump results from background receive loop to main thread/UI
        while (_incomingTexts.TryDequeue(out var text))
        {
            if (streamSegments)
                AppendStreamedSegment(text);
            else
                HandleFinalResult(text);
        }
        while (_incomingErrors.TryDequeue(out var err))
        {
            Debug.LogError($"STT error: {err}");
        }
    }

    private void OnDestroy()
    {
        if (microphoneStreamer != null)
            microphoneStreamer.OnAudioChunk -= SendAudioChunk;

        _ = CloseWebSocket();
        if (button != null) button.onClick.RemoveListener(OnButtonPressed);
    }

    #endregion

    #region WebSocket

    private async Task ConnectWebSocket()
    {
        ws = new ClientWebSocket();
        cts = new CancellationTokenSource();

        try
        {
            await ws.ConnectAsync(new Uri(serverUrl), cts.Token);
            Debug.Log("Connected to STT server");
            onWhisperReady?.Invoke();
            _ = ReceiveLoop(); // fire-and-forget
        }
        catch (Exception e)
        {
            Debug.LogError("WebSocket connect failed: " + e.Message);
        }
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[32 * 1024];

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", cts.Token);
                    break;
                }

                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                // Expect {"text":"..."} or {"error":"..."}
                var res = JsonUtility.FromJson<STTResult>(msg);
                if (res != null && !string.IsNullOrEmpty(res.text))
                {
                    _incomingTexts.Enqueue(res.text);
                    continue;
                }

                var err = JsonUtility.FromJson<STTError>(msg);
                if (err != null && !string.IsNullOrEmpty(err.error))
                {
                    _incomingErrors.Enqueue(err.error);
                }
            }
        }
        catch (Exception e)
        {
            _incomingErrors.Enqueue($"ReceiveLoop exception: {e.Message}");
        }
    }

    private async Task CloseWebSocket()
    {
        try
        {
            if (ws != null && (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived))
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("CloseWebSocket warning: " + e.Message);
        }
        finally
        {
            ws?.Dispose();
            ws = null;
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
    }

    private async Task SendAsync(string json)
    {
        if (ws == null || ws.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
        }
        catch (Exception e)
        {
            Debug.LogError("SendAsync error: " + e.Message);
        }
    }

    #endregion

    #region Controls & Streaming

    private void OnButtonPressed() => OnStartRecording();

    /// <summary>
    /// Toggle recording (invoked by UI button or external callers).
    /// </summary>
    public void OnStartRecording()
    {
        if (!_isRecording) StartRecording();
        else StopRecording();
    }

    private void StartRecording()
    {
        if (microphoneStreamer == null)
        {
            Debug.LogError("STTManager: MicrophoneStreamer is not assigned in the inspector.");
            return;
        }

        _isRecording = true;
        _buffer = "";
        _chunkCount = 0;
        if (buttonText) buttonText.text = "Stop";

        microphoneStreamer.OnAudioChunk += SendAudioChunk;
        microphoneStreamer.StartStreaming();

        Debug.Log("Recording started... awaiting chunks");
    }

    private void StopRecording()
    {
        _isRecording = false;
        if (buttonText) buttonText.text = "Record";

        if (microphoneStreamer != null)
        {
            microphoneStreamer.OnAudioChunk -= SendAudioChunk;
            microphoneStreamer.StopStreaming();
        }

        var stopMsg = JsonUtility.ToJson(new StopEvent { eventType = "stop" });
        _ = SendAsync(stopMsg);

        Debug.Log($"Recording stopped. Chunks sent: {_chunkCount}");
    }

    private void SendAudioChunk(string base64Chunk)
    {
        _chunkCount++;
        if (_chunkCount <= 5)
            Debug.Log($"Sending chunk #{_chunkCount} (base64 len {base64Chunk.Length})");

        var audioMsg = JsonUtility.ToJson(new AudioEvent { audio = base64Chunk });
        _ = SendAsync(audioMsg);
    }

    #endregion

    #region Results & DTOs

    private void AppendStreamedSegment(string segment)
    {
        _buffer += segment;
        if (outputText != null) outputText.text = _buffer + "...";
    }

    private void HandleFinalResult(string text)
    {
        onTranscribed?.Invoke(text);
        if (outputText != null) outputText.text = text;
    }

    [Serializable] private class STTResult { public string text; }
    [Serializable] private class STTError  { public string error; }
    [Serializable] private class StopEvent { public string eventType; }  // "stop"
    [Serializable] private class AudioEvent { public string audio; }     // Base64 PCM16

    #endregion
}
