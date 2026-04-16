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
        // Wait for one frame to ensure UI is ready
        yield return null;
        blackout?.AddToClassList("blackout--hidden");
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
        blackout?.RemoveFromClassList("blackout--hidden");

        // Wait for animation (uss duration is 0.8s)
        yield return new WaitForSeconds(1.0f);

        SceneManager.LoadScene(nextSceneName);
    }
}
