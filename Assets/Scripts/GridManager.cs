using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Manages the 7x7 grid, match detection, gravity, refilling, and input handling.
/// </summary>
public class GridManager : MonoBehaviour
{
    public enum BlockType
    {
        Sword = 0,   // Red
        Shield = 1,  // Blue
        Magic = 2,   // Purple
        Heal = 3,    // Green
        Gem = 4,     // Yellow
        Key = 5,     // White
        Junk = 6      // Gray (Empty effect)
    }

    public const int GridWidth = 7;
    public const int GridHeight = 7;
    private const float BlockSpacing = 1.1f;

    public SpriteRenderer GetRenderer(int x, int y)
    {
        if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight) return null;
        return renderers[x, y];
    }

    [Header("Assets")]
    [SerializeField] private Sprite blockBaseSprite = null;
    [SerializeField] private Sprite blockCrackedSprite = null;
    [SerializeField] private Sprite[] iconSprites = new Sprite[7];
    [SerializeField, Range(0.1f, 2.0f)] private float iconScale = 0.8f;
    [SerializeField] private ParticleSystem manualDestroyFX = null;
    [SerializeField] private ParticleSystem matchDestroyFX = null;
    [SerializeField] private ParticleSystem shockwaveFX = null;

    [Header("Visual Settings")]
    [SerializeField] private Color[] blockColors = new Color[7] {
        Color.red,      // Sword
        Color.blue,     // Shield
        new Color(0.6f, 0f, 0.8f), // Magic
        Color.green,    // Heal
        Color.cyan,     // Gem
        Color.yellow,   // Key
        Color.gray      // Junk
    };

    [Header("Generation Settings")]
    [Tooltip("Weights for Sword, Shield, Magic, Heal, Gem, Key (Junk is handled separately)")]
    [SerializeField] private float[] typeWeights = new float[6] { 1.0f, 1.0f, 0.5f, 0.5f, 0.2f, 0.2f };
    [Range(0f, 1f)]
    [SerializeField] private float manualJunkRate = 1.0f; // Default 100%

    private BlockType[,] grid = new BlockType[GridWidth, GridHeight];
    private SpriteRenderer[,] renderers = new SpriteRenderer[GridWidth, GridHeight];
    private int[] matchCounts = new int[7];

    private bool isProcessing = false;
    private CameraShake cameraShake;

    public event System.Action OnBottomRowClicked;

    private void Awake()
    {
    }

    private void Start()
    {
        if (blockBaseSprite == null) Debug.LogWarning("blockBaseSprite is NULL in GridManager!");

        cameraShake = Camera.main.GetComponent<CameraShake>();

        InitializeGrid();

        int count = 0;
        foreach (var r in renderers) if (r != null) count++;
    }

    private void InitializeGrid()
    {
        // Clear existing children first to avoid duplicates
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                BlockType type;
                do
                {
                    type = GetWeightedRandomType();
                } while (WouldMatch(x, y, type));

                grid[x, y] = type;
                renderers[x, y] = CreateBlockObject(x, y, type);
            }
        }
    }

    private BlockType GetWeightedRandomType()
    {
        float totalWeight = 0;
        foreach (float w in typeWeights) totalWeight += w;

        float r = Random.value * totalWeight;
        float currentWeight = 0;

        for (int i = 0; i < typeWeights.Length; i++)
        {
            currentWeight += typeWeights[i];
            if (r <= currentWeight) return (BlockType)i;
        }

        return BlockType.Sword;
    }

    private bool WouldMatch(int x, int y, BlockType type)
    {
        if (x >= 2 && grid[x - 1, y] == type && grid[x - 2, y] == type) return true;
        if (y >= 2 && grid[x, y - 1] == type && grid[x, y - 2] == type) return true;
        return false;
    }

    private SpriteRenderer CreateBlockObject(int x, int y, BlockType type)
    {
        GameObject root = new GameObject($"Block_{x}_{y}");
        root.transform.parent = this.transform;
        root.transform.position = GetWorldPosition(x, y);

        // Base Layer
        var baseRenderer = root.AddComponent<SpriteRenderer>();
        baseRenderer.sprite = blockBaseSprite;
        baseRenderer.color = GetColor(type);
        baseRenderer.sortingOrder = 0;

        // Icon Layer
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.parent = root.transform;
        iconObj.transform.localPosition = Vector3.zero;
        iconObj.transform.localScale = Vector3.one * iconScale;

        var iconRenderer = iconObj.AddComponent<SpriteRenderer>();
        if ((int)type >= 0 && (int)type < iconSprites.Length && iconSprites[(int)type] != null)
        {
            iconRenderer.sprite = iconSprites[(int)type];
        }
        iconRenderer.sortingOrder = 1;

        // Add a collider for click detection to the root
        var col = root.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        return baseRenderer;
    }

    private Vector3 GetWorldPosition(float x, float y)
    {
        return new Vector3(x - (GridWidth - 1) / 2f, y - (GridHeight - 1) / 2f, 0) * BlockSpacing;
    }

    private Color GetColor(BlockType type)
    {
        int index = (int)type;
        if (index >= 0 && index < blockColors.Length)
        {
            return blockColors[index];
        }
        return Color.white;
    }

    private void Update()
    {
        // Ignore input while processing or if a UI overlay (modal) is active
        if (isProcessing) return;
        if (CombatManager.Instance != null && CombatManager.Instance.IsOverlayActive) return;

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector2 pointerPos = Pointer.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(pointerPos);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

        if (hit.collider != null)
        {
            // Interaction is strictly limited to the bottom row (y = 0) as per design.
            for (int x = 0; x < GridWidth; x++)
            {
                if (hit.collider.gameObject == renderers[x, 0]?.gameObject)
                {
                    OnBottomRowClicked?.Invoke();
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySEWithRandomPitch(SEType.Click, 0.8f);
                    }
                    StartCoroutine(ProcessMove(x, 0));
                    break;
                }
            }
        }
    }

    private System.Collections.IEnumerator ProcessMove(int x, int y)
    {
        isProcessing = true;

        // Junk-Cleansing: Identify if the target was a Junk block to prevent immediate re-generation.
        bool clickedJunk = (grid[x, y] == BlockType.Junk);

        // Perform manual destruction animation
        yield return StartCoroutine(AnimateManualDestroy(renderers[x, y].gameObject));

        // Logical destruction
        DestroyBlock(x, y);

        // Gravity and refill. Manual destruction usually leads to Junk refill, 
        // except when 'clickedJunk' is true (Cleansing mechanic).
        bool firstRefill = true;
        bool hasMatches;
        int comboCount = 0;

        do
        {
            // Force non-Junk refill if the user successfully 'cleansed' a Junk block.
            yield return StartCoroutine(HandleGravityAndRefill(firstRefill, clickedJunk && firstRefill));
            firstRefill = false;

            // Match detection
            List<(int, int)> matchedSet = new List<(int, int)>();
            hasMatches = CheckMatches(out matchedSet);
            if (hasMatches)
            {
                comboCount++;
                yield return StartCoroutine(AnimateMatchesAndDestroy(matchedSet, comboCount));
            }
        } while (hasMatches);

        isProcessing = false;
    }

    private System.Collections.IEnumerator AnimateManualDestroy(GameObject block)
    {
        if (block == null) yield break;

        // 0. Shockwave immediately upon click
        if (shockwaveFX != null)
        {
            Instantiate(shockwaveFX, block.transform.position, Quaternion.identity);
        }

        // 1. Swap sprite to cracked
        SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
        if (sr != null && blockCrackedSprite != null)
        {
            sr.sprite = blockCrackedSprite;
        }

        // 2. Vibrate and micro-shake camera
        if (cameraShake != null) cameraShake.Shake(0.05f, 0.1f);

        Vector3 originalLocalPos = block.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            block.transform.localPosition = originalLocalPos + (Vector3)Random.insideUnitCircle * 0.1f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        block.transform.localPosition = originalLocalPos;

        // 3. Shrink and Glow before final pop
        elapsed = 0f;
        Vector3 originalScale = block.transform.localScale;
        while (elapsed < 0.08f)
        {
            float t = elapsed / 0.08f;
            block.transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.7f, t);
            if (sr != null) sr.color = Color.Lerp(sr.color, Color.white, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4. Particles
        if (manualDestroyFX != null)
        {
            Instantiate(manualDestroyFX, block.transform.position, Quaternion.identity);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(SEType.Pop, 0.7f);
        }

        // Hide visuals before physical destroy to avoid "pop"
        block.SetActive(false);
        block.transform.localScale = originalScale; // Reset for potential pool/reuse though not using pool
    }

    private void DestroyBlock(int x, int y)
    {
        if (renderers[x, y] != null)
        {
            Destroy(renderers[x, y].gameObject);
            renderers[x, y] = null;
            // Reset the logical value as well
            grid[x, y] = (BlockType)(-1);
        }
    }

    private System.Collections.IEnumerator HandleGravityAndRefill(bool isManualRefill, bool forceNoJunk = false)
    {
        // 1. Identify all falling blocks and refill blocks
        var fallingBlocks = new List<FallingBlockInfo>();

        for (int x = 0; x < GridWidth; x++)
        {
            int emptyCount = 0;
            for (int y = 0; y < GridHeight; y++)
            {
                if (renderers[x, y] == null)
                {
                    emptyCount++;
                }
                else if (emptyCount > 0)
                {
                    // Existing block needs to fall
                    int targetY = y - emptyCount;

                    grid[x, targetY] = grid[x, y];
                    renderers[x, targetY] = renderers[x, y];

                    fallingBlocks.Add(new FallingBlockInfo {
                        renderer = renderers[x, targetY],
                        targetPos = GetWorldPosition(x, targetY),
                        targetY = targetY,
                        targetX = x
                    });

                    grid[x, y] = (BlockType)(-1);
                    renderers[x, y] = null;
                }
            }

            // 2. Refill blocks above the grid
            for (int i = 0; i < emptyCount; i++)
            {
                int targetY = GridHeight - emptyCount + i;
                BlockType newType = DecideNewBlockType(isManualRefill, forceNoJunk);

                // Spawn above the grid
                Vector3 spawnPos = GetWorldPosition(x, GridHeight + i + 0.5f);
                SpriteRenderer newSR = CreateBlockObject(x, targetY, newType);
                newSR.transform.position = spawnPos;

                grid[x, targetY] = newType;
                renderers[x, targetY] = newSR;

                fallingBlocks.Add(new FallingBlockInfo {
                    renderer = newSR,
                    targetPos = GetWorldPosition(x, targetY),
                    targetY = targetY,
                    targetX = x
                });
            }
        }

        // 3. Animate all falling blocks
        if (fallingBlocks.Count > 0)
        {
            bool allLanded = false;
            while (!allLanded)
            {
                allLanded = true;
                foreach (var fb in fallingBlocks)
                {
                    if (fb.isLanded) continue;
                    allLanded = false;

                    // Accelerate
                    fb.velocity += 15f * Time.deltaTime; // Gravity
                    Vector3 pos = fb.renderer.transform.position;
                    pos.y -= fb.velocity * Time.deltaTime;

                    // Check if reached target
                    if (pos.y <= fb.targetPos.y)
                    {
                        pos = fb.targetPos;
                        fb.isLanded = true;
                        fb.renderer.gameObject.name = $"Block_{fb.targetX}_{fb.targetY}";
                        // Subtle camera shake on landing
                        if (cameraShake != null) cameraShake.Shake(0.01f, 0.05f);
                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlaySEWithRandomPitch(SEType.Land, 0.3f);
                        }
                    }
                    fb.renderer.transform.position = pos;
                }
                yield return null;
            }
        }
    }

    private class FallingBlockInfo
    {
        public SpriteRenderer renderer;
        public Vector3 targetPos;
        public int targetX, targetY;
        public float velocity = 0f;
        public bool isLanded = false;
    }

    private BlockType DecideNewBlockType(bool isManualRefill, bool forceNoJunk = false)
    {
        if (isManualRefill)
        {
            // Junk-Cleansing: If forceNoJunk is true, skip the Junk check
            if (!forceNoJunk && Random.value < manualJunkRate) return BlockType.Junk;
            return GetWeightedRandomType();
        }
        else
        {
            // Only effective blocks appear for match-triggered refills
            return GetWeightedRandomType();
        }
    }

    private bool CheckMatches(out List<(int, int)> finalSet)
    {
        finalSet = new List<(int, int)>();
        HashSet<(int, int)> triggerSet = new HashSet<(int, int)>();

        // Horizontal match check
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth - 2; x++)
            {
                BlockType type = grid[x, y];
                if ((int)type == -1 || type == BlockType.Junk) continue;
                if (grid[x + 1, y] == type && grid[x + 2, y] == type)
                {
                    triggerSet.Add((x, y)); triggerSet.Add((x + 1, y)); triggerSet.Add((x + 2, y));
                }
            }
        }

        // Vertical match check
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight - 2; y++)
            {
                BlockType type = grid[x, y];
                if ((int)type == -1 || type == BlockType.Junk) continue;
                if (grid[x, y + 1] == type && grid[x, y + 2] == type)
                {
                    triggerSet.Add((x, y)); triggerSet.Add((x, y + 1)); triggerSet.Add((x, y + 2));
                }
            }
        }

        if (triggerSet.Count > 0)
        {
            HashSet<(int, int)> matchedSet = new HashSet<(int, int)>();
            HashSet<(int, int)> visited = new HashSet<(int, int)>();

            foreach (var pos in triggerSet)
            {
                if (visited.Contains(pos)) continue;

                List<(int, int)> cluster = new List<(int, int)>();
                BlockType type = grid[pos.Item1, pos.Item2];
                FindCluster(pos.Item1, pos.Item2, type, cluster, visited);

                // Find unique Junk blocks adjacent to this cluster
                HashSet<(int, int)> junkInCluster = new HashSet<(int, int)>();
                int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
                foreach (var c in cluster)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = c.Item1 + dx[i], ny = c.Item2 + dy[i];
                        if (nx >= 0 && nx < GridWidth && ny >= 0 && ny < GridHeight)
                        {
                            if (grid[nx, ny] == BlockType.Junk) junkInCluster.Add((nx, ny));
                        }
                    }
                }

                // Calculate center world position of the cluster and its adjacent Junk blocks
                Vector3 centerWorldPos = Vector3.zero;
                foreach (var c in cluster) centerWorldPos += GetWorldPosition(c.Item1, c.Item2);
                foreach (var s in junkInCluster) centerWorldPos += GetWorldPosition(s.Item1, s.Item2);
                centerWorldPos /= (cluster.Count + junkInCluster.Count);

                // Notify CombatManager for each cluster found
                if (CombatManager.Instance != null)
                {
                    CombatManager.Instance.AddPlayerAction(type, cluster.Count, junkInCluster.Count, centerWorldPos);
                }

                foreach (var c in cluster) matchedSet.Add(c);
                foreach (var s in junkInCluster) matchedSet.Add(s);
            }

            foreach (var pos in matchedSet) finalSet.Add(pos);
            return true;
        }
        return false;
    }

    private System.Collections.IEnumerator AnimateMatchesAndDestroy(List<(int, int)> positions, int comboCount = 1)
    {
        // 1. Swell and Glow
        float elapsed = 0f;
        while (elapsed < 0.075f)
        {
            float t = elapsed / 0.075f;
            float scale = Mathf.Lerp(1.0f, 1.3f, t);
            foreach (var pos in positions)
            {
                var r = renderers[pos.Item1, pos.Item2];
                if (r != null)
                {
                    r.transform.localScale = Vector3.one * scale;
                    r.color = Color.white;
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. Stretch and Particle
        elapsed = 0f;
        while (elapsed < 0.075f)
        {
            float t = elapsed / 0.075f;
            Vector3 targetScale = new Vector3(0.2f, 2.0f, 1.0f);
            foreach (var pos in positions)
            {
                var r = renderers[pos.Item1, pos.Item2];
                if (r != null)
                {
                    r.transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, targetScale, t);
                    Color c = r.color;
                    c.a = 1.0f - t;
                    r.color = c;
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (AudioManager.Instance != null && positions.Count > 0)
        {
            // Pitch increases by 0.05 per combo, max 1.5
            float pitch = Mathf.Min(1.0f + (comboCount - 1) * 0.05f, 1.5f);
            AudioManager.Instance.PlaySE(SEType.Match, 0.9f, pitch);
        }

        foreach (var pos in positions)
        {
            if (matchDestroyFX != null && renderers[pos.Item1, pos.Item2] != null)
            {
                Instantiate(matchDestroyFX, renderers[pos.Item1, pos.Item2].transform.position, Quaternion.identity);
            }
            DestroyBlock(pos.Item1, pos.Item2);
        }
    }

    private void FindCluster(int x, int y, BlockType type, List<(int, int)> cluster, HashSet<(int, int)> visited)
    {
        if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight) return;
        if (visited.Contains((x, y)) || grid[x, y] != type) return;

        visited.Add((x, y));
        cluster.Add((x, y));

        // Recursively explore 4 directions
        FindCluster(x + 1, y, type, cluster, visited);
        FindCluster(x - 1, y, type, cluster, visited);
        FindCluster(x, y + 1, type, cluster, visited);
        FindCluster(x, y - 1, type, cluster, visited);
    }
}
