using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BlockManager : MonoBehaviour
{
    public enum BlockType
    {
        Sword,   // Red
        Shield,  // Blue
        Magic,   // Purple
        Heal,    // Green
        Gem,     // Yellow
        Key,     // White
        Empty    // Gray
    }

    [SerializeField] private UIDocument uiDocument;
    
    private const int GridWidth = 7;
    private const int GridHeight = 7;
    private const float BlockSpacing = 1.1f;

    private BlockType[,] grid = new BlockType[GridWidth, GridHeight];
    private GameObject[,] blockObjects = new GameObject[GridWidth, GridHeight];
    private int[] matchCounts = new int[7];

    [SerializeField] private Sprite whiteSprite;

    private bool isProcessing = false;

    private void Start()
    {
        InitializeGrid();
        UpdateUI();
    }

    private void InitializeGrid()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                BlockType type;
                do
                {
                    // For initial grid, we don't want "Empty" blocks
                    type = (BlockType)Random.Range(0, 6); 
                } while (WouldMatch(x, y, type));
                
                grid[x, y] = type;
                blockObjects[x, y] = CreateBlockObject(x, y, type);
            }
        }
    }

    private bool WouldMatch(int x, int y, BlockType type)
    {
        // Check horizontal
        if (x >= 2 && grid[x - 1, y] == type && grid[x - 2, y] == type) return true;
        // Check vertical
        if (y >= 2 && grid[x, y - 1] == type && grid[x, y - 2] == type) return true;
        return false;
    }

    private GameObject CreateBlockObject(int x, int y, BlockType type)
    {
        GameObject obj = new GameObject($"Block_{x}_{y}");
        obj.transform.parent = this.transform;
        obj.transform.position = GetPosition(x, y);
        
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = whiteSprite;
        renderer.color = GetColor(type);
        
        // Add collider for click detection
        var col = obj.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        return obj;
    }

    private Vector3 GetPosition(int x, int y)
    {
        return new Vector3(x - GridWidth / 2f, y - GridHeight / 2f, 0) * BlockSpacing;
    }

    private Color GetColor(BlockType type)
    {
        return type switch
        {
            BlockType.Sword => Color.red,
            BlockType.Shield => Color.blue,
            BlockType.Magic => new Color(0.5f, 0, 0.5f), // Purple
            BlockType.Heal => Color.green,
            BlockType.Gem => Color.yellow,
            BlockType.Key => Color.white,
            BlockType.Empty => Color.gray,
            _ => Color.white
        };
    }

    private void Update()
    {
        if (isProcessing) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

        if (hit.collider != null)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                // Only interact with the bottom row (y = 0)
                if (hit.collider.gameObject == blockObjects[x, 0])
                {
                    isProcessing = true;
                    DestroyBlock(x, 0);
                    // For manual destroy, we refill with high Empty chance
                    ProcessGravityAndMatches(true);
                    isProcessing = false;
                    break;
                }
            }
        }
    }

    private void DestroyBlock(int x, int y)
    {
        if (blockObjects[x, y] != null)
        {
            Destroy(blockObjects[x, y]);
            blockObjects[x, y] = null;
            grid[x, y] = BlockType.Empty; 
        }
    }

    private void ProcessGravityAndMatches(bool isManualRefill)
    {
        bool changed;
        do
        {
            changed = false;
            // 1. Gravity
            for (int x = 0; x < GridWidth; x++)
            {
                int emptySpaces = 0;
                for (int y = 0; y < GridHeight; y++)
                {
                    if (blockObjects[x, y] == null)
                    {
                        emptySpaces++;
                    }
                    else if (emptySpaces > 0)
                    {
                        // Move down
                        grid[x, y - emptySpaces] = grid[x, y];
                        blockObjects[x, y - emptySpaces] = blockObjects[x, y];
                        blockObjects[x, y - emptySpaces].transform.position = GetPosition(x, y - emptySpaces);
                        
                        grid[x, y] = BlockType.Empty;
                        blockObjects[x, y] = null;
                        changed = true;
                    }
                }
                
                // Refill top
                for (int i = 0; i < emptySpaces; i++)
                {
                    int y = GridHeight - emptySpaces + i;
                    // Use isManualRefill only for the immediate refill after a manual click
                    BlockType newType = GetNewBlockType(isManualRefill);
                    grid[x, y] = newType;
                    blockObjects[x, y] = CreateBlockObject(x, y, newType);
                    changed = true;
                }
            }

            // After first gravity/refill, any subsequent ones in the loop (chains) are NOT manual
            isManualRefill = false;

            // 2. Matches
            if (CheckAndMatch())
            {
                changed = true;
            }
        } while (changed);
    }

    private BlockType GetNewBlockType(bool isManualRefill)
    {
        if (isManualRefill)
        {
            // High chance (e.g. 50%) of Empty (Ska)
            return Random.value < 0.5f ? BlockType.Empty : (BlockType)Random.Range(0, 6);
        }
        else
        {
            // Effective blocks only for matches
            return (BlockType)Random.Range(0, 6);
        }
    }

    private bool CheckAndMatch()
    {
        bool foundMatch = false;
        HashSet<(int, int)> toDestroy = new HashSet<(int, int)>();

        // Horizontal
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth - 2; x++)
            {
                BlockType type = grid[x, y];
                if (type == BlockType.Empty) continue;
                if (grid[x+1, y] == type && grid[x+2, y] == type)
                {
                    toDestroy.Add((x, y));
                    toDestroy.Add((x+1, y));
                    toDestroy.Add((x+2, y));
                    foundMatch = true;
                }
            }
        }

        // Vertical
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight - 2; y++)
            {
                BlockType type = grid[x, y];
                if (type == BlockType.Empty) continue;
                if (grid[x, y+1] == type && grid[x, y+2] == type)
                {
                    toDestroy.Add((x, y));
                    toDestroy.Add((x, y+1));
                    toDestroy.Add((x, y+2));
                    foundMatch = true;
                }
            }
        }

        if (foundMatch)
        {
            foreach (var pos in toDestroy)
            {
                matchCounts[(int)grid[pos.Item1, pos.Item2]]++;
                DestroyBlock(pos.Item1, pos.Item2);
            }
            UpdateUI();
        }
        return foundMatch;
    }

    private void UpdateUI()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        
        SetCountText(root, "SwordCount", matchCounts[0]);
        SetCountText(root, "ShieldCount", matchCounts[1]);
        SetCountText(root, "MagicCount", matchCounts[2]);
        SetCountText(root, "HealCount", matchCounts[3]);
        SetCountText(root, "GemCount", matchCounts[4]);
        SetCountText(root, "KeyCount", matchCounts[5]);
        SetCountText(root, "EmptyCount", matchCounts[6]);
    }

    private void SetCountText(VisualElement root, string name, int count)
    {
        var label = root.Q<Label>(name);
        if (label != null) label.text = count.ToString();
    }
}
