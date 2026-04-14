using UnityEngine;

public class RowHighlighter : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Material pulseMaterial;
    [SerializeField] private Color pulseColor = Color.white;
    [SerializeField, Range(1f, 10f)] private float pulseSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.5f;

    private SpriteRenderer[] highlightRenderers = new SpriteRenderer[GridManager.GridWidth];
    private static readonly int PulseColorID = Shader.PropertyToID("_PulseColor");

    private void Start()
    {
        if (gridManager == null) gridManager = GetComponent<GridManager>();
        
        // Pre-create highlight objects for each column
        for (int x = 0; x < GridManager.GridWidth; x++)
        {
            GameObject obj = new GameObject($"Highlight_{x}");
            obj.transform.parent = transform; // Keep it under GridManager or this object
            
            highlightRenderers[x] = obj.AddComponent<SpriteRenderer>();
            highlightRenderers[x].material = pulseMaterial;
            highlightRenderers[x].sortingOrder = 5; // Top layer
            highlightRenderers[x].enabled = false;
        }
    }

    private void Update()
    {
        if (gridManager == null) return;

        float alpha = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * maxAlpha;
        Color c = pulseColor;
        c.a = alpha;

        for (int x = 0; x < GridManager.GridWidth; x++)
        {
            SpriteRenderer targetRenderer = gridManager.GetRenderer(x, 0);
            
            if (targetRenderer != null && targetRenderer.gameObject.activeInHierarchy)
            {
                highlightRenderers[x].enabled = true;
                highlightRenderers[x].sprite = targetRenderer.sprite;
                highlightRenderers[x].transform.position = targetRenderer.transform.position;
                highlightRenderers[x].transform.localScale = targetRenderer.transform.localScale;
                highlightRenderers[x].material.SetColor(PulseColorID, c);
            }
            else
            {
                highlightRenderers[x].enabled = false;
            }
        }
    }
}
