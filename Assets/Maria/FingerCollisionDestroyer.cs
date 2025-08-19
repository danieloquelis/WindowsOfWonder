using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class FingerCollisionDestroyer : MonoBehaviour
{
    [Header("Destruction Target")]
    public GameObject objectToDestroy;
    
    [Header("Growth Animation")]
    public float growthScale = 1.2f;
    public float growthDuration = 0.2f;
    
    [Header("Audio")]
    public AudioClip popSound;
    public float volume = 1f;
    public float audioDelay = 0.5f;
    
    [Header("Events")]
    public UnityEvent OnFingerCollision;

    private Vector3 originalScale;
    private bool hasTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        // Store the original scale
        originalScale = transform.localScale;
        
        // Force create AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log($"Added AudioSource to {gameObject.name}");
        }
        
        // Configure AudioSource for reliable playback
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f; // 2D audio, not 3D
        audioSource.priority = 128;
        
        Debug.Log($"FingerCollisionDestroyer setup complete on {gameObject.name}");
        Debug.Log($"Pop sound assigned: {(popSound != null ? popSound.name : "NONE")}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"TRIGGER ENTER: {other.gameObject.name} (layer: {other.gameObject.layer}) touched {gameObject.name}");
        
        // Check if the colliding object is on the "Finger" layer and hasn't triggered yet
        if (other.gameObject.layer == LayerMask.NameToLayer("Finger") && !hasTriggered)
        {
            hasTriggered = true;
            Debug.Log($"FINGER COLLISION CONFIRMED on {gameObject.name}!");
            StartCoroutine(GrowAndDestroy());
        }
        else
        {
            Debug.Log($"Not finger layer or already triggered. Expected Finger layer: {LayerMask.NameToLayer("Finger")}, Got: {other.gameObject.layer}");
        }
    }

    private IEnumerator GrowAndDestroy()
    {
        Debug.Log("=== STARTING GROW AND DESTROY SEQUENCE ===");
        
        // Play pop sound with multiple fallback methods
        if (popSound != null && audioSource != null)
        {
            Debug.Log("Playing pop sound...");
            
            // Method 1: Try PlayOneShot
            audioSource.PlayOneShot(popSound, volume);
            
            // Method 2: Also try direct play as backup
            audioSource.clip = popSound;
            audioSource.Play();
            
            Debug.Log($"Pop sound played! Duration: {popSound.length} seconds");
        }
        else
        {
            Debug.LogError($"Cannot play sound - popSound: {(popSound != null ? "OK" : "NULL")}, audioSource: {(audioSource != null ? "OK" : "NULL")}");
        }
        
        // Trigger the event
        try
        {
            OnFingerCollision?.Invoke();
            Debug.Log("Unity Event triggered successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Unity Event: {e.Message}");
        }
        
        // Growth animation
        Vector3 targetScale = originalScale * growthScale;
        float elapsedTime = 0f;
        
        Debug.Log("Starting growth animation...");
        while (elapsedTime < growthDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / growthDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, smoothProgress);
            yield return null;
        }
        
        transform.localScale = targetScale;
        Debug.Log("Growth animation completed");
        
        // Wait for audio to finish
        Debug.Log($"Waiting {audioDelay} seconds for audio...");
        yield return new WaitForSeconds(audioDelay);
        
        // Destroy the target object
        if (objectToDestroy != null)
        {
            Debug.Log($"DESTROYING target object: {objectToDestroy.name}");
            Destroy(objectToDestroy);
        }
        else
        {
            Debug.LogWarning("No object to destroy assigned!");
        }
    }

    // Test method you can call from inspector
    [ContextMenu("Test Audio")]
    public void TestAudio()
    {
        if (popSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(popSound);
            Debug.Log("Test audio played!");
        }
        else
        {
            Debug.LogError("Cannot test audio - missing components or audio clip");
        }
    }
}
