using UnityEngine;
using UnityEngine.Assertions;

public class TagManager : MonoBehaviour
{
    [SerializeField] private GameObject imagePlane;
    [SerializeField] private Material depthMaterial; // Your "DepthMat" with all the tuned values

    private static readonly int BaseMapID  = Shader.PropertyToID("_BaseMap");
    private static readonly int DepthMapID = Shader.PropertyToID("_DepthMap");

    private string _objectName;
    private GameManager _gameManager;
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
        Assert.IsNotNull(_gameManager, "GameManager not found in scene");
        Assert.IsNotNull(imagePlane, "imagePlane not set");
        Assert.IsNotNull(_renderer, "imagePlane needs a Renderer");
        Assert.IsNotNull(depthMaterial, "Assign your DepthMat material in the Inspector");

        // Use the shared material so we don't duplicate and we keep the Inspector values
        _renderer.sharedMaterial = depthMaterial;

        imagePlane.SetActive(false);
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
}
