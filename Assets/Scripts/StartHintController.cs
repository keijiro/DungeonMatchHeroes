using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class StartHintController : MonoBehaviour
{
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private float displayDuration = 4.0f;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start()
    {
        if (hudDocument == null) hudDocument = GetComponent<UIDocument>();
        if (hudDocument == null) return;

        StartCoroutine(ShowAndHideHint());
    }

    private IEnumerator ShowAndHideHint()
    {
        var root = hudDocument.rootVisualElement;
        var hint = root.Q<VisualElement>("start-hint");
        if (hint == null) yield break;

        // Ensure visible at start
        hint.RemoveFromClassList("start-hint--hidden");

        // Wait
        yield return new WaitForSeconds(displayDuration);

        // Add hidden class to trigger USS transition
        hint.AddToClassList("start-hint--hidden");

        // Wait for fade animation
        yield return new WaitForSeconds(fadeDuration);

        // Optionally remove from hierarchy
        hint.parent?.Remove(hint);
    }
}
