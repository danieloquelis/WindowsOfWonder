using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class TagManager : MonoBehaviour
{
    [SerializeField] private GameObject imagePlane;
    [SerializeField] private Material depthMaterial; // Your "DepthMat" with all the tuned values
    
    [Header("Recording Controls")]
    [SerializeField] private Toggle stopRecordingToggle;
    [SerializeField] private Canvas stopRecordingCanvas; // Separate canvas root for stop button

    private static readonly int BaseMapID  = Shader.PropertyToID("_BaseMap");
    private static readonly int DepthMapID = Shader.PropertyToID("_DepthMap");

    private string _objectName;
    private GameManager _gameManager;
    private STTManager _sttManager;
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        if (imagePlane) _renderer = imagePlane.GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        _sttManager = FindFirstObjectByType<STTManager>();
        
        Assert.IsNotNull(_gameManager, "GameManager not found in scene");
        Assert.IsNotNull(_sttManager, "STTManager not found in scene");
        Assert.IsNotNull(imagePlane, "imagePlane not set");
        Assert.IsNotNull(_renderer, "imagePlane needs a Renderer");
        Assert.IsNotNull(depthMaterial, "Assign your DepthMat material in the Inspector");

        // Use the shared material so we don't duplicate and we keep the Inspector values
        _renderer.sharedMaterial = depthMaterial;

        imagePlane.SetActive(false);
        
        // Initially hide the stop recording canvas
        if (stopRecordingCanvas) 
            stopRecordingCanvas.gameObject.SetActive(false);
        
        // Set up toggle listener and initial state
        if (stopRecordingToggle) 
        {
            stopRecordingToggle.onValueChanged.AddListener(OnStopRecordingToggled);
            stopRecordingToggle.isOn = false; // Start in off state
        }
    }

    public void OnClicked() => _gameManager.OnObjectSelected(this);
    public void SetObjectName(string objName) => _objectName = objName;
    public string GetObjectName() => _objectName;

    public void ShowImage(Texture2D colorTex, Texture2D depthTex)
    {
        if (!colorTex || !depthTex)
        {
            Debug.LogError("ShowImage requires both color and depth textures.");
            return;
        }

        // Only override the textures for this renderer (doesn't touch the material asset)
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetTexture(BaseMapID,  colorTex);
        _mpb.SetTexture(DepthMapID, depthTex);
        _renderer.SetPropertyBlock(_mpb);

        imagePlane.SetActive(true);
    }

    /// <summary>
    /// Show the stop recording toggle when user should record story about this object
    /// </summary>
    public void ShowStopRecordingButton()
    {
        if (stopRecordingCanvas)
        {
            stopRecordingCanvas.gameObject.SetActive(true);
            Debug.Log($"Showing stop recording canvas for {_objectName}");
        }
        
        if (stopRecordingToggle)
        {
            stopRecordingToggle.isOn = true; // Set to "recording" state
            Debug.Log($"Setting toggle to recording state for {_objectName}");
        }
    }

    /// <summary>
    /// Hide the stop recording toggle
    /// </summary>
    public void HideStopRecordingButton()
    {
        if (stopRecordingCanvas)
        {
            stopRecordingCanvas.gameObject.SetActive(false);
            Debug.Log($"Hiding stop recording canvas for {_objectName}");
        }
        
        if (stopRecordingToggle)
        {
            stopRecordingToggle.isOn = false; // Reset to off state
        }
    }

    /// <summary>
    /// Called when user toggles the stop recording toggle
    /// </summary>
    private void OnStopRecordingToggled(bool isOn)
    {
        Debug.Log($"Stop recording toggle changed to {isOn} for {_objectName}");
        
        if (!isOn) // User turned off the toggle = stop recording
        {
            // Stop the recording via STTManager
            if (_sttManager)
            {
                _sttManager.ForceStopRecording();
            }
            
            // Hide the canvas since recording is now stopped
            HideStopRecordingButton();
        }
        // Note: We don't handle isOn=true because that's set programmatically when recording starts
    }
}
