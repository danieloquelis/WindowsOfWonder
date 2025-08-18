using UnityEngine;
using UnityEngine.Assertions;

public class TagManager : MonoBehaviour
{
    private string _objectName;
    private GameManager _gameManager;
    
    private void Start()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
        Assert.IsNotNull(_gameManager, "GameManager not found in scene");
    }

    public void OnClicked()
    {
        StartCoroutine(_gameManager.OnObjectSelected(this));
    }

    public void SetObjectName(string objName)
    {
        _objectName = objName;
    }

    public string GetObjectName()
    {
        return _objectName;
    }
}
