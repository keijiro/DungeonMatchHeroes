using UnityEngine;
using UnityEngine.UIElements;
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
    public int MaxHP = 100;
    public int CurrentHP;
    public int Shield;
    public int Experience;
    public bool HasKey;

    [Header("Player Power (Base)")]
    public int BaseAttack = 5;
    public int BaseMagicAttack = 3;
    public int BaseHeal = 3;
    public int BaseShield = 2;
    public int BaseExp = 10;
    public int KeyBonusExp = 50;

    [Header("Enemy Settings")]
    public GameObject[] EnemyPrefabs;
    public Transform[] EnemySpawnPoints;
    public List<EnemyUnit> ActiveEnemies = new List<EnemyUnit>();

    private LinkedList<CombatAction> eventQueue = new LinkedList<CombatAction>();
    private bool isProcessingQueue = false;

    [Header("UI References")]
    public UIDocument HUD;
    private VisualElement hpBarFill;
    private Label hpText;
    private Label shieldText;
    private Label expText;
    private Label keyLabel;
    private VisualElement notificationLayer;

    [Header("Player Animators")]
    public Animator FighterAnimator;
    public Animator MageAnimator;
    public Animator TankAnimator;

    private CharacterVisuals fighterVisuals;
    private CharacterVisuals mageVisuals;
    private CharacterVisuals tankVisuals;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

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
        hpText = root.Q<Label>("hp-text");
        shieldText = root.Q<Label>("shield-text");
        expText = root.Q<Label>("exp-text");
        keyLabel = root.Q<Label>("key-label");
        notificationLayer = root.Q<VisualElement>("notification-layer");
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (hpBarFill != null)
        {
            float percentage = (float)CurrentHP / MaxHP * 100f;
            hpBarFill.style.width = new Length(percentage, LengthUnit.Percent);
        }
        if (hpText != null) hpText.text = $"{CurrentHP}/{MaxHP}";
        if (shieldText != null) shieldText.text = Shield.ToString();
        if (expText != null) expText.text = Experience.ToString();
        if (keyLabel != null) keyLabel.text = HasKey ? "KEY: YES" : "KEY: NO";
    }

    private void Start()
    {
        SpawnWave();
        StartCoroutine(QueueProcessor());
    }

    public void AddPlayerAction(GridManager.BlockType type, int matchCount, int skaCount, Vector3 worldPos)
    {
        int effectiveCount = matchCount + (skaCount / 3);
        if (effectiveCount <= 0) return;

        CombatAction action = new CombatAction();
        switch (type)
        {
            case GridManager.BlockType.Sword:
                action.Type = CombatActionType.PlayerAttack;
                action.Value = effectiveCount * BaseAttack;
                break;
            case GridManager.BlockType.Magic:
                action.Type = CombatActionType.PlayerMagicAttack;
                action.Value = effectiveCount * BaseMagicAttack;
                break;
            case GridManager.BlockType.Heal:
                action.Type = CombatActionType.PlayerHeal;
                action.Value = effectiveCount * BaseHeal;
                break;
            case GridManager.BlockType.Shield:
                action.Type = CombatActionType.PlayerShield;
                action.Value = effectiveCount * BaseShield;
                break;
            case GridManager.BlockType.Gem:
                action.Type = CombatActionType.PlayerExp;
                action.Value = effectiveCount * BaseExp;
                break;
            case GridManager.BlockType.Key:
                action.Type = CombatActionType.PlayerKey;
                action.Value = KeyBonusExp; 
                break;
        }

        // Show UI notification
        ShowActionNotification(action.Type, action.Value, worldPos);

        // Priority: Insert player actions at the head
        eventQueue.AddFirst(action);
        Debug.Log($"Added Player Action: {action.Type} Value: {action.Value}. Interrupting queue.");
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
                text = $"Key! +{value} EXP";
                color = Color.yellow;
                break;
        }

        label.text = text;
        label.style.color = color;
        notificationLayer.Add(label);

        StartCoroutine(AnimateNotification(label));
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
        Debug.Log($"Added Enemy Action: {enemy.gameObject.name} Damage: {damage} (Magic: {isMagic})");
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

    private IEnumerator ExecuteAction(CombatAction action)
    {
        isProcessingQueue = true;
        
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
                List<Coroutine> healCoroutines = new List<Coroutine>();
                Color healColor = new Color(0f, 1f, 0f, 0.8f);
                if (fighterVisuals != null) healCoroutines.Add(StartCoroutine(fighterVisuals.TriggerFlash(healColor, 0.3f)));
                if (mageVisuals != null) healCoroutines.Add(StartCoroutine(mageVisuals.TriggerFlash(healColor, 0.3f)));
                if (tankVisuals != null) healCoroutines.Add(StartCoroutine(tankVisuals.TriggerFlash(healColor, 0.3f)));
                foreach (var c in healCoroutines) yield return c;

                CurrentHP = Mathf.Min(MaxHP, CurrentHP + action.Value);
                Debug.Log($"Healed {action.Value}. HP: {CurrentHP}");
                yield return new WaitForSeconds(0.2f);
                break;
            case CombatActionType.PlayerShield:
                // Tank flashes Cyan
                if (tankVisuals != null) yield return StartCoroutine(tankVisuals.TriggerFlash(new Color(0f, 1f, 1f, 0.8f), 0.3f));

                Shield += action.Value;
                Debug.Log($"Gained {action.Value} Shield. Total: {Shield}");
                yield return new WaitForSeconds(0.2f);
                break;
case CombatActionType.PlayerExp:
                Experience += action.Value;
                Debug.Log($"Gained {action.Value} EXP. Total: {Experience}");
                yield return new WaitForSeconds(0.1f);
                break;
            case CombatActionType.PlayerKey:
                if (!HasKey)
                {
                    HasKey = true;
                    Debug.Log("Key Obtained!");
                }
                Experience += action.Value; 
                yield return new WaitForSeconds(0.3f);
                break;
            case CombatActionType.EnemyAttack:
                yield return StartCoroutine(HandleEnemyAttack(action));
                break;
        }

        UpdateUI();
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

        // 3. Apply Damage and Enemy Reaction
        EnemyUnit target = ActiveEnemies[0];
        if (target != null)
        {
            Debug.Log($"Player attacks {target.name} for {damage} damage.");
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

        // 3. Apply Damage to all and Enemy Reactions
        Debug.Log($"Mage casts AOE Magic for {damage} damage to ALL enemies.");
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
            StartCoroutine(SpawnWaveWithDelay());
        }
    }

    private IEnumerator SpawnWaveWithDelay()
    {
        yield return new WaitForSeconds(1.0f);
        SpawnWave();
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
            // Only Tank flashes Yellow
            if (tankVisuals != null) responseCoroutines.Add(StartCoroutine(tankVisuals.TriggerFlash(Color.yellow, 0.3f)));
        }
        else
        {
            // Trigger player damage visual (All characters Red Flash + Shake)
            if (fighterVisuals != null) responseCoroutines.Add(StartCoroutine(fighterVisuals.TriggerDamageEffect()));
            if (mageVisuals != null) responseCoroutines.Add(StartCoroutine(mageVisuals.TriggerDamageEffect()));
            if (tankVisuals != null) responseCoroutines.Add(StartCoroutine(tankVisuals.TriggerDamageEffect()));
        }
        
        foreach (var c in responseCoroutines) yield return c;

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            Debug.LogError("Game Over (Party Wiped) - Restarting prototype stats.");
            yield return new WaitForSeconds(1.0f);
            CurrentHP = MaxHP;
            Shield = 0;
            SpawnWave(); 
        }

        UpdateUI();
    }

    public void SpawnWave()
    {
        foreach (var enemy in ActiveEnemies) if (enemy != null) Destroy(enemy.gameObject);
        ActiveEnemies.Clear();

        int count = Random.Range(2, 6); 
        for (int i = 0; i < count; i++)
        {
            if (EnemyPrefabs.Length == 0) break;
            
            GameObject prefab = EnemyPrefabs[Random.Range(0, EnemyPrefabs.Length)];
            Vector3 pos = EnemySpawnPoints.Length > i ? EnemySpawnPoints[i].position : new Vector3(i * 1.5f - 3f, 3.5f, 0);
            
            GameObject enemyObj = Instantiate(prefab, pos, Quaternion.identity);
            EnemyUnit unit = enemyObj.GetComponent<EnemyUnit>();
            if (unit == null) unit = enemyObj.AddComponent<EnemyUnit>();
            
            ActiveEnemies.Add(unit);
        }
        Debug.Log($"New wave spawned: {count} enemies.");
    }
}
