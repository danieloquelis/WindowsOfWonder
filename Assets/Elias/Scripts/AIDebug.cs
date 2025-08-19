using UnityEngine;

public class AIDebug : MonoBehaviour
{
    public GroqRequestSender sender;
    string myobj = "Cup";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sender.SendUserInput(sender.GetPrompt(1, myobj), (response) =>
        {
            Debug.Log(response.ToString());
        }, myobj);
        
     }

    // Update is called once per frame
    void Update()
    {
        
    }
}
