using UnityEngine;
using UnityEngine.SceneManagement; // Needed for scene loading

public class SceneChanger : MonoBehaviour
{
    // Load scene by name
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        
    }

    // Load scene by index
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
