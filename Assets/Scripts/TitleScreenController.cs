using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string nextSceneName = "Main";

    private Label startMessage;
    private VisualElement blackout;
    private VisualElement root;
    private bool isStarting = false;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        startMessage = root.Q<Label>("start-message");
        blackout = root.Q("blackout");

        // Make root clickable to start the adventure
        root.RegisterCallback<PointerDownEvent>(OnRootClicked);

        // Start blinking effect
        StartCoroutine(BlinkMessage());
        
        // Trigger intro animation
        StartCoroutine(TriggerIntro());

        // Play Title SE after a short delay to ensure AudioManager is loaded
        StartCoroutine(PlayTitleSEWithDelay());
        }

        private IEnumerator PlayTitleSEWithDelay()
        {
        // Wait until AudioManager is available
        while (AudioManager.Instance == null)
        {
            yield return null;
        }

        // Additional small delay to be safe
        yield return new WaitForSeconds(0.2f);
        
        AudioManager.Instance.PlaySE(SEType.Title);
        }

        private IEnumerator TriggerIntro()
    {
        // Wait a bit to ensure the UI has been painted at least once in its intro state
        yield return new WaitForSeconds(0.1f);
        
        root.Q("background")?.RemoveFromClassList("background--intro");
        root.Q("heroes")?.RemoveFromClassList("heroes--intro");
        root.Q("heroes-shadow")?.RemoveFromClassList("heroes--intro");
        root.Q("logo")?.RemoveFromClassList("logo--intro");
        }

    private void OnDisable()
    {
        if (root != null)
        {
            root.UnregisterCallback<PointerDownEvent>(OnRootClicked);
        }
    }

    private IEnumerator BlinkMessage()
    {
        if (startMessage == null) yield break;

        while (true)
        {
            startMessage.ToggleInClassList("start-message--hidden");
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void OnRootClicked(PointerDownEvent evt)
    {
        if (isStarting) return;
        isStarting = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(SEType.Select);
        }

        StartCoroutine(StartAdventureSequence());
    }

    private IEnumerator StartAdventureSequence()
    {
        // Trigger blackout animation
        blackout?.AddToClassList("blackout--active");

        // Wait for the animation to complete (0.6s in USS)
        yield return new WaitForSeconds(0.7f);

        SceneManager.LoadScene(nextSceneName);
    }
    }
