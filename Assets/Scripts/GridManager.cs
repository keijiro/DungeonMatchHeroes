using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 7x7の盤面管理、マッチ判定、落下、補充、入力処理を統括する。
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
        Ska = 6      // Gray (Empty effect)
    }

    private const int GridWidth = 7;
    private const int GridHeight = 7;
    private const float BlockSpacing = 1.1f;

    [Header("Assets")]
    [SerializeField] private Sprite whiteSprite;
    [SerializeField] private UIDocument uiDocument;

    private BlockType[,] grid = new BlockType[GridWidth, GridHeight];
    private SpriteRenderer[,] renderers = new SpriteRenderer[GridWidth, GridHeight];
    private int[] matchCounts = new int[7];

    private bool isProcessing = false;

    private void Start()
    {
        if (uiDocument == null) uiDocument = FindFirstObjectByType<UIDocument>();
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
                    // 初期配置では「スカ」を除き、マッチが発生しないようにする
                    type = (BlockType)Random.Range(0, 6);
                } while (WouldMatch(x, y, type));
                
                grid[x, y] = type;
                renderers[x, y] = CreateBlockObject(x, y, type);
            }
        }
    }

    private bool WouldMatch(int x, int y, BlockType type)
    {
        if (x >= 2 && grid[x - 1, y] == type && grid[x - 2, y] == type) return true;
        if (y >= 2 && grid[x, y - 1] == type && grid[x, y - 2] == type) return true;
        return false;
    }

    private SpriteRenderer CreateBlockObject(int x, int y, BlockType type)
    {
        GameObject obj = new GameObject($"Block_{x}_{y}");
        obj.transform.parent = this.transform;
        obj.transform.position = GetWorldPosition(x, y);
        
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = whiteSprite;
        renderer.color = GetColor(type);
        
        // クリック判定用にコライダーを追加
        var col = obj.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        return renderer;
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x - (GridWidth - 1) / 2f, y - (GridHeight - 1) / 2f, 0) * BlockSpacing;
    }

    private Color GetColor(BlockType type)
    {
        return type switch
        {
            BlockType.Sword => Color.red,
            BlockType.Shield => Color.blue,
            BlockType.Magic => new Color(0.6f, 0f, 0.8f), // Purple
            BlockType.Heal => Color.green,
            BlockType.Gem => Color.yellow,
            BlockType.Key => Color.white,
            BlockType.Ska => Color.gray,
            _ => Color.white
        };
    }

    private void Update()
    {
        // 処理中は入力を受け付けない
        if (isProcessing) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
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
            // 最下段（y = 0）のみ干渉可能
            for (int x = 0; x < GridWidth; x++)
            {
                if (hit.collider.gameObject == renderers[x, 0]?.gameObject)
                {
                    StartCoroutine(ProcessMove(x, 0));
                    break;
                }
            }
        }
    }

    private System.Collections.IEnumerator ProcessMove(int x, int y)
    {
        isProcessing = true;

        // 指定ブロックを破壊
        DestroyBlock(x, y);

        // 落下と補充（手動破壊時はスカ発生率高）
        bool firstRefill = true;
        bool hasMatches;

        do
        {
            yield return StartCoroutine(HandleGravityAndRefill(firstRefill));
            firstRefill = false;
            
            // マッチ判定
            hasMatches = CheckAndApplyMatches();
            if (hasMatches)
            {
                UpdateUI();
                yield return new WaitForSeconds(0.2f);
            }
        } while (hasMatches);

        isProcessing = false;
    }

    private void DestroyBlock(int x, int y)
    {
        if (renderers[x, y] != null)
        {
            Destroy(renderers[x, y].gameObject);
            renderers[x, y] = null;
            // 論理的な値もリセット
            grid[x, y] = (BlockType)(-1); 
        }
    }

    private System.Collections.IEnumerator HandleGravityAndRefill(bool isManualRefill)
    {
        // 1. 落下
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
                    // 落下移動
                    grid[x, y - emptyCount] = grid[x, y];
                    renderers[x, y - emptyCount] = renderers[x, y];
                    renderers[x, y - emptyCount].gameObject.name = $"Block_{x}_{y - emptyCount}";
                    renderers[x, y - emptyCount].transform.position = GetWorldPosition(x, y - emptyCount);
                    
                    grid[x, y] = (BlockType)(-1);
                    renderers[x, y] = null;
                }
            }

            // 2. 補充
            for (int i = 0; i < emptyCount; i++)
            {
                int y = GridHeight - emptyCount + i;
                BlockType newType = DecideNewBlockType(isManualRefill);
                grid[x, y] = newType;
                renderers[x, y] = CreateBlockObject(x, y, newType);
            }
        }
        yield return null;
    }

    private BlockType DecideNewBlockType(bool isManualRefill)
    {
        if (isManualRefill)
        {
            // 手動破壊時の補充は「スカ」の発生率を高くする (例: 50%)
            if (Random.value < 0.5f) return BlockType.Ska;
            return (BlockType)Random.Range(0, 6);
        }
        else
        {
            // マッチ時の補充では効果のあるブロックだけが登場する
            return (BlockType)Random.Range(0, 6);
        }
    }

    private bool CheckAndApplyMatches()
    {
        HashSet<(int, int)> matchedSet = new HashSet<(int, int)>();

        // 横
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth - 2; x++)
            {
                BlockType type = grid[x, y];
                if ((int)type == -1 || type == BlockType.Ska) continue;
                if (grid[x + 1, y] == type && grid[x + 2, y] == type)
                {
                    matchedSet.Add((x, y));
                    matchedSet.Add((x + 1, y));
                    matchedSet.Add((x + 2, y));
                }
            }
        }

        // 縦
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight - 2; y++)
            {
                BlockType type = grid[x, y];
                if ((int)type == -1 || type == BlockType.Ska) continue;
                if (grid[x, y + 1] == type && grid[x, y + 2] == type)
                {
                    matchedSet.Add((x, y));
                    matchedSet.Add((x, y + 1));
                    matchedSet.Add((x, y + 2));
                }
            }
        }

        if (matchedSet.Count > 0)
        {
            foreach (var pos in matchedSet)
            {
                matchCounts[(int)grid[pos.Item1, pos.Item2]]++;
                DestroyBlock(pos.Item1, pos.Item2);
            }
            return true;
        }
        return false;
    }

    private void UpdateUI()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        
        root.Q<Label>("SwordCount").text = matchCounts[0].ToString();
        root.Q<Label>("ShieldCount").text = matchCounts[1].ToString();
        root.Q<Label>("MagicCount").text = matchCounts[2].ToString();
        root.Q<Label>("HealCount").text = matchCounts[3].ToString();
        root.Q<Label>("GemCount").text = matchCounts[4].ToString();
        root.Q<Label>("KeyCount").text = matchCounts[5].ToString();
        root.Q<Label>("EmptyCount").text = matchCounts[6].ToString();
    }
}
