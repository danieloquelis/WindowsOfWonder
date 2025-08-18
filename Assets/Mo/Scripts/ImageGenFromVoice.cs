
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ImageGenFromVoice : MonoBehaviour
{
    [Header("API Settings")]
    public string apiKey = "YOUR_API_KEY"; // Enter your Flux 1.1 [Pro] API key here

    [Header("Scene Object")]
    public Renderer targetRenderer; // Assign your plane or curved mesh renderer here

    [Header("Prompt Settings")]
    [TextArea]
    public string prompt = "A futuristic city skyline at sunset with flying cars";

    [Header("Image Settings")]
    public int width = 1024;  // Make sure this is allowed by the API
    public int height = 1024; // Make sure this is allowed by the API
    public float pollInterval = 0.5f; // seconds between polling

    [ContextMenu("Generate Image")]
    public void GenerateImageFromPrompt(string str)
    {
        prompt = str;
        StartCoroutine(GenerateImageAsync());
    }

    private IEnumerator GenerateImageAsync()
    {
        // Step 1: Send generation request
        FluxRequest requestData = new FluxRequest(prompt, width, height);
        string jsonPayload = JsonUtility.ToJson(requestData);

        UnityWebRequest postRequest = new UnityWebRequest("https://api.bfl.ai/v1/flux-pro-1.1", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        postRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        postRequest.downloadHandler = new DownloadHandlerBuffer();
        postRequest.SetRequestHeader("Content-Type", "application/json");
        postRequest.SetRequestHeader("x-key", apiKey);

        yield return postRequest.SendWebRequest();

        if (postRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Request error: {postRequest.error}");
            yield break;
        }

        var response = JsonUtility.FromJson<FluxRequestResponse>(postRequest.downloadHandler.text);
        string pollingUrl = response.polling_url;

        // Step 2: Poll until image is ready
        bool ready = false;
        while (!ready)
        {
            yield return new WaitForSeconds(pollInterval);

            UnityWebRequest pollRequest = UnityWebRequest.Get(pollingUrl);
            pollRequest.SetRequestHeader("accept", "application/json");
            pollRequest.SetRequestHeader("x-key", apiKey);
            yield return pollRequest.SendWebRequest();

            if (pollRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Polling error: {pollRequest.error}");
                yield break;
            }

            var pollResult = JsonUtility.FromJson<FluxPollResponse>(pollRequest.downloadHandler.text);

            if (pollResult.status == "Ready")
            {
                // Treat sample as a URL, not Base64
                string imageUrl = pollResult.result.sample;
                UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(imageUrl);
                yield return textureRequest.SendWebRequest();

                if (textureRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Texture download failed: {textureRequest.error}");
                    yield break;
                }

                Texture2D tex = DownloadHandlerTexture.GetContent(textureRequest);
                targetRenderer.material.mainTexture = tex;
                ready = true;
            }
            else if (pollResult.status == "Error" || pollResult.status == "Failed")
            {
                Debug.LogError($"Generation failed: {pollRequest.downloadHandler.text}");
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
