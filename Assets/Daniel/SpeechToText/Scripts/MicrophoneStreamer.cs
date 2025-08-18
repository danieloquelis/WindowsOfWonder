using System;
using UnityEngine;

/// <summary>
/// Streams microphone audio as Base64-encoded 16-bit mono PCM chunks.
/// Each chunk is 1,024 samples (~64 ms at 16 kHz).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MicrophoneStreamer : MonoBehaviour
{
    public Action<string> OnAudioChunk;

    [Header("Debug")]
    public bool verboseLogs = true;

    private const int SampleRateOut   = 16_000; 
    private const int ChunkSamplesOut = 1_024;

    private AudioClip _microphoneClip;
    private string _micDevice;         // null => default device
    private int _micSampleRate;   
    private int _chunkSamplesIn;  
    private int _lastSamplePos;
    private bool _isRecording;

    private AudioSource _audioSource;
    private float _posLogTimer;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        _audioSource.volume = 0f;  // mute to avoid feedback
    }

    private void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("MicrophoneStreamer: no microphone devices. (Grant OS permission?)");
            enabled = false;
            return;
        }

        // Use default device for stability across platforms
        _micDevice = null;

        if (verboseLogs)
        {
            var devs = string.Join(", ", Microphone.devices);
            Debug.Log($"[MicrophoneStreamer] Devices: {devs}");
        }
    }

    private void Update()
    {
        if (!_isRecording || !_microphoneClip) return;

        // Log mic cursor occasionally to verify movement
        _posLogTimer += Time.unscaledDeltaTime;
        if (_posLogTimer >= 0.5f)
        {
            _posLogTimer = 0f;
            int pos = Microphone.GetPosition(_micDevice);
            if (verboseLogs) Debug.Log($"[MicrophoneStreamer] Mic position: {pos}/{_microphoneClip.samples}");
        }

        var currentPos = Microphone.GetPosition(_micDevice);
        if (currentPos <= 0) return; // not ready yet

        var samplesAvailable = currentPos - _lastSamplePos;
        if (samplesAvailable < 0) 
            samplesAvailable += _microphoneClip.samples;

        if (samplesAvailable < _chunkSamplesIn) return;

        var inBuf = new float[_chunkSamplesIn];
        ReadCircular(_microphoneClip, _lastSamplePos, inBuf);
        _lastSamplePos = (_lastSamplePos + _chunkSamplesIn) % _microphoneClip.samples;

        var pcm16 = DownsampleAndConvert(inBuf, _micSampleRate, SampleRateOut);
        OnAudioChunk?.Invoke(Convert.ToBase64String(pcm16));
    }

    public void StartStreaming()
    {
        if (_isRecording) return;

        // Use a longer ring buffer (e.g., 5s) for stability
        int bufferSeconds = 5;
        _microphoneClip   = Microphone.Start(_micDevice, loop: true, lengthSec: bufferSeconds, frequency: SampleRateOut);

        // Start a coroutine to wait until the mic actually starts producing data
        StartCoroutine(WaitForMicReady());
    }

    public void StopStreaming()
    {
        if (!_isRecording) return;

        if (Microphone.IsRecording(_micDevice))
            Microphone.End(_micDevice);

        _audioSource.Stop();
        _microphoneClip = null;
        _isRecording = false;

        if (verboseLogs)
            Debug.Log("[MicrophoneStreamer] stopped.");
    }

    private System.Collections.IEnumerator WaitForMicReady()
    {
        float startTime = Time.realtimeSinceStartup;
        // Some platforms take a few frames before GetPosition advances > 0
        while (Microphone.GetPosition(_micDevice) <= 0)
        {
            if (Time.realtimeSinceStartup - startTime > 3f)
            {
                Debug.LogWarning("[MicrophoneStreamer] Microphone did not start within 3s.");
                break;
            }
            yield return null;
        }

        if (_microphoneClip == null)
            yield break;

        _micSampleRate  = _microphoneClip.frequency;
        _chunkSamplesIn = Mathf.Max(1, Mathf.RoundToInt(ChunkSamplesOut * (float)_micSampleRate / SampleRateOut));
        _lastSamplePos  = Microphone.GetPosition(_micDevice); // start reading from current cursor

        // Drive the ring buffer by playing the clip on a muted AudioSource
        _audioSource.clip = _microphoneClip;
        _audioSource.loop = true;
        _audioSource.volume = 0f;
        _audioSource.Play();

        _isRecording = true;

        if (verboseLogs)
        {
            Debug.Log($"[MicrophoneStreamer] start: dev={( _micDevice ?? "default")}, " +
                      $"requested={SampleRateOut} Hz, real={_micSampleRate} Hz, " +
                      $"clipSamples={_microphoneClip.samples}, channels={_microphoneClip.channels}, " +
                      $"chunkIn={_chunkSamplesIn}");
        }
    }

    private static void ReadCircular(AudioClip clip, int start, float[] buffer)
    {
        var len         = buffer.Length;
        var clipSamples = clip.samples;
        var tail        = clipSamples - start;

        if (len <= tail)
        {
            clip.GetData(buffer, start);
        }
        else
        {
            var tempTail = new float[tail];
            var tempHead = new float[len - tail];

            clip.GetData(tempTail, start);
            clip.GetData(tempHead, 0);

            Array.Copy(tempTail, 0, buffer, 0, tail);
            Array.Copy(tempHead, 0, buffer, tail, tempHead.Length);
        }
    }

    private static byte[] DownsampleAndConvert(float[] inBuf, int inRate, int outRate)
    {
        if (inRate == outRate) 
            return ConvertToPcm16(inBuf);

        var ratio   = (float)inRate / outRate;
        var outLen  = Mathf.RoundToInt(inBuf.Length / ratio);
        var pcmOut  = new byte[outLen * 2];

        var pos = 0f;
        for (var o = 0; o < outLen; o++, pos += ratio)
        {
            var i0 = Mathf.Clamp((int)pos, 0, inBuf.Length - 1);
            var i1 = Mathf.Min(i0 + 1, inBuf.Length - 1);
            var frac = pos - i0;

            var sample = Mathf.Lerp(inBuf[i0], inBuf[i1], frac);
            var s16 = (short)Mathf.Clamp(sample * 32767f, short.MinValue, short.MaxValue);

            pcmOut[o * 2]     = (byte)(s16 & 0xFF);
            pcmOut[o * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
        }

        return pcmOut;
    }

    private static byte[] ConvertToPcm16(float[] buf)
    {
        var pcm = new byte[buf.Length * 2];
        for (var i = 0; i < buf.Length; i++)
        {
            var s = (short)Mathf.Clamp(buf[i] * 32767f, short.MinValue, short.MaxValue);
            pcm[i * 2]     = (byte)(s & 0xFF);
            pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return pcm;
    }
}
