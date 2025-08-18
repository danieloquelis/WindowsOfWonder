using System.Threading.Tasks;
using System.Collections;
using OpenAI;
using OpenAI.Audio;
using System;
using UnityEngine;

public class OpenAIManager
{
    private static OpenAIClient _openAIClient;

    private static OpenAIClient GetOpenAIClient()
    {
        if (_openAIClient != null)
        {
            return _openAIClient;
        }
        return new OpenAIClient(Resources.Load<OpenAIConfiguration>("OpenAIConfiguration"));
    }

    public static IEnumerator TTSCoroutine(string text, string model, string voice, string instructions, Action<AudioClip> onComplete)
    {
        AudioClip result = null;

        SpeechRequest request = new(text, model, voice, instructions);
        Task<SpeechClip> speechClip = GetOpenAIClient().AudioEndpoint.GetSpeechAsync(request);

        while (!speechClip.IsCompleted) yield return null;

        if (speechClip.Exception != null)
        {
            Debug.LogError($"Error in TTSOpenAI.ExecuteCoroutine: {speechClip.Exception.Message}");
        }
        else
        {
            result = speechClip.Result.AudioClip;
        }

        onComplete?.Invoke(result);
    }

    public static IEnumerator STTCoroutine(AudioClip audioClip, Action<string> onComplete)
    {
        var request = new AudioTranscriptionRequest(audioClip, language: "en");
        var transcription = GetOpenAIClient().AudioEndpoint.CreateTranscriptionTextAsync(request);
        
        while (!transcription.IsCompleted) yield return null;
        if (transcription.Exception != null)
        {
            Debug.LogError($"Error in TTSOpenAI.ExecuteCoroutine: {transcription.Exception.Message}");
        }
        
        onComplete?.Invoke(transcription.Result);
    }
}