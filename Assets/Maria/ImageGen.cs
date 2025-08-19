using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageGen : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string apiKey = "YOUR_API_KEY";
    
    [Header("Image Settings")]
    [SerializeField] private int width = 640; 
    [SerializeField] private int height = 480;
    [SerializeField] private float pollInterval = 0.3f;
    
    [Header("Test only (Optional)")]
    [SerializeField] private RawImage targetRawImage;
    [TextArea][SerializeField] private string testPrompt = "A futuristic city skyline at sunset with flying cars";

    [Header("Events")] 
    public UnityEvent onGenerating;
    public UnityEvent<Texture2D> onGenerationCompleted;
    public UnityEvent<string> onGenerationFailed;
    
    public void GenerateImage(string prompt)
    {
        Debug.Log($"[ImageGen] GenerateImage called with prompt: '{prompt}'");
        Debug.Log($"[ImageGen] Starting GenerateImageAsync coroutine");
        StartCoroutine(GenerateImageAsync(prompt));
    }
    
    public void GenerateTestImage()
    {
        StartCoroutine(GenerateImageAsync(testPrompt));
    }

    private IEnumerator GenerateImageAsync(string prompt)
    {
        Debug.Log($"[ImageGen] GenerateImageAsync started with prompt: '{prompt}'");
        var requestData = new FluxRequest(prompt, width, height);
        var jsonPayload = JsonUtility.ToJson(requestData);
        Debug.Log($"[ImageGen] Created request JSON: {jsonPayload}");

        var postRequest = new UnityWebRequest("https://api.bfl.ai/v1/flux-pro-1.1", "POST");
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        postRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        postRequest.downloadHandler = new DownloadHandlerBuffer();
        postRequest.SetRequestHeader("Content-Type", "application/json");
        postRequest.SetRequestHeader("x-key", apiKey);
        
        onGenerating?.Invoke();
        
        yield return postRequest.SendWebRequest();

        if (postRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Request error: {postRequest.error}");
            onGenerationFailed?.Invoke(postRequest.error);
            yield break;
        }

        var response = JsonUtility.FromJson<FluxRequestResponse>(postRequest.downloadHandler.text);
        var pollingUrl = response.polling_url;

        var ready = false;
        while (!ready)
        {
            yield return new WaitForSeconds(pollInterval);

            var pollRequest = UnityWebRequest.Get(pollingUrl);
            pollRequest.SetRequestHeader("accept", "application/json");
            pollRequest.SetRequestHeader("x-key", apiKey);
            yield return pollRequest.SendWebRequest();

            if (pollRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Polling error: {pollRequest.error}");
                onGenerationFailed?.Invoke(pollRequest.error);
                yield break;
            }

            var pollResult = JsonUtility.FromJson<FluxPollResponse>(pollRequest.downloadHandler.text);

            if (pollResult.status == "Ready")
            {
                var imageUrl = pollResult.result.sample;
                var textureRequest = UnityWebRequestTexture.GetTexture(imageUrl);
                yield return textureRequest.SendWebRequest();

                if (textureRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Texture download failed: {textureRequest.error}");
                    onGenerationFailed?.Invoke(textureRequest.error);
                    yield break;
                }
                
                var tex = DownloadHandlerTexture.GetContent(textureRequest);
                if (targetRawImage)
                {
                    targetRawImage.texture = tex;
                    targetRawImage.enabled = true;
                }
                
                Debug.Log($"[ImageGen] Image generation completed successfully, invoking onGenerationCompleted event");
                onGenerationCompleted?.Invoke(tex);
                ready = true;
            }
            else if (pollResult.status == "Error" || pollResult.status == "Failed")
            {
                Debug.LogError($"Generation failed: {pollRequest.downloadHandler.text}");
                onGenerationFailed?.Invoke(pollRequest.error);
                yield break;
            }
            else
            {
                Debug.Log($"Status: {pollResult.status}");
            }
        }
    }

    [System.Serializable]
    public class FluxRequest
    {
        public string prompt;
        public int width;
        public int height;

        public FluxRequest(string prompt, int width, int height)
        {
            this.prompt = prompt;
            this.width = width;
            this.height = height;
        }
    }

    [System.Serializable]
    private class FluxRequestResponse
    {
        public string id;
        public string polling_url;
    }

    [System.Serializable]
    private class FluxPollResponse
    {
        public string status;
        public ResultData result;

        [System.Serializable]
        public class ResultData
        {
            public string sample; // Now treated as URL
        }
    }
}
