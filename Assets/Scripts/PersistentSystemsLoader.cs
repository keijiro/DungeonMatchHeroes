using UnityEngine;
using UnityEngine.SceneManagement;

public static class PersistentSystemsLoader
{
    private const string SceneName = "PersistentSystems";

    // This method is called automatically before the first scene is loaded.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadSystems()
    {
        // Check if the scene is already loaded (to avoid duplicates in Editor if the scene is manually loaded)
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == SceneName) return;
        }

        // Load the scene additively. 
        // Objects inside (like AudioManager) should have DontDestroyOnLoad to survive 
        // when the primary scene is loaded in Single mode.
        SceneManager.LoadScene(SceneName, LoadSceneMode.Additive);
    }
}
