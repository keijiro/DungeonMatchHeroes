using Unity.AI.Assistant.UI.Editor.Scripts;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.GameDataCollection.Editor
{
    static class GameDataCollectionUIHelpers
    {
        const string k_CommonMuseTheme = AssistantUIConstants.UIStylePath + AssistantUIConstants.AssistantBaseStyle;

        static readonly string k_SkinTheme = EditorGUIUtility.isProSkin
            ? AssistantUIConstants.UIStylePath + AssistantUIConstants.AssistantSharedStyleDark + ".uss"
            : AssistantUIConstants.UIStylePath + AssistantUIConstants.AssistantSharedStyleLight + ".uss";

        internal static void LoadThemeFromMainPackage(this EditorWindow window)
        {
            var element = window.rootVisualElement;
            element.styleSheets.Add(Load<StyleSheet>(k_SkinTheme));
            element.styleSheets.Add(Load<StyleSheet>(k_CommonMuseTheme));
        }

        static T Load<T>(string path)
            where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}

