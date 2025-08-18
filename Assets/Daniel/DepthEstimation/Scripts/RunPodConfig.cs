using UnityEngine;

[CreateAssetMenu(fileName = "RunPodConfig", menuName = "RunPod/Config")]
public class RunPodConfig : ScriptableObject
{
    [Header("RunPod API Settings")]
    [Tooltip("API Key for RunPod")]
    public string apiKey;
}
