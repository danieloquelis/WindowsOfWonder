using PresentFutures.XRAI.Florence;
using UnityEngine;
using UnityEngine.Assertions;

public class ObjectDetectionManager : MonoBehaviour
{
    private Florence2Controller _florence2Controller;
    
    private void Start()
    {
        _florence2Controller = GetComponent<Florence2Controller>();
        Assert.IsNotNull(_florence2Controller, "_florence2Controller is not present in scene");
    }

    public void StartObjectDetection()
    {
        Debug.Log("StartObjectDetection...");
        _florence2Controller.SendRequest();
    }
}
