using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class StartHintController : MonoBehaviour
{
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float oscillationAmplitude = 15.0f;
    [SerializeField] private float oscillationSpeed = 3.0f;

    private VisualElement hintContainer;
    private bool isHiding = false;
    private bool hasBeenClicked = false;
    private const float MinimumDisplayTime = 10.0f;

    private void Start()
    {
        if (hudDocument == null) hudDocument = GetComponent<UIDocument>();
        if (hudDocument == null) return;

        var root = hudDocument.rootVisualElement;
        hintContainer = root.Q<VisualElement>("hint-container");
        var hintLabel = root.Q<VisualElement>("start-hint");

        var gridManager = Object.FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            gridManager.OnBottomRowClicked += () => hasBeenClicked = true;
        }

        if (hintContainer != null && hintLabel != null)
        {
            hintContainer.RemoveFromClassList("hint-container--hidden");
            StartCoroutine(HideTimer());
        }
    }

    private void Update()
    {
        if (hintContainer == null || isHiding) return;

        // Floating effect: abs(sin(t)) to move upward
        float offset = Mathf.Abs(Mathf.Sin(Time.time * oscillationSpeed)) * oscillationAmplitude;
        
        // Horizontal: -50% (centered on the 30% left anchor), Vertical: -offset pixels
        hintContainer.style.translate = new Translate(Length.Percent(-50), new Length(-offset));
    }

    private IEnumerator HideTimer()
    {
        // Keep showing while: (time < 10s) OR (not clicked)
        while (Time.time < MinimumDisplayTime || !hasBeenClicked)
        {
            yield return null;
        }
        
        isHiding = true;
        hintContainer.AddToClassList("hint-container--hidden");

        yield return new WaitForSeconds(fadeDuration);
        hintContainer?.parent?.Remove(hintContainer);
        hintContainer = null;
    }
    }
