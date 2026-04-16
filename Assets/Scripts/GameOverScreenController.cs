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
    private Label lossMessage;
    private bool isTransitioning = false;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        blackout = root.Q("blackout");
        lossMessage = root.Q<Label>("loss-message");

        // Runtime check: Immediately make it black for the starting fade-in
        blackout?.AddToClassList("blackout--active");

        // Click to return to title
        root.RegisterCallback<PointerDownEvent>(OnRootClicked);

        // Start Fade-In
        StartCoroutine(FadeInSequence());
    }

    private void OnDisable()
    {
        if (root != null)
        {
            root.UnregisterCallback<PointerDownEvent>(OnRootClicked);
        }
    }

    private IEnumerator FadeInSequence()
    {
        // Wait a bit to ensure the UI has been painted at least once in its initial state.
        yield return new WaitForSeconds(0.1f);
        
        // Start Fade-In (0.8s transition)
        blackout?.RemoveFromClassList("blackout--active");
        
        // Start showing message earlier (e.g. 0.2s after fade start)
        yield return new WaitForSeconds(0.2f);
        
        // Show loss message with a scale-up animation
        lossMessage?.AddToClassList("loss-message--visible");
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
