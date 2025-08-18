using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TTSManager ttsManager;
    [SerializeField] private LLMManager llmManager;
    [SerializeField] private ImageGen imageGenerator;
    
    [Header("UI")]
    [SerializeField] private GameObject loadingPrefab;
    
    private List<TagManager> _tagManagers = new();
    private TagManager selectedTagManager = null;
    
    private void Start()
    {
        ToggleLoading(false);
        ttsManager.onStartProcessing.AddListener(() =>
        {
            ToggleLoading(true);
        });
        llmManager.onInferenceRunning.AddListener(() =>
        {
            ToggleLoading(true);
        });
        imageGenerator.onGenerationCompleted.AddListener(OnImageGeneratedCompleted);
        
        llmManager.onInferenceCompleted.AddListener(llmResponse =>
        {
            ToggleLoading(false);
            var aiStory = llmResponse.GetStory();
            var imageGenPrompt = llmResponse.GetImagePrompt();
            
            imageGenerator.GenerateImage(imageGenPrompt);
            ttsManager.Speak(aiStory, () =>
            {
                ttsManager.Speak("Now, it is your turn. Tell me a story about this bottle");
            });
        });
    }

    private void OnImageGeneratedCompleted(Texture2D arg0)
    {
        Debug.Log("Image generated");
    }

    public IEnumerator OnObjectSelected(TagManager tagManager)
    {
        var objectName = tagManager.GetObjectName();
        _tagManagers = new List<TagManager>(FindObjectsByType<TagManager>(FindObjectsSortMode.None));
        selectedTagManager = tagManager;
        
        ttsManager.Speak($"Perfect!, you selected {objectName}. Let me see how creative I can be...", () =>
        {
            ToggleLoading(true);
            llmManager.CallLLM(objectName);
        });
        
        foreach (var tag in _tagManagers)
        {
            if (tag == tagManager) continue;
            
            tag.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void ToggleLoading(bool shouldShow)
    {
        loadingPrefab.SetActive(shouldShow);
    }
}
