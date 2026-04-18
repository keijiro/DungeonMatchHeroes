using UnityEngine;

public class RowHighlighter : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Material pulseMaterial;
    [SerializeField] private Color pulseColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.5f;

    private SpriteRenderer[] highlightRenderers = new SpriteRenderer[GridManager.GridWidth];
private static readonly int PulseColorID = Shader.PropertyToID("_PulseColor");

    private bool hasBeenClicked = false;
    private const float MinimumDisplayTime = 10.0f;

    private void Start()
    {
        if (gridManager == null) gridManager = GetComponent<GridManager>();

        if (gridManager != null)
        {
            gridManager.OnBottomRowClicked += () => hasBeenClicked = true;
        }
        
        // Create a separate container that is NOT a child of this.transform (GridManager)
// to avoid being destroyed by GridManager.InitializeGrid()
        GameObject container = new GameObject("HighlightContainer");
        // No parent assignment or choose a different parent if necessary

        // Pre-create highlight objects for each column
        for (int x = 0; x < GridManager.GridWidth; x++)
        {
            GameObject obj = new GameObject($"Highlight_{x}");
            obj.transform.parent = container.transform;
            
            highlightRenderers[x] = obj.AddComponent<SpriteRenderer>();
            highlightRenderers[x].material = pulseMaterial;
            highlightRenderers[x].sortingOrder = 5; // Top layer
            highlightRenderers[x].enabled = false;
        }
    }

    private void Update()
    {
        if (gridManager == null) return;

        bool isTutorial = (Time.time < MinimumDisplayTime) || !hasBeenClicked;

        float alpha = 0;

        if (isTutorial)
        {
            // [Tutorial: Strong] Custom pulse pattern: 1s wait, then two quick flashes
            // Cycle: Wait(1.0s) -> Flash(0.2s) -> Gap(0.1s) -> Flash(0.2s) = 1.5s total
            float cycleTime = 1.5f;
            float timeInCycle = Time.time % cycleTime;

            if (timeInCycle > 1.0f)
            {
                float flashPhase = timeInCycle - 1.0f; // 0.0 to 0.5
                if (flashPhase < 0.2f)
                {
                    // First flash: 0.0 to 0.2
                    alpha = Mathf.Sin((flashPhase / 0.2f) * Mathf.PI) * maxAlpha;
                }
                else if (flashPhase > 0.3f && flashPhase < 0.5f)
                {
                    // Second flash: 0.3 to 0.5
                    alpha = Mathf.Sin(((flashPhase - 0.3f) / 0.2f) * Mathf.PI) * maxAlpha;
                }
            }
        }
        else
        {
            // [Normal: Weak] 3.0s cycle, 1 slow flash, lower alpha (40% of maxAlpha)
            float cycleTime = 3.0f;
            float timeInCycle = Time.time % cycleTime;
            float flashDuration = 1.0f;
            float flashStart = 2.0f; // 2 seconds wait, then 1 second flash

            if (timeInCycle > flashStart)
            {
                float flashPhase = timeInCycle - flashStart;
                alpha = Mathf.Sin((flashPhase / flashDuration) * Mathf.PI) * (maxAlpha * 0.4f);
            }
        }

        Color c = pulseColor;
        c.a = alpha;

        for (int x = 0; x < GridManager.GridWidth; x++)
{
            SpriteRenderer targetRenderer = gridManager.GetRenderer(x, 0);
            SpriteRenderer highlightSR = highlightRenderers[x];

            // Safety check in case objects were destroyed
            if (highlightSR == null) continue;
            
            if (targetRenderer != null && targetRenderer.gameObject.activeInHierarchy)
            {
                highlightSR.enabled = true;
                highlightSR.sprite = targetRenderer.sprite;
                highlightSR.transform.position = targetRenderer.transform.position;
                highlightSR.transform.localScale = targetRenderer.transform.localScale;
                highlightSR.material.SetColor(PulseColorID, c);
            }
            else
            {
                highlightSR.enabled = false;
            }
        }
    }
}
