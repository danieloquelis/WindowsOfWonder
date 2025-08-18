using System.Collections;
using System.Collections.Generic;
using PresentFutures.XRAI.Florence;
using UnityEngine;
using Utilities.Extensions;

public class GameManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Florence2Controller florence2Controller;
    [SerializeField] private TTSManager ttsManager;
    [SerializeField] private LLMManager llmManager;
    [SerializeField] private ImageGen imageGenerator;
    [SerializeField] private STTManager sttManager;
    [SerializeField] private DepthEstimationManager depthEstimationManager;
    
    [Header("UI")]
    [SerializeField] private GameObject loadingPrefab;

    private readonly List<string> _detectedObjectNames = new();
    private List<TagManager> _totalTagManagers = new();
    private readonly List<TagManager> _selectedTagManagers = new();
    private TagManager _currentSelectedTagManager = null;
    
    private void Start()
    {
        ToggleLoading(false);
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
                // Remove selected tag and make it null
                // Remove selected tag from the list as it was already spoken
                _totalTagManagers.Remove(_currentSelectedTagManager);
                var go = _currentSelectedTagManager.gameObject;
                go.Destroy();
                
                _currentSelectedTagManager = null;
                
                // Show all items
                StartCoroutine(ShowRemainingTags());
                
                Debug.Log($"Next question {llmResponse.GetNextQuestion()}");
                ttsManager.Speak(llmResponse.GetNextQuestion(), () =>
                {
                    // Assign _currentSelectedTagManager from the LLM
                    var newSelectedTagManager = _totalTagManagers.Find(it => it.GetObjectName() == llmResponse.GetSelectedObjectByAi());
                    _currentSelectedTagManager = newSelectedTagManager;
                    _selectedTagManagers.Add(newSelectedTagManager);
                    
                    // Hide elements -> we are spotting it as the user needs
                    StartCoroutine(HideRemainingTags());
                    
                    sttManager.OnStartRecording();
                    // Start bubble with stop button
                });
            });
        });

        florence2Controller.onInferenceCompleted.AddListener(entities =>
        {
            foreach (var entityName in entities.Labels)
            {
                _detectedObjectNames.Add(entityName);
            }
        });
        sttManager.onTranscribed.AddListener(OnUserStoryTranscription);
        
        depthEstimationManager.OnDepthMapReceived.AddListener(OnDepthMapReceived);
    }

    private void OnDepthMapReceived(Texture2D originalTexture, Texture2D depthTexture)
    {
        
    }

    private void OnUserStoryTranscription(string userStory)
    {
        // Call LLM with second prompt that evaluates story of user and says sth like
        // "Ah nice story involving it, I imagine something like this"
        // LLM also brings the audio and some small summary of the understading giving credit to user for beating AI
        
        // OnSpeechCompleted, show again all Tags and ask for picking
    }

    private void OnImageGeneratedCompleted(Texture2D image)
    {
        depthEstimationManager.ProcessImage(image);
    }

    public void OnObjectSelected(TagManager selectedTagManager)
    {
        var objectName = selectedTagManager.GetObjectName();
        // Only first time
        if (_totalTagManagers.Count == 0)
        {
            _totalTagManagers = new List<TagManager>(FindObjectsByType<TagManager>(FindObjectsSortMode.None));
        }
        else
        {
            StartCoroutine(ShowRemainingTags());
        }
        
        _currentSelectedTagManager = selectedTagManager;
        _selectedTagManagers.Add(selectedTagManager);
        
        ttsManager.Speak($"Perfect!, you selected {objectName}. Let me see how creative I can be...", () =>
        {
            ToggleLoading(true);
            llmManager.CallLLM(objectName, _detectedObjectNames);
        });

        StartCoroutine(HideRemainingTags());
    }

    public void ToggleLoading(bool shouldShow)
    {
        loadingPrefab.SetActive(shouldShow);
    }

    private IEnumerator ShowRemainingTags()
    {
        foreach (var tagManager in _totalTagManagers)
        {
            // It means we already used it for the game
            if (_selectedTagManagers.Contains(tagManager)) continue;
            
            tagManager.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator HideRemainingTags()
    {
        foreach (var tag in _totalTagManagers)
        {
            if (tag == _currentSelectedTagManager) continue;
            
            tag.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
