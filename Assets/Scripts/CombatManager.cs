using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public enum CombatActionType
{
    PlayerAttack,
    PlayerMagicAttack,
    PlayerHeal,
    PlayerShield,
    PlayerExp,
    PlayerKey,
    EnemyAttack
}

public class CombatAction
{
    public CombatActionType Type;
    public int Value;
    public EnemyUnit SourceEnemy; // Only for EnemyAttack
    public bool IsMagic; // For Zombie Mage
}

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Party Stats")]
    public int Level = 1;
    public int MaxHP = 100;
    public int CurrentHP;
    public int Shield;
    public int Experience;
    public bool HasKey;

    [Header("Game Balance")]
    public GameBalanceData balanceData;

    [Header("Enemy Settings")]
    public GameObject[] EnemyPrefabs;
    public Transform[] EnemySpawnPoints;
    public List<EnemyUnit> ActiveEnemies = new List<EnemyUnit>();
    public int WaveCount = 0;

    private Dictionary<string, GameBalanceData.EnemyDefinition> enemyDefs = new Dictionary<string, GameBalanceData.EnemyDefinition>();

    private void InitializeEnemyDefs()
    {
        enemyDefs.Clear();
        if (balanceData != null && balanceData.EnemyDefinitions != null)
        {
            foreach (var def in balanceData.EnemyDefinitions)
            {
                enemyDefs[def.Name] = def;
            }
        }
    }

    private int GetMaxLevelForWave(int wave)
    {
        if (balanceData == null) return Mathf.FloorToInt(6f + 1.2f * wave);
        return Mathf.FloorToInt(balanceData.InitialWaveBudget + (wave - 1) * balanceData.BudgetIncreasePerWave);
    }

    private LinkedList<CombatAction> eventQueue = new LinkedList<CombatAction>();
    private bool isProcessingQueue = false;
    private CombatAction currentAction = null;

    [Header("UI References")]
    public UIDocument HUD;
    private VisualElement hpBarFill;
    private VisualElement shieldBarFill;
    private Label hpText;
    private Label shieldText;
    private Label expText;
    private Label keyLabel;
    private VisualElement notificationLayer;

    private bool isModalActive = false;
    public bool IsOverlayActive => isModalActive;

    [Header("Treasure Chest Settings")]
    [Range(0f, 1f)] public float TreasureSpawnChance = 0.5f;
    public Sprite ChestClosedSprite;
    public Sprite ChestOpenSprite;
    private VisualElement treasureOverlay;
    private VisualElement treasureImage;
    private VisualElement tipIcon;
    private VisualElement dialogueBox;
    private Label treasureMessage;
    private Label treasureMessageJp;

    private static readonly string[] TIPS = new string[]
    {
        "\"?\" blocks cannot be matched. Shatter or Blast them.",
        "Shattering an elemental block drops a \"?\" block from above.",
        "Shattering or Blasting a \"?\" drops an elemental block from above.",
        "If stuck, focus on shattering \"?\" blocks to find new matches.",
        "Match Shields to block incoming physical attacks.",
        "Magic matches hit the entire enemy party.",
        "Shields cannot exceed your Max HP.",
        "Shields cannot block enemy magic attacks.",
        "A Key is needed for chests, but you can only carry one.",
        "Match Gems to gain EXP.",
        "Matching Heal blocks at full HP grants EXP.",
        "Matching Keys while holding one grants EXP.",
        "Enemies in the back row attack more slowly.",
        "Blasting \"?\" blocks near your matches slightly increases their power."
    };

    private static readonly string[] TIPS_JP = new string[]
    {
        "「？」ブロックは揃えることができません。砕くか爆破しましょう。",
        "属性ブロックを砕くと、上から「？」ブロックが降ってきます。",
        "「？」を砕いたり爆破したりすると、属性ブロックが降ってきます。",
        "行き詰まったら、「？」ブロックを砕いて新しいマッチを探しましょう。",
        "シールドを揃えると、敵の物理攻撃を防ぐことができます。",
        "魔法を揃えると、敵パーティ全体を攻撃できます。",
        "シールド量は最大HPを超えることはできません。",
        "シールドで敵の魔法攻撃を防ぐことはできません。",
        "宝箱を開けるには鍵が必要ですが、持てる鍵は1つだけです。",
        "ジェムを揃えると、EXP（経験値）を獲得できます。",
        "HP満タン時に回復ブロックを揃えると、EXPを獲得できます。",
        "鍵を持っている時にさらに鍵を揃えると、EXPを獲得できます。",
        "後列の敵は、前列よりも攻撃の頻度が低くなります。",
        "マッチの近くで「？」を爆破すると、その攻撃力が少しアップします。"
    };

    private static int nextTipIndex = 0;
    private bool overlayClicked = false;

    [Header("Player Animators")]
    public Animator FighterAnimator;
    public Animator MageAnimator;
    public Animator TankAnimator;

    private CharacterVisuals fighterVisuals;
    private CharacterVisuals mageVisuals;
    private CharacterVisuals tankVisuals;

    private int GetThresholdForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        if (balanceData == null) return 0;
        float total = 0;
        for (int i = 1; i < targetLevel; i++)
        {
            total += (float)balanceData.ExpBaseRequirement + (i - 1) * balanceData.ExpIncreasePerLevel;
        }
        return Mathf.RoundToInt(total);
    }

    private int GetMaxHPForLevel(int level)
    {
        if (balanceData == null) return 100;
        return Mathf.RoundToInt(balanceData.PlayerBaseHP + (level - 1) * balanceData.HPIncreasePerLevel);
    }

    private int GetAttackForLevel(int level)
    {
        if (balanceData == null) return 10;
        return Mathf.RoundToInt(balanceData.PlayerBaseAttack + (level - 1) * balanceData.AttackIncreasePerLevel);
    }

    private int GetMagicAttackForLevel(int level)
    {
        if (balanceData == null) return 3;
        return Mathf.RoundToInt(GetAttackForLevel(level) * balanceData.MagicAttackRatio);
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeEnemyDefs();
        MaxHP = GetMaxHPForLevel(Level);
        CurrentHP = MaxHP;
        SetupUI();

        // Assign player animators and visuals
        SetupPlayerComponents();
    }

    private void SetupPlayerComponents()
    {
        if (FighterAnimator == null)
        {
            GameObject f = GameObject.Find("Fighter");
            if (f != null) FighterAnimator = f.GetComponent<Animator>();
        }
        if (MageAnimator == null)
        {
            GameObject m = GameObject.Find("Mage");
            if (m != null) MageAnimator = m.GetComponent<Animator>();
        }
        if (TankAnimator == null)
        {
            GameObject t = GameObject.Find("Tank");
            if (t != null) TankAnimator = t.GetComponent<Animator>();
        }

        if (FighterAnimator != null) fighterVisuals = GetOrAddVisuals(FighterAnimator.gameObject);
        if (MageAnimator != null) mageVisuals = GetOrAddVisuals(MageAnimator.gameObject);
        if (TankAnimator != null) tankVisuals = GetOrAddVisuals(TankAnimator.gameObject);
    }

    private CharacterVisuals GetOrAddVisuals(GameObject go)
    {
        CharacterVisuals v = go.GetComponent<CharacterVisuals>();
        if (v == null) v = go.AddComponent<CharacterVisuals>();
        return v;
    }

    private void SetupUI()
    {
        if (HUD == null) HUD = GetComponent<UIDocument>();
        if (HUD == null) return;

        var root = HUD.rootVisualElement;
        hpBarFill = root.Q<VisualElement>("hp-bar-fill");
        shieldBarFill = root.Q<VisualElement>("shield-bar-fill");
        hpText = root.Q<Label>("hp-text");
        shieldText = root.Q<Label>("shield-text");
        expText = root.Q<Label>("exp-text");
        keyLabel = root.Q<Label>("key-label");
        notificationLayer = root.Q<VisualElement>("notification-layer");
        treasureOverlay = root.Q<VisualElement>("treasure-overlay");
        treasureImage = root.Q<VisualElement>("treasure-chest-image");
        tipIcon = root.Q<VisualElement>("tip-icon");
        dialogueBox = root.Q<VisualElement>("dialogue-box");
        treasureMessage = root.Q<Label>("treasure-message");
        treasureMessageJp = root.Q<Label>("treasure-message-jp");

        UpdateUI();
    }

    public void AddExperience(int amount)
    {
        Experience += amount;

        while (Experience >= GetThresholdForLevel(Level + 1))
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        MaxHP = GetMaxHPForLevel(Level);
        CurrentHP = MaxHP; // Full heal upon leveling up

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(SEType.LevelUp);
        }

        ShowLevelUpPopup();
        UpdateUI();
    }

    private void ShowLevelUpPopup()
    {
        StartCoroutine(ShowLevelUpPopupRoutine());
    }

    private IEnumerator ShowLevelUpPopupRoutine()
    {
        if (HUD == null) yield break;
        var root = HUD.rootVisualElement;

        Label label = new Label("LEVEL UP!");
        label.AddToClassList("center-message");
        label.AddToClassList("level-up-message");
        label.style.color = Color.cyan;
        root.Add(label);

        // Small delay to allow UITK to pick up the initial state for transition
        yield return null;
        label.AddToClassList("center-message--visible");

        yield return new WaitForSeconds(1.5f);

        label.RemoveFromClassList("center-message--visible");
        label.AddToClassList("center-message--hiding");
        yield return new WaitForSeconds(0.4f); // Wait for fade out transition
        root.Remove(label);
    }

    private void UpdateUI()
    {
        if (hpBarFill != null)
        {
            float percentage = (float)CurrentHP / MaxHP * 100f;
            hpBarFill.style.width = new Length(percentage, LengthUnit.Percent);
        }
        if (shieldBarFill != null)
        {
            // Shield bar represents current shield strength relative to Max HP
            float percentage = (float)Shield / MaxHP * 100f;
            shieldBarFill.style.width = new Length(percentage, LengthUnit.Percent);
        }
        if (hpText != null) hpText.text = $"{CurrentHP}/{MaxHP}";
        if (shieldText != null) shieldText.text = Shield.ToString();
        if (expText != null) expText.text = Experience.ToString();
        if (keyLabel != null) keyLabel.text = HasKey ? "KEY: YES" : "KEY: NO";
    }

    public void ShowCenterMessage(string text, Color color)
    {
        StartCoroutine(ShowCenterMessageRoutine(text, color));
    }

    private IEnumerator ShowCenterMessageRoutine(string text, Color color)
    {
        if (HUD == null) yield break;
        var root = HUD.rootVisualElement;

        Label label = new Label(text);
        label.AddToClassList("center-message");
        label.style.color = color;
        root.Add(label);

        // Play SE based on text
        if (AudioManager.Instance != null)
        {
            if (text.Contains("MONSTERS")) AudioManager.Instance.PlaySE(SEType.Approach);
            else if (text.Contains("VICTORY")) AudioManager.Instance.PlaySE(SEType.Victory);
        }

        // Small delay to allow UITK to pick up the initial state for transition
        yield return null;
        label.AddToClassList("center-message--visible");

        yield return new WaitForSeconds(1.5f);

        label.RemoveFromClassList("center-message--visible");
        label.AddToClassList("center-message--hiding");
        yield return new WaitForSeconds(0.4f); // Wait for fade out transition
        root.Remove(label);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(ShowCenterMessageRoutine("MONSTERS APPROACH!", new Color(1f, 0.4f, 0.2f)));

        yield return StartCoroutine(SpawnWaveSequentially());
        StartCoroutine(QueueProcessor());
    }

    public void AddPlayerAction(GridManager.BlockType type, int matchCount, int junkCount, Vector3 worldPos)
    {
        // effectiveCount factors in adjacent Junk blocks to boost the action's power.
        float divisor = (balanceData != null) ? balanceData.JunkDivisor : 3.0f;
        float baseMatch = (balanceData != null) ? balanceData.BaseMatchCount : 3.0f;
        int effectiveCount = matchCount + Mathf.FloorToInt(junkCount / divisor);
        if (effectiveCount <= 0) return;

        CombatAction action = new CombatAction();
        int currentReq = GetThresholdForLevel(Level + 1) - GetThresholdForLevel(Level);

        switch (type)
        {
            case GridManager.BlockType.Sword:
                {
                    action.Type = CombatActionType.PlayerAttack;
                    // Scale attack based on how many blocks were matched relative to 'BaseMatchCount'.
                    action.Value = Mathf.RoundToInt((effectiveCount / baseMatch) * GetAttackForLevel(Level));
                }
                break;
            case GridManager.BlockType.Magic:
                {
                    action.Type = CombatActionType.PlayerMagicAttack;
                    action.Value = Mathf.RoundToInt((effectiveCount / baseMatch) * GetMagicAttackForLevel(Level));
                }
                break;
            case GridManager.BlockType.Heal:
                {
                    if (CurrentHP >= MaxHP)
                    {
                        // Overhealing: Convert Heal to EXP (roughly 1/3 value of a Gem).
                        action.Type = CombatActionType.PlayerExp;
                        int gemDiv = (balanceData != null) ? balanceData.GemExpDivisor : 20;
                        int gemExpVal = effectiveCount * Mathf.Max(1, currentReq / gemDiv);
                        action.Value = Mathf.Max(1, gemExpVal / 3);
                    }
                    else
                    {
                        action.Type = CombatActionType.PlayerHeal;
                        float healRatio = (balanceData != null) ? balanceData.HealAttackRatio : 0.6f;
                        action.Value = Mathf.RoundToInt((effectiveCount / baseMatch) * GetAttackForLevel(Level) * healRatio);
                    }
                }
                break;
            case GridManager.BlockType.Shield:
                {
                    action.Type = CombatActionType.PlayerShield;
                    int shieldDivisor = (balanceData != null) ? balanceData.ShieldMaxBlocksToReachMaxHP : 20;
                    action.Value = effectiveCount * Mathf.Max(1, MaxHP / shieldDivisor);
                }
                break;
            case GridManager.BlockType.Gem:
                {
                    action.Type = CombatActionType.PlayerExp;
                    int gemDivisor = (balanceData != null) ? balanceData.GemExpDivisor : 20;
                    action.Value = effectiveCount * Mathf.Max(1, currentReq / gemDivisor);
                }
                break;
            case GridManager.BlockType.Key:
                {
                    action.Type = CombatActionType.PlayerKey;
                    int keyDivisor = (balanceData != null) ? balanceData.GemExpDivisor : 20;
                    action.Value = effectiveCount * Mathf.Max(1, currentReq / keyDivisor);
                }
                break;
        }

        // Show UI notification at the match centroid
        ShowActionNotification(action.Type, action.Value, worldPos);

        // Player actions are prioritized and placed at the head of the processing queue.
        eventQueue.AddFirst(action);
    }

    private void ShowActionNotification(CombatActionType type, int value, Vector3 worldPos)
    {
        if (notificationLayer == null) return;

        Label label = new Label();
        label.AddToClassList("notification-label");

        // Position based on world coordinates
        Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(notificationLayer.panel, worldPos, Camera.main);
        label.style.left = panelPos.x;
        label.style.top = panelPos.y;

        string text = "";
        Color color = Color.white;

        switch (type)
        {
            case CombatActionType.PlayerAttack:
                text = $"Attack! {value} pts.";
                color = Color.red;
                break;
            case CombatActionType.PlayerMagicAttack:
                text = $"Magic! {value} pts.";
                color = new Color(0.6f, 0f, 0.8f);
                break;
            case CombatActionType.PlayerHeal:
                text = $"Heal! {value} pts.";
                color = Color.green;
                break;
            case CombatActionType.PlayerShield:
                text = $"Shield! {value} pts.";
                color = new Color(0.5f, 0.8f, 1f);
                break;
            case CombatActionType.PlayerExp:
                text = $"EXP! +{value}";
                color = Color.cyan;
                break;
            case CombatActionType.PlayerKey:
                if (!HasKey)
                {
                    text = "Key Obtained!";
                }
                else
                {
                    text = $"Key! +{value} EXP";
                }
                color = Color.yellow;
                break;
        }

        label.text = text;
        label.style.color = color;
        notificationLayer.Add(label);

        StartCoroutine(AnimateNotification(label));
    }

    public void ShowCombatNumber(int value, Color color, Vector3 worldPos)
    {
        if (notificationLayer == null) return;

        Label label = new Label(value.ToString());
        label.AddToClassList("combat-number");
        label.style.color = color;

        Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(notificationLayer.panel, worldPos, Camera.main);
        label.style.left = panelPos.x;
        label.style.top = panelPos.y;

        notificationLayer.Add(label);
        StartCoroutine(AnimateCombatNumber(label, panelPos));
    }

    private IEnumerator AnimateCombatNumber(Label label, Vector2 basePos)
    {
        float totalDuration = 1.0f;
        float elapsed = 0f;

        // Initial state
        label.style.scale = new Scale(new Vector3(1.2f, 1.2f, 1));
        label.style.opacity = 1;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / totalDuration;

            // 1. Bouncing motion (Physics-like bounce)
            // Increase damping (Pow) and frequency to make it snappier
            float bounceAmplitude = 60f * Mathf.Pow(Mathf.Max(0, 1f - t * 2.0f), 2.0f);
            float bounceY = -Mathf.Abs(Mathf.Cos(t * 25f)) * bounceAmplitude;

            label.style.left = basePos.x;
            label.style.top = basePos.y + bounceY;

            // 2. Scale effect (1.2 -> 1.0)
            float scale = Mathf.Lerp(1.2f, 1.0f, Mathf.Min(1, t * 8f));
            label.style.scale = new Scale(new Vector3(scale, scale, 1));

            // 3. Fade out (disappears in the last 0.3 seconds)
            if (t > 0.7f)
            {
                label.style.opacity = 1f - (t - 0.7f) / 0.3f;
            }

            yield return null;
        }

        label.RemoveFromHierarchy();
    }

    private Vector3 GetPartyCentroid()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        if (FighterAnimator != null) { sum += FighterAnimator.transform.position; count++; }
        if (MageAnimator != null) { sum += MageAnimator.transform.position; count++; }
        if (TankAnimator != null) { sum += TankAnimator.transform.position; count++; }
        return count > 0 ? (sum / count) : Vector3.zero;
    }

    private IEnumerator AnimateNotification(Label label)
    {
        // Initial state: Slightly below center, invisible
        label.style.opacity = 0;
        label.style.translate = new Translate(Length.Percent(-50), Length.Percent(0));

        // Phase 1: Emergence (Ease Out) - Increased travel (3x)
        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1 - Mathf.Pow(1 - t, 3); // Ease Out Cubic

            label.style.opacity = t;
            // From 0 to -150% (was -50%)
            label.style.translate = new Translate(Length.Percent(-50), Length.Percent(Mathf.Lerp(0, -150, easedT)));
            yield return null;
        }

        // Phase 2: Float and Fade (Slow) - Restored to original travel distance (100% delta)
        elapsed = 0f;
        duration = 1.0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            label.style.opacity = 1 - t;
            // From -150% to -250% (delta 100%, matching original travel speed)
            label.style.translate = new Translate(Length.Percent(-50), Length.Percent(Mathf.Lerp(-150, -250, t)));
            yield return null;
        }

        label.RemoveFromHierarchy();
    }

    public void AddEnemyAction(EnemyUnit enemy, int damage, bool isMagic)
    {
        CombatAction action = new CombatAction
        {
            Type = CombatActionType.EnemyAttack,
            Value = damage,
            SourceEnemy = enemy,
            IsMagic = isMagic
        };
        eventQueue.AddLast(action);
    }

    private IEnumerator QueueProcessor()
    {
        while (true)
        {
            if (eventQueue.Count > 0 && !isProcessingQueue)
            {
                CombatAction action = eventQueue.First.Value;
                eventQueue.RemoveFirst();
                yield return StartCoroutine(ExecuteAction(action));
            }
            yield return null;
        }
    }

    public bool HasPendingAction(EnemyUnit enemy)
    {
        if (enemy == null) return false;

        // 1. Check if the enemy is currently executing an action
        if (currentAction != null && currentAction.SourceEnemy == enemy) return true;

        // 2. Check if the enemy has an action in the queue
        foreach (var action in eventQueue)
        {
            if (action.SourceEnemy == enemy) return true;
        }
        return false;
    }

    private IEnumerator ExecuteAction(CombatAction action)
    {
        isProcessingQueue = true;
        currentAction = action;

        switch (action.Type)
        {
            case CombatActionType.PlayerAttack:
                yield return StartCoroutine(HandlePlayerAttack(action.Value));
                break;
            case CombatActionType.PlayerMagicAttack:
                yield return StartCoroutine(HandlePlayerMagicAttack(action.Value));
                break;
            case CombatActionType.PlayerHeal:
                // All players flash Green
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(SEType.Heal);
                List<Coroutine> healCoroutines = new List<Coroutine>();
                Color healColor = new Color(0f, 1f, 0f, 0.8f);
                if (fighterVisuals != null) healCoroutines.Add(StartCoroutine(fighterVisuals.TriggerSoftDoubleFlash(healColor, 0.4f)));
                if (mageVisuals != null) healCoroutines.Add(StartCoroutine(mageVisuals.TriggerSoftDoubleFlash(healColor, 0.4f)));
                if (tankVisuals != null) healCoroutines.Add(StartCoroutine(tankVisuals.TriggerSoftDoubleFlash(healColor, 0.4f)));
                foreach (var c in healCoroutines) yield return c;

                ShowCombatNumber(action.Value, Color.green, GetPartyCentroid());

                CurrentHP = Mathf.Min(MaxHP, CurrentHP + action.Value);
                yield return new WaitForSeconds(0.2f);
                break;
            case CombatActionType.PlayerShield:
                // Tank flashes Cyan
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(SEType.Shield);
                if (tankVisuals != null) yield return StartCoroutine(tankVisuals.TriggerSoftDoubleFlash(new Color(0f, 1f, 1f, 0.8f), 0.4f));

                ShowCombatNumber(action.Value, new Color(0.2f, 0.6f, 1f), TankAnimator.transform.position);

                Shield = Mathf.Min(MaxHP, Shield + action.Value);
                yield return new WaitForSeconds(0.2f);
                break;
            case CombatActionType.PlayerExp:
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySEWithRandomPitch(SEType.Exp, 0.7f);
                AddExperience(action.Value);
                yield return new WaitForSeconds(0.1f);
                break;
            case CombatActionType.PlayerKey:
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(SEType.Key);
                if (!HasKey)
                {
                    HasKey = true;
                }
                else
                {
                    AddExperience(action.Value);
                }
                yield return new WaitForSeconds(0.3f);
                break;
            case CombatActionType.EnemyAttack:
                yield return StartCoroutine(HandleEnemyAttack(action));
                break;
        }

        UpdateUI();
        currentAction = null;
        isProcessingQueue = false;
    }

    private IEnumerator WaitForAnimation(Animator animator, string stateName)
    {
        if (animator == null) yield break;

        // 1. Wait until the animator begins to enter the target state
        // We check for transition or the state name itself
        bool started = false;
        float timeout = 2.0f; // Safety timeout
        while (!started && timeout > 0)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(stateName))
            {
                started = true;
            }
            else
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        // 2. Wait until the state reaches at least 95% completion
        // (Using 0.95 instead of 1.0 for snappier feel, as transitions back to Idle usually start before 1.0)
        while (started)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(stateName) || state.normalizedTime >= 0.95f)
            {
                break;
            }
            yield return null;
        }
    }

    private IEnumerator HandlePlayerAttack(int damage)
    {
        if (ActiveEnemies.Count == 0) yield break;

        // 1. Player Flash
        if (fighterVisuals != null) yield return StartCoroutine(fighterVisuals.TriggerAttackEffect());

        // 2. Player Attack Animation
        if (FighterAnimator != null)
        {
            FighterAnimator.SetTrigger("Attack");
            yield return StartCoroutine(WaitForAnimation(FighterAnimator, "Attack"));
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(SEType.Attack);

        // 3. Apply Damage and Enemy Reaction
        EnemyUnit target = ActiveEnemies[0];
        if (target != null)
        {
            yield return StartCoroutine(target.TakeDamage(damage));
        }

        CleanupEnemies();
    }

    private IEnumerator HandlePlayerMagicAttack(int damage)
    {
        if (ActiveEnemies.Count == 0) yield break;

        // 1. Mage Flash
        if (mageVisuals != null) yield return StartCoroutine(mageVisuals.TriggerAttackEffect());

        // 2. Mage Magic Animation
        if (MageAnimator != null)
        {
            MageAnimator.SetTrigger("Magic");
            yield return StartCoroutine(WaitForAnimation(MageAnimator, "Magic"));
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(SEType.Magic);

        // 3. Apply Damage to all and Enemy Reactions
        List<EnemyUnit> targets = new List<EnemyUnit>(ActiveEnemies);
        List<Coroutine> coroutines = new List<Coroutine>();
        foreach (var enemy in targets)
        {
            if (enemy != null)
            {
                coroutines.Add(StartCoroutine(enemy.TakeDamage(damage)));
            }
        }

        foreach (var c in coroutines) yield return c;

        CleanupEnemies();
    }

    private void CleanupEnemies()
    {
        ActiveEnemies.RemoveAll(e => e == null || e.IsDead);

        if (ActiveEnemies.Count == 0)
        {
            StartCoroutine(HandleWaveClear());
        }
    }

    private IEnumerator HandleWaveClear()
    {
        yield return new WaitForSeconds(0.8f);
        yield return StartCoroutine(ShowCenterMessageRoutine("VICTORY!", new Color(1f, 0.8f, 0.2f)));

        // Use configured spawn chance
        if (Random.value < TreasureSpawnChance)
        {
            yield return StartCoroutine(TreasureChestEventRoutine());
        }

        yield return StartCoroutine(ShowTipRoutine());

        yield return StartCoroutine(SpawnWaveWithDelay());
    }

    private IEnumerator ShowTipRoutine()
    {
        if (nextTipIndex >= TIPS.Length) yield break;
        if (treasureOverlay == null || treasureMessage == null || treasureMessageJp == null) yield break;

        string currentTip = TIPS[nextTipIndex];
        string currentTipJp = TIPS_JP[nextTipIndex];
        nextTipIndex++;

        // Reset state
        isModalActive = true;
        overlayClicked = false;
        treasureOverlay.pickingMode = PickingMode.Position;
        treasureOverlay.RegisterCallback<ClickEvent>(OnOverlayClicked);

        // Setup UI for Tip
        treasureImage.style.display = DisplayStyle.None;
        if (tipIcon != null) tipIcon.style.display = DisplayStyle.Flex;
        if (dialogueBox != null) dialogueBox.AddToClassList("dialogue-box--tip");
        treasureMessage.text = currentTip;
        treasureMessageJp.text = currentTipJp;
        treasureMessageJp.style.display = DisplayStyle.Flex;

        // Show
        treasureOverlay.style.display = DisplayStyle.Flex;
        treasureOverlay.AddToClassList("treasure-overlay--visible");
        if (dialogueBox != null) dialogueBox.style.opacity = 1;

        // Wait for 4 seconds or click
        yield return StartCoroutine(WaitForSecondsOrClick(4.0f));

        // Hide
        treasureOverlay.RemoveFromClassList("treasure-overlay--visible");
        if (dialogueBox != null) dialogueBox.style.opacity = 0;
        yield return StartCoroutine(WaitForSecondsOrClick(0.5f));

        // Cleanup
        isModalActive = false;
        treasureOverlay.UnregisterCallback<ClickEvent>(OnOverlayClicked);
        treasureOverlay.style.display = DisplayStyle.None;
        treasureOverlay.pickingMode = PickingMode.Ignore;

        // Reset for potential TreasureChestEventRoutine
        treasureImage.style.display = DisplayStyle.Flex;
        if (tipIcon != null) tipIcon.style.display = DisplayStyle.None;
        if (dialogueBox != null) dialogueBox.RemoveFromClassList("dialogue-box--tip");
    }

    private void OnOverlayClicked(ClickEvent evt)
    {
        overlayClicked = true;
    }

    private IEnumerator WaitForSecondsOrClick(float seconds)
    {
        float timer = seconds;
        while (timer > 0 && !overlayClicked)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        overlayClicked = false;
    }

    private IEnumerator TreasureChestEventRoutine()
    {
        if (treasureOverlay == null) yield break;

        // Reset state
        isModalActive = true;
        overlayClicked = false;
        treasureOverlay.pickingMode = PickingMode.Position;
        treasureOverlay.RegisterCallback<ClickEvent>(OnOverlayClicked);

        if (ChestClosedSprite != null)
        {
            treasureImage.style.backgroundImage = new StyleBackground(ChestClosedSprite);
            treasureImage.style.opacity = 1;
        }

        treasureMessage.text = "You found a chest!";
        treasureMessageJp.text = "";
        treasureMessageJp.style.display = DisplayStyle.None;
        if (dialogueBox != null) dialogueBox.style.opacity = 1;

        // Show overlay
        treasureOverlay.style.display = DisplayStyle.Flex;
        treasureOverlay.AddToClassList("treasure-overlay--visible");
        yield return StartCoroutine(WaitForSecondsOrClick(0.5f)); // Fade in time

        yield return StartCoroutine(WaitForSecondsOrClick(1.0f));

        if (HasKey)
        {
            // Sync fade out for both dialogue box and closed chest
            if (dialogueBox != null) dialogueBox.style.opacity = 0;
            treasureImage.style.opacity = 0;
            yield return StartCoroutine(WaitForSecondsOrClick(0.3f));

            // Opening success logic
            HasKey = false;
            int currentReq = GetThresholdForLevel(Level + 1) - GetThresholdForLevel(Level);
            int chestDivisor = (balanceData != null) ? balanceData.ChestExpDivisor : 8;
            AddExperience(Mathf.Max(1, currentReq / chestDivisor));
            UpdateUI();

            // Play elegant harp SE
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(SEType.ChestOpen);

            // Switch sprite to open state
            if (ChestOpenSprite != null)
                treasureImage.style.backgroundImage = new StyleBackground(ChestOpenSprite);

            // Sync fade in for both dialogue box (with new message) and open chest
            treasureMessage.text = "Unlocked with the key!\nBonus EXP obtained!";
            treasureMessageJp.text = "";
            treasureMessageJp.style.display = DisplayStyle.None;
            treasureImage.style.opacity = 1;
            if (dialogueBox != null) dialogueBox.style.opacity = 1;

            yield return StartCoroutine(WaitForSecondsOrClick(2.5f));
        }
        else
        {
            // Opening failure - Only fade the dialogue box
            if (dialogueBox != null) dialogueBox.style.opacity = 0;
            yield return StartCoroutine(WaitForSecondsOrClick(0.3f));

            treasureMessage.text = "No key to open it...";
            treasureMessageJp.text = "";
            treasureMessageJp.style.display = DisplayStyle.None;
            if (dialogueBox != null) dialogueBox.style.opacity = 1;
            yield return StartCoroutine(WaitForSecondsOrClick(1.5f));
        }

        // Hide overlay (Fades out everything)
        treasureOverlay.RemoveFromClassList("treasure-overlay--visible");
        yield return StartCoroutine(WaitForSecondsOrClick(0.5f)); // Fade out time

        // Cleanup
        isModalActive = false;
        treasureOverlay.UnregisterCallback<ClickEvent>(OnOverlayClicked);
        treasureOverlay.style.display = DisplayStyle.None;
        treasureOverlay.pickingMode = PickingMode.Ignore;
    }

    private IEnumerator SpawnWaveWithDelay()
    {
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(ShowCenterMessageRoutine("MONSTERS APPROACH!", new Color(1f, 0.4f, 0.2f)));
        yield return StartCoroutine(SpawnWaveSequentially());
    }

    private IEnumerator HandleEnemyAttack(CombatAction action)
    {
        if (action.SourceEnemy == null || action.SourceEnemy.IsDead) yield break;

        // 1. Enemy Attack (Flash + Animation)
        yield return StartCoroutine(action.SourceEnemy.Attack());
        Animator enemyAnimator = action.SourceEnemy.GetComponent<Animator>();
        if (enemyAnimator != null)
        {
            yield return StartCoroutine(WaitForAnimation(enemyAnimator, "Attack"));
        }

        // Play Monster Attack SE after animation
        if (AudioManager.Instance != null)
        {
            if (action.IsMagic) AudioManager.Instance.PlaySE(SEType.Magic);
            else AudioManager.Instance.PlaySE(SEType.Attack);
        }

        // 2. Apply Damage and 3. Player Response Visual
        int finalDamage = action.Value;
        bool fullBlock = false;

        if (action.IsMagic)
        {
            CurrentHP -= finalDamage;
        }
        else
        {
            if (Shield >= finalDamage)
            {
                Shield -= finalDamage;
                fullBlock = true;
            }
            else
            {
                finalDamage -= Shield;
                Shield = 0;
                CurrentHP -= finalDamage;
            }
        }

        List<Coroutine> responseCoroutines = new List<Coroutine>();
        if (fullBlock)
        {
            // Trigger Shield Block SE
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(SEType.ShieldBlock);

            // Only Tank flashes Yellow and Shakes (same shake as damage)
            if (tankVisuals != null)
            {
                responseCoroutines.Add(StartCoroutine(tankVisuals.TriggerFlash(Color.yellow, 0.3f)));
                responseCoroutines.Add(StartCoroutine(tankVisuals.ShakeRoutine(0.15f, 0.2f)));
                ShowCombatNumber(action.Value, Color.yellow, TankAnimator.transform.position);
            }
        }
        else
        {
            // Trigger Player Hit SE
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(SEType.Hit);

            // Trigger player damage visual (All characters Red Flash + Shake)
            if (fighterVisuals != null)
            {
                responseCoroutines.Add(StartCoroutine(fighterVisuals.TriggerDamageEffect()));
                ShowCombatNumber(finalDamage, Color.red, FighterAnimator.transform.position);
            }
            if (mageVisuals != null)
            {
                responseCoroutines.Add(StartCoroutine(mageVisuals.TriggerDamageEffect()));
                ShowCombatNumber(finalDamage, Color.red, MageAnimator.transform.position);
            }
            if (tankVisuals != null)
            {
                responseCoroutines.Add(StartCoroutine(tankVisuals.TriggerDamageEffect()));
                ShowCombatNumber(finalDamage, Color.red, TankAnimator.transform.position);
            }
        }

        foreach (var c in responseCoroutines) yield return c;

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySE(SEType.GameOver);
                AudioManager.Instance.PlaySE(SEType.GameOver2);
            }

            // Phase 1: All characters flash Red
            List<Coroutine> gameOverFlashes = new List<Coroutine>();
            Color redFlash = new Color(1f, 0f, 0f, 0.9f);
            if (fighterVisuals != null) gameOverFlashes.Add(StartCoroutine(fighterVisuals.TriggerFlash(redFlash, 0.4f)));
            if (mageVisuals != null) gameOverFlashes.Add(StartCoroutine(mageVisuals.TriggerFlash(redFlash, 0.4f)));
            if (tankVisuals != null) gameOverFlashes.Add(StartCoroutine(tankVisuals.TriggerFlash(redFlash, 0.4f)));
            foreach (var c in gameOverFlashes) yield return c;

            // Phase 2: All characters turn black (silhouette)
            if (fighterVisuals != null) fighterVisuals.SetPersistentColor(Color.black);
            if (mageVisuals != null) mageVisuals.SetPersistentColor(Color.black);
            if (tankVisuals != null) tankVisuals.SetPersistentColor(Color.black);

            // Save results for game over screen
            GameResults.FinalLevel = Level;
            GameResults.FinalExperience = Experience;
            GameResults.WavesWon = Mathf.Max(0, WaveCount - 1);

            yield return new WaitForSeconds(1.0f);
            SceneManager.LoadScene("GameOver");
        }

        UpdateUI();
    }

    public void SpawnWave()
    {
        StartCoroutine(SpawnWaveSequentially());
    }

    private IEnumerator SpawnWaveSequentially()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(SEType.WaveStart);
        foreach (var enemy in ActiveEnemies) if (enemy != null) Destroy(enemy.gameObject);
        ActiveEnemies.Clear();

        WaveCount++;
        int budget = GetMaxLevelForWave(WaveCount);
        int currentBudget = budget;

        int spawnIndex = 0;
        while (currentBudget >= 2) // Minimum level (Slime) is 2
        {
            List<int> validIndices = new List<int>();
            for (int i = 0; i < EnemyPrefabs.Length; i++)
            {
                // Note: prefab.name might have "(Clone)" if it was already instantiated, 
                // but here we are using the array from inspector.
                string name = EnemyPrefabs[i].name;
                if (enemyDefs.ContainsKey(name) && enemyDefs[name].Level <= currentBudget)
                {
                    validIndices.Add(i);
                }
            }

            if (validIndices.Count == 0) break;
            if (spawnIndex >= EnemySpawnPoints.Length) break;

            // Dynamic weighted selection: Weight = Level ^ (-1.0 + WaveCount / WaveWeightShiftFactor)
            // Starts by favoring weaker enemies and shifts to favor stronger ones as waves progress.
            float shiftFactor = (balanceData != null) ? balanceData.WaveWeightShiftFactor : 10.0f;
            float power = -1.0f + (float)WaveCount / shiftFactor;
            float totalWeight = 0;
            List<float> weights = new List<float>();
            foreach (int index in validIndices)
            {
                string name = EnemyPrefabs[index].name;
                int level = enemyDefs[name].Level;
                float weight = Mathf.Pow(level, power);
                weights.Add(weight);
                totalWeight += weight;
            }

            int prefabIndex = validIndices[0];
            float r = Random.value * totalWeight;
            float cumulative = 0;
            for (int i = 0; i < validIndices.Count; i++)
            {
                cumulative += weights[i];
                if (r <= cumulative)
                {
                    prefabIndex = validIndices[i];
                    break;
                }
            }

            GameObject prefab = EnemyPrefabs[prefabIndex];
            GameBalanceData.EnemyDefinition def = enemyDefs[prefab.name];

            currentBudget -= def.Level;

            Vector3 pos = EnemySpawnPoints[spawnIndex].position;
            GameObject enemyObj = Instantiate(prefab, pos, Quaternion.identity);
            EnemyUnit unit = enemyObj.GetComponent<EnemyUnit>();
            if (unit == null) unit = enemyObj.AddComponent<EnemyUnit>();

            // Initialize stats from definition
            unit.Level = def.Level;
            unit.MaxHP = def.HP;
            unit.HP = def.HP;
            unit.AttackPower = def.ATK;
            unit.AttackInterval = def.CD;
            unit.IsMagic = def.IsMagic;

            ActiveEnemies.Add(unit);

            // White fade flash
            CharacterVisuals v = unit.GetComponent<CharacterVisuals>();
            if (v == null) v = unit.gameObject.AddComponent<CharacterVisuals>();
            StartCoroutine(v.FadeFlashRoutine(Color.white, 0.5f));

            spawnIndex++;
            yield return new WaitForSeconds(0.4f);
        }
    }
}
