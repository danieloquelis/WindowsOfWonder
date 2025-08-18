using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Diagnostics;

[Serializable]
public class DepthRequest
{
    public InputData input;
}

[Serializable]
public class InputData
{
    public string image;
    public int input_size = 384;
}

[Serializable]
public class DepthResponse
{
    public OutputData output;
}

[Serializable]
public class OutputData
{
    public string depth_map;
    public float inference_time_ms;
    public bool success;
}

public class DepthEstimationManager : MonoBehaviour
{
    [Header("RunPod Configuration")]
    [SerializeField] private string endpointId = "9dbl38zufl370w";
    [SerializeField] private RunPodConfig runPodConfig;
    
    [Header("Test Setup (Optional)")]
    public Texture2D testImage;
    public Button testButton;
    public RawImage resultDisplay;
    
    [Header("Events")]
    public UnityEvent OnLoadingStarted;
    public UnityEvent OnLoadingCompleted;
    public UnityEvent<Texture2D> OnDepthMapReceived;
    public UnityEvent<string> OnError;
    
    private string apiUrl => $"https://api.runpod.ai/v2/{endpointId}/runsync";
    
    void Start()
    {
        if (testButton != null)
            testButton.onClick.AddListener(TestDepthEstimation);
    }
    
    public void TestDepthEstimation()
    {
        if (testImage != null)
        {
            ProcessImage(testImage);
        }
        else
        {
            UnityEngine.Debug.LogError("No test image assigned!");
            OnError?.Invoke("No test image assigned");
        }
    }
    
    public void ProcessImage(Texture2D inputImage)
    {
        if (inputImage == null)
        {
            OnError?.Invoke("Input image is null");
            return;
        }
        
        StartCoroutine(ProcessImageCoroutine(inputImage));
    }
    
    public void ProcessImageFromBase64(string base64Image)
    {
        StartCoroutine(ProcessBase64Coroutine(base64Image));
    }
    
    private IEnumerator ProcessImageCoroutine(Texture2D inputImage)
    {
        OnLoadingStarted?.Invoke();

        var sw = Stopwatch.StartNew();
        
        // Convert to base64
        UnityEngine.Debug.Log("[Timer] Starting TextureToBase64...");
        var base64Image = TextureToBase64(inputImage);
        UnityEngine.Debug.Log($"[Timer] TextureToBase64 took {sw.ElapsedMilliseconds} ms");

        yield return ProcessBase64Coroutine(base64Image, sw);
    }
    
    private IEnumerator ProcessBase64Coroutine(string base64Image, Stopwatch sw = null)
    {
        if (string.IsNullOrEmpty(base64Image))
        {
            OnError?.Invoke("Failed to convert image to base64");
            OnLoadingCompleted?.Invoke();
            yield break;
        }
        
        sw ??= Stopwatch.StartNew();
        
        // Create request payload
        UnityEngine.Debug.Log("[Timer] Creating JSON payload...");
        var request = new DepthRequest
        {
            input = new InputData
            {
                image = base64Image,
                input_size = 384
            }
        };
        
        var jsonPayload = JsonConvert.SerializeObject(request);
        var bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        UnityEngine.Debug.Log($"[Timer] JSON serialization took {sw.ElapsedMilliseconds} ms total");

        // Create UnityWebRequest
        using (UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {runPodConfig.apiKey}");
            webRequest.timeout = 120;
            
            UnityEngine.Debug.Log("[Timer] Sending depth estimation request...");
            sw.Restart();
            
            // Send request
            yield return webRequest.SendWebRequest();
            
            UnityEngine.Debug.Log($"[Timer] HTTP request took {sw.ElapsedMilliseconds} ms");
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    sw.Restart();
                    var responseText = webRequest.downloadHandler.text;
                    var response = JsonConvert.DeserializeObject<DepthResponse>(responseText);
                    UnityEngine.Debug.Log($"[Timer] JSON parse took {sw.ElapsedMilliseconds} ms");

                    if (response?.output?.success == true && !string.IsNullOrEmpty(response.output.depth_map))
                    {
                        sw.Restart();
                        // Convert base64 back to texture
                        Texture2D depthTexture = Base64ToTexture(response.output.depth_map);
                        UnityEngine.Debug.Log($"[Timer] Base64→Texture2D took {sw.ElapsedMilliseconds} ms");

                        if (depthTexture != null)
                        {
                            UnityEngine.Debug.Log($"Depth estimation completed in {response.output.inference_time_ms}ms (server reported).");
                            
                            // Display result
                            if (resultDisplay != null)
                                resultDisplay.texture = depthTexture;
                            
                            OnDepthMapReceived?.Invoke(depthTexture);
                        }
                        else
                        {
                            OnError?.Invoke("Failed to convert depth map to texture");
                        }
                    }
                    else
                    {
                        var errorMsg = response?.output?.success == false ? "Server processing failed" : "Invalid response format";
                        OnError?.Invoke(errorMsg);
                        UnityEngine.Debug.LogError($"API Error: {errorMsg}");
                    }
                }
                catch (Exception e)
                {
                    OnError?.Invoke($"JSON parsing error: {e.Message}");
                    UnityEngine.Debug.LogError($"JSON Error: {e.Message}");
                }
            }
            else
            {
                var errorMsg = $"Request failed: {webRequest.error}";
                OnError?.Invoke(errorMsg);
                UnityEngine.Debug.LogError(errorMsg);
            }
        }
        
        OnLoadingCompleted?.Invoke();
    }
    
    private string TextureToBase64(Texture2D texture)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var readableTexture = texture;
            
            // Check if texture is readable and uncompressed
            if (!texture.isReadable || texture.format == TextureFormat.DXT1 || texture.format == TextureFormat.DXT5)
            {
                UnityEngine.Debug.Log("[Timer] Making texture readable/uncompressed...");
                var renderTex = RenderTexture.GetTemporary(texture.width, texture.height);
                Graphics.Blit(texture, renderTex);
                
                var previous = RenderTexture.active;
                RenderTexture.active = renderTex;
                
                readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                readableTexture.Apply();
                
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTex);
            }
            
            var imageBytes = readableTexture.EncodeToPNG();
            UnityEngine.Debug.Log($"[Timer] EncodeToPNG took {sw.ElapsedMilliseconds} ms");

            // Clean up if we created a copy
            if (readableTexture != texture)
            {
                DestroyImmediate(readableTexture);
            }
            
            return Convert.ToBase64String(imageBytes);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to convert texture to base64: {e.Message}");
            return null;
        }
    }
    
    private Texture2D Base64ToTexture(string base64)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var imageBytes = Convert.FromBase64String(base64);
            UnityEngine.Debug.Log($"[Timer] Base64 decode to bytes took {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageBytes))
            {
                UnityEngine.Debug.Log($"[Timer] Texture2D.LoadImage took {sw.ElapsedMilliseconds} ms");
                return texture;
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to load image from bytes");
                return null;
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to convert base64 to texture: {e.Message}");
            return null;
        }
    }
}
