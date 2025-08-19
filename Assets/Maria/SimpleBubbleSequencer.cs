using UnityEngine;
using System.Collections;

public class SimpleBubbleSequencer : MonoBehaviour
{
    [Header("Settings")]
    public float delayBetweenBubbles = 0.5f;
    public bool startOnEnable = true;
    
    [Header("Growth Animation")]
    public float growthDuration = 0.3f;

    private Vector3[] originalScales;

    void Start()
    {
        // Store the original scales of all children
        StoreOriginalScales();
        
        if (startOnEnable)
        {
            StartSequence();
        }
    }

    private void StoreOriginalScales()
    {
        originalScales = new Vector3[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            originalScales[i] = transform.GetChild(i).localScale;
        }
    }

    public void StartSequence()
    {
        StartCoroutine(ShowBubblesOneByOne());
    }

    private IEnumerator ShowBubblesOneByOne()
    {
        // Start all children at zero scale
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.gameObject.SetActive(true);
            child.localScale = Vector3.zero;
        }

        // Grow each child one by one
        for (int i = 0; i < transform.childCount; i++)
        {
            yield return StartCoroutine(GrowChild(i));
            yield return new WaitForSeconds(delayBetweenBubbles);
        }
    }

    private IEnumerator GrowChild(int childIndex)
    {
        Transform child = transform.GetChild(childIndex);
        Vector3 targetScale = originalScales[childIndex];
        
        float elapsedTime = 0f;
        
        while (elapsedTime < growthDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / growthDuration;
            
            // Smooth growth curve
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            child.localScale = Vector3.Lerp(Vector3.zero, targetScale, smoothProgress);
            
            yield return null;
        }
        
        // Ensure final scale is exact
        child.localScale = targetScale;
    }

    public void HideAll()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void ShowAll()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.gameObject.SetActive(true);
            child.localScale = originalScales[i];
        }
    }
}
