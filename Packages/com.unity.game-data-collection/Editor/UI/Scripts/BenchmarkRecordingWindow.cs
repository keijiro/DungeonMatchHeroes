using Unity.AI.Assistant.UI.Editor.Scripts;
using UnityEditor;
using UnityEngine;

namespace Unity.GameDataCollection.Editor
{
    class BenchmarkRecordingWindow : EditorWindow
    {
        const string k_WindowName = "Benchmark Recording";

        static readonly Vector2 k_MinSize = new(500, 250);

        BenchmarkRecordingView m_View;

        [MenuItem("AI Assistant/Internals/Data Collection/Benchmark Recording")]
        public static void ShowWindow()
        {
            var editor = GetWindow<BenchmarkRecordingWindow>();
            editor.titleContent = new GUIContent(k_WindowName);
            editor.Show();
            editor.minSize = k_MinSize;
            editor.LoadThemeFromMainPackage();
        }

        void CreateGUI()
        {
            var window = AssistantWindow.FindExistingWindow() ?? EditorWindow.GetWindow<AssistantWindow>();
            var context = new AssistantUIContext(window.AssistantInstance);

            m_View = new BenchmarkRecordingView();
            m_View.Initialize(context);
            m_View.style.flexGrow = 1;
            m_View.style.minWidth = 400;
            rootVisualElement.Add(m_View);
        }
    }
}

