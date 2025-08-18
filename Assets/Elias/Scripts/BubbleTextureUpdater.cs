using UnityEngine;
using UnityEngine.UI;

public class BubbleTextureUpdater : MonoBehaviour
{
    [Header("UI Source")]
    public RawImage sourceImage;   // Your passthrough feed is here

    [Header("Target Material")]
    public Material targetMaterial;
    public string textureProperty = "_SceneTex"; // Shader property name

    void Update()
    {
        if (sourceImage != null && sourceImage.texture != null && targetMaterial != null)
        {
            targetMaterial.SetTexture(textureProperty, sourceImage.texture);
        }
    }
}
