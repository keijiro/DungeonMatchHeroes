using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class SceneTransitionController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private void Start()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument != null)
        {
            StartCoroutine(FadeIn());
        }
    }

    private IEnumerator FadeIn()
    {
        var root = uiDocument.rootVisualElement;
        var blackout = root.Q("blackout");

        if (blackout != null)
        {
            // 1. Make visible and closed
            blackout.AddToClassList("blackout--visible");
            blackout.AddToClassList("blackout--active");
            
            // 2. Wait a small amount of time to ensure layout/draw call is registered
            yield return new WaitForSeconds(0.1f);
            
            // 3. Start the transition by removing 'active' (scale 1 1 -> 1 0)
            // 'visible' class stays, so display: flex is maintained during animation
            blackout.RemoveFromClassList("blackout--active");

            // 4. Wait for the transition duration (0.8s in USS)
            yield return new WaitForSeconds(0.8f);

            // 5. Finally hide completely to free up processing/UI-picking
            blackout.RemoveFromClassList("blackout--visible");
        }
    }
}
