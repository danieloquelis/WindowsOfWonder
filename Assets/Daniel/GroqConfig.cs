using UnityEngine;

[CreateAssetMenu(fileName = "GroqConfig", menuName = "Groq/Config")]
public class GroqConfig : ScriptableObject
{
    [Header("Groq API Settings")]
    [Tooltip("API Key for RunPod")]
    public string apiKey;
}
