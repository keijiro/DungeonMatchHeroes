using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIAssistantExtensions {

class AssistantImeProxyWindow : EditorWindow
{
    const string UxmlPath =
      "Packages/jp.keijiro.ai.assistant.extensions/Editor/AssistantImeProxy.uxml";

    const string StyleSheetPath =
      "Packages/jp.keijiro.ai.assistant.extensions/Editor/AssistantImeProxy.uss";

    VisualElement _placeholder;
    VisualElement _inputRoot;
    VisualElement _textFieldContainer;
    TextField _textField;

    [MenuItem("Window/AI/IME Proxy")]
    static void Open()
    {
        var window = GetWindow<AssistantImeProxyWindow>();
        window.titleContent = new GUIContent("IME Proxy");
    }

    static VisualTreeAsset _uxml;
    static StyleSheet _styleSheet;

    void CreateGUI()
    {
        _uxml ??= AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        _styleSheet ??= AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);

        rootVisualElement.Clear();
        if (_styleSheet != null && !rootVisualElement.styleSheets.Contains(_styleSheet))
            rootVisualElement.styleSheets.Add(_styleSheet);

        if (_uxml == null)
        {
            rootVisualElement.Add(new Label("Failed to load AssistantImeProxy.uxml"));
            return;
        }

        _uxml?.CloneTree(rootVisualElement);

        _placeholder = rootVisualElement.Q<VisualElement>("placeholder");
        _inputRoot = rootVisualElement.Q<VisualElement>("input-root");
        if (_placeholder == null || _inputRoot == null)
        {
            rootVisualElement.Clear();
            rootVisualElement.Add(new Label("Assistant IME Proxy UI is invalid."));
            return;
        }

        _placeholder.RegisterCallback<ClickEvent>(_ => ActivateTextField());
    }

    void ActivateTextField()
    {
        // Hide placeholder
        _placeholder.style.display = DisplayStyle.None;

        // Container with rounded corners and relative positioning for the hint label
        _textFieldContainer = new VisualElement();
        _textFieldContainer.AddToClassList("ime-input-container");

        // Create a fresh TextField each time — no stale cursor or IME state.
        _textField = new TextField { multiline = true };
        _textField.AddToClassList("ime-input-field");

        // Also round the inner TextInput element
        var textInput = _textField.Q("unity-text-input");
        if (textInput != null) textInput.AddToClassList("ime-input-field__input");

        _textField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        _textFieldContainer.Add(_textField);

        // Hint label overlaid at the bottom-right corner
        var hint = new Label("\u2318+Enter to send");
        hint.AddToClassList("ime-input-hint");
        hint.pickingMode = PickingMode.Ignore;
        _textFieldContainer.Add(hint);

        _inputRoot.style.display = DisplayStyle.Flex;
        _inputRoot.Add(_textFieldContainer);

        // Focus after the element is attached to the panel
        _textField.schedule.Execute(() => _textField.Focus());
    }

    void DeactivateTextField()
    {
        // Destroy the TextField entirely — all internal state is discarded.
        if (_textFieldContainer != null)
        {
            _textFieldContainer.RemoveFromHierarchy();
            _textFieldContainer = null;
            _textField = null;
        }

        // Show placeholder again
        _inputRoot.style.display = DisplayStyle.None;
        _placeholder.style.display = DisplayStyle.Flex;
    }

    void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
        if (!evt.commandKey) return; // Cmd+Enter to send

        var text = _textField.value?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        evt.StopImmediatePropagation();
#pragma warning disable CS0618
        evt.PreventDefault();
#pragma warning restore CS0618

        SendToAssistant(text);

        // Destroy the TextField after the current event processing completes.
        _textField.schedule.Execute(DeactivateTextField);
    }

    void SendToAssistant(string text)
    {
        // Find the AI Assistant window's chat input and action button.
        TextField chatInput = null;
        Button actionButton = null;

        foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            if (w == this) continue;
            var typeName = w.GetType().FullName;
            if (typeName == null || !typeName.Contains("Assistant")) continue;

            var root = w.rootVisualElement;
            chatInput = root?.Q<TextField>("input");
            actionButton = root?.Q<Button>("actionButton");

            if (chatInput != null && actionButton != null) break;
            chatInput = null;
            actionButton = null;
        }

        if (chatInput == null || actionButton == null) return;

        // Set the text on the chat input field.
        // This triggers OnTextFieldValueChanged -> OnChatValueChanged,
        // which saves the prompt and enables the submit button.
        chatInput.value = text;

        // Click the action button after the value change has propagated.
        // Using PointerUpEvent avoids the KeyDownEvent -> BubbleUp crash
        // path entirely.
        chatInput.schedule.Execute(() =>
        {
            var guiEvent = new Event
            {
                type = EventType.MouseUp,
                button = 0,
                mousePosition = actionButton.worldBound.center
            };
            using var ptrUp = PointerUpEvent.GetPooled(guiEvent);
            actionButton.SendEvent(ptrUp);
        });
    }
}

} // namespace AIAssistantExtensions
