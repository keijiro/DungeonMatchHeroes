using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class CreateGameOverScene
{
    [MenuItem("Tools/Create GameOver Scene")]
    public static void Create()
    {
        // Create new scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "GameOver";

        // Find assets
        VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameOver.uxml");
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/DefaultPanel.asset");
        if (panelSettings == null)
        {
            // Try searching by name if the path is wrong
            string[] guids = AssetDatabase.FindAssets("DefaultPanel t:PanelSettings");
            if (guids.Length > 0)
            {
                panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        // Create UI GameObject
        GameObject uiGO = new GameObject("GameOverUI");
        UIDocument uiDoc = uiGO.AddComponent<UIDocument>();
        uiDoc.visualTreeAsset = uxml;
        uiDoc.panelSettings = panelSettings;
        uiGO.AddComponent<GameOverScreenController>();

        // Save scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameOver.unity");
    }
}
