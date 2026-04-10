using Unity.AI.Assistant.UI.Editor.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GameDataCollection.Editor
{
    class SnapshottingSettingsWindow : EditorWindow
    {
        const string k_WindowName = "Snapshotting Settings";
        const string k_StyleSheetPath = GameDataCollectionConstants.UIModulePath + "Styles/SnapshottingSettingsView.uss";

        static readonly Vector2 k_MinSize = new(400, 300);

        SnapshottingSettingsView m_View;

        [MenuItem("AI Assistant/Internals/Data Collection/Snapshotting Settings")]
        public static void ShowWindow()
        {
            var editor = GetWindow<SnapshottingSettingsWindow>();
            editor.titleContent = new GUIContent(k_WindowName);
            editor.Show();
            editor.minSize = k_MinSize;
            editor.LoadThemeFromMainPackage();
        }

        void CreateGUI()
        {
            var window = AssistantWindow.FindExistingWindow() ?? EditorWindow.GetWindow<AssistantWindow>();
            var context = new AssistantUIContext(window.AssistantInstance);

            // Load the stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StyleSheetPath);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            m_View = new SnapshottingSettingsView();
            m_View.Initialize(context);
            m_View.style.flexGrow = 1;
            m_View.style.minWidth = 400;
            rootVisualElement.Add(m_View);
        }
    }
}
