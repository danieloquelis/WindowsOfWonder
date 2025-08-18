using UnityEngine;
using UnityEngine.EventSystems;

public enum ClickAction
{
    StartSpeaking,
    StopSpeaking,
    ExitGame,
    RepeatGame,
    TriggerAnimation,
    CustomAction
}

public class ClickableObject : MonoBehaviour, IPointerClickHandler
{
    [Header("Click Action Settings")]
    public ClickAction actionType;
    public string customMessage = "Default action";
    
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"{gameObject.name} was clicked!");
        
        // Execute the specific action based on the selected type
        ExecuteAction();
    }
    
    private void ExecuteAction()
    {
        switch (actionType)
        {
            case ClickAction.StartSpeaking:
                StartSpeaking();
                break;
            case ClickAction.StopSpeaking:
                StopSpeaking();
                break;
            case ClickAction.ExitGame:
                ExitGame();
                break;
            case ClickAction.RepeatGame:
                RepeatGame();
                break;
            case ClickAction.TriggerAnimation:
                TriggerAnimation();
                break;
            case ClickAction.CustomAction:
                CustomAction();
                break;
        }
    }
    
    // Specific action methods
    private void StartSpeaking()
    {
        Debug.Log("Please start speaking");
        // Add door opening logic here
    }
    
    private void StopSpeaking()
    {
        Debug.Log("Please stop speaking");
        // Add chest opening logic here
    }
    
    private void ExitGame()
    {
        Debug.Log("Button pressed!");
        // Add button press logic here
    }
    
    private void RepeatGame()
    {
        Debug.Log("Item picked up!");
        // Add pickup logic here
    }
    
    private void TriggerAnimation()
    {
        Debug.Log("Playing animation...");
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("PlayAnimation");
        }
    }
    
    private void CustomAction()
    {
        Debug.Log($"Custom action: {customMessage}");
        // Use the customMessage field for flexible actions
    }
}
