using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

public class LLMManager : MonoBehaviour
{
    [SerializeField] private GroqRequestSender requestSender; // Assign in Inspector
    [SerializeField] private MultiAudioPlayer audioPlayer;    // Assign in Inspector
    [SerializeField] private Image imageObject; // Assign in Inspector
    [SerializeField] private string myObject;
    
    [TextArea]
    public string userInput;

    public UnityEvent onInferenceRunning;
    public UnityEvent<GroqResponseParser> onInferenceCompleted;
    
    private void Start()
    {
        // Correct sprite path: relative to Resources folder, no extension
        if (imageObject != null) 
            LoadSpriteIntoImage("Images/orange_moon", imageObject);
        else
            Debug.LogWarning("ImageObject is null");
        
        
        requestSender = GetComponent<GroqRequestSender>();
    }
    
    /// <summary>
    /// Start the game
    /// </summary>

    public void CallLLM(string selectedObject, List<string> detectedObjects, int chapter = 1)
    {
        userInput = requestSender.GetPrompt(chapter, selectedObject, detectedObjects);
        onInferenceRunning?.Invoke();
        StartCoroutine(RunLLMFlow(userInput));
    }

    public void CallLLMForEvaluatingUser(string userStory, List<string> availableObjects = null)
    {
        var prompt = requestSender.GetPromptForUserEvaluation(userStory, availableObjects);
        onInferenceRunning?.Invoke();
        StartCoroutine(RunLLMFlow(prompt));
    }
        
    /// <summary>
    /// Loads a sprite from the Resources folder and assigns it to the target Image component.
    /// </summary>
    public static void LoadSpriteIntoImage(string spriteName, Image targetImage)
    {
        if (targetImage == null)
        {
            Debug.LogWarning("Target Image is null.");
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(spriteName);
        if (sprite != null)
        {
            targetImage.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("Sprite not found at: " + spriteName);
        }
    }
    
    private IEnumerator RunLLMFlow(string prompt)
    {
        bool requestComplete = false;
        string responseJson = null;

        // Step 1: Send request and get JSON response
        /***
        requestSender.SendUserInput(prompt, (response) =>
        {
            responseJson = response;
            requestComplete = true;
        });
        ***/
        requestSender.GenericLLMRequest(prompt, (response) =>
        {
            responseJson = response;
            requestComplete = true;
        });


        // Wait for the request to complete
        yield return new WaitUntil(() => requestComplete);

        if (string.IsNullOrEmpty(responseJson))
        {
            Debug.LogError("No JSON received from Groq API.");
            yield break;
        }

        Debug.Log("Raw API Response: " + responseJson);

        // Step 2: Parse the response
        GroqResponseParser parser = new GroqResponseParser(responseJson);
        onInferenceCompleted?.Invoke(parser);
        // // Step 3: Handle lighting
        // string[] lightingColors = parser.GetLightingColors();
        // string lightingBrightness = parser.GetLightingBrightness();
        //
        // //ApplyLighting(lightingColors, lightingBrightness);
        //
        // // Step 4: Handle audio
        audioPlayer.PlayAll(parser);
    }

    private void ApplyLighting(string[] colorNames, string brightness)
    {
        // Find the main light in the scene
        Light sceneLight = FindFirstObjectByType<Light>();
        if (sceneLight == null)
        {
            Debug.LogWarning("No Light found in scene to apply lighting changes.");
            return;
        }

        // Try to parse first available color
        if (colorNames != null && colorNames.Length > 0 && !string.IsNullOrEmpty(colorNames[0]))
        {
            if (ColorUtility.TryParseHtmlString(colorNames[0], out Color parsedColor))
            {
                sceneLight.color = parsedColor;
            }
            else
            {
                Debug.LogWarning($"Invalid color format: {colorNames[0]}");
            }
        }

        // Apply brightness/intensity
        if (!string.IsNullOrEmpty(brightness))
        {
            switch (brightness.ToLower())
            {
                case "increase":
                    sceneLight.intensity = Mathf.Min(sceneLight.intensity * 1.5f, 8f);
                    break;
                case "decrease":
                    sceneLight.intensity = Mathf.Max(sceneLight.intensity * 0.5f, 0.1f);
                    break;
                case "maintain":
                default:
                    // Keep current intensity
                    break;
            }
        }
    }
}