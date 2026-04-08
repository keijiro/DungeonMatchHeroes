using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIAssistantExtensions {

[InitializeOnLoad]
static class AssistantFontModifier
{
    const string OverrideStylePath =
      "Packages/jp.keijiro.ai.assistant.extensions/Editor/AssistantFontOverride.uss";

    const string TargetWindowTypeName =
      "Unity.AI.Assistant.UI.Editor.Scripts.AssistantWindow";

    const string TargetRootElementName = "root-panel";

    //
    // Constructor and event handlers
    //

    static AssistantFontModifier()
    {
        // Subscribe to window focus change events
        EditorWindow.windowFocusChanged += OnWindowFocusChanged;

        // Double delayCall to wait for Assistant UI to be fully constructed after domain reload
        EditorApplication.delayCall += () =>
          { EditorApplication.delayCall += () => ApplyToAllOpenWindows(); };
    }

    static void OnWindowFocusChanged()
      => EditorApplication.delayCall += () =>
           CheckAndApplyCustomFont(EditorWindow.focusedWindow);

    static void ApplyToAllOpenWindows()
    {
        foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            CheckAndApplyCustomFont(window);
    }

    //
    // Font application logic
    //

    static StyleSheet _styleSheet;

    static bool CheckWindowType(EditorWindow window)
      => window?.GetType()?.FullName == TargetWindowTypeName;

    static void ApplyCustomFont(EditorWindow window)
    {
        var root = window.rootVisualElement;
        var target = root?.Q<VisualElement>(TargetRootElementName);
        if (target == null) return;

        if (_styleSheet == null)
            _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(OverrideStylePath);

        if (!target.styleSheets.Contains(_styleSheet))
            target.styleSheets.Add(_styleSheet);
    }

    static void CheckAndApplyCustomFont(EditorWindow window)
    {
        if (CheckWindowType(window)) ApplyCustomFont(window);
    }
}

} // namespace AIAssistantExtensions
