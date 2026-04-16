using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string nextSceneName = "Title";

    private VisualElement blackout;
    private VisualElement root;
    private bool isTransitioning = false;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        blackout = root.Q("blackout");

        // Runtime check: Immediately make it black for the starting fade-in
        blackout?.AddToClassList("blackout--active");

        // Click to return to title
        root.RegisterCallback<PointerDownEvent>(OnRootClicked);

        // Start Fade-In
        StartCoroutine(FadeIn());
    }

    private void OnDisable()
    {
        if (root != null)
        {
            root.UnregisterCallback<PointerDownEvent>(OnRootClicked);
        }
    }

    private IEnumerator FadeIn()
    {
        // Wait a bit to ensure the UI has been painted at least once in its initial state.
        // This ensures the transition from opaque to transparent is correctly triggered.
        yield return new WaitForSeconds(0.1f);
        blackout?.RemoveFromClassList("blackout--active");
    }

    private void OnRootClicked(PointerDownEvent evt)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(SEType.Select);
        }

        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        // Trigger Fade-Out
        blackout?.AddToClassList("blackout--active");

        // Wait for animation (uss duration is 0.8s)
        yield return new WaitForSeconds(1.0f);

        SceneManager.LoadScene(nextSceneName);
    }
}
