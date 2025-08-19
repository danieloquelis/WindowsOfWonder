using UnityEngine;

public class SimpleEventTester : MonoBehaviour
{
    public void OnSphereTouch()
    {
        Debug.Log("Event Works! Sphere was touched!");
    }
}