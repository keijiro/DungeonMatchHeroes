using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SetupGameOverScene
{
    [MenuItem("Tools/Setup GameOver Scene")]
    public static void Setup()
    {
        // 1. Create the scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "GameOver";

        // Find assets
        VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameOver.uxml");
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/DefaultPanel.asset");
        if (panelSettings == null)
        {
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
        string scenePath = "Assets/Scenes/GameOver.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // 2. Add to Build Settings
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        
        // Check if already exists
        bool exists = false;
        foreach (var s in scenes)
        {
            if (s.path == scenePath)
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Added GameOver scene to Build Settings.");
        }

        Debug.Log("GameOver scene setup complete.");
    }
}
