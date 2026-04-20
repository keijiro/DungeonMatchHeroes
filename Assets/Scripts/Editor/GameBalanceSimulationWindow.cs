using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

public class GameBalanceSimulationWindow : EditorWindow
{
    private GameBalanceData balanceData;
    private int previewWave = 1;
    private int previewPlayerLevel = 1;

    private VisualElement root;
    private ObjectField dataField;
    private SliderInt levelSlider;
    private SliderInt waveSlider;

    [MenuItem("Window/Combat/Balance Simulator")]
    public static void ShowWindow()
    {
        GetWindow<GameBalanceSimulationWindow>("Balance Simulator");
    }

    public static void Open(GameBalanceData data)
    {
        GameBalanceSimulationWindow window = GetWindow<GameBalanceSimulationWindow>("Balance Simulator");
        window.balanceData = data;
        window.RefreshUI();
        window.Show();
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += RefreshUI;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= RefreshUI;
    }

    public void CreateGUI()
    {
        root = rootVisualElement;

        // Load UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Editor/BalanceSimulator.uxml");
        if (visualTree == null)
        {
            root.Add(new Label("Could not find Assets/UI/Editor/BalanceSimulator.uxml"));
            return;
        }
        visualTree.CloneTree(root);

        // Bind Elements
        dataField = root.Q<ObjectField>("balanceDataField");
        dataField.objectType = typeof(GameBalanceData);
        dataField.value = balanceData;
        dataField.RegisterValueChangedCallback(evt => {
            balanceData = evt.newValue as GameBalanceData;
            RefreshUI();
        });

        levelSlider = root.Q<SliderInt>("playerLevelSlider");
        if (levelSlider != null)
        {
            levelSlider.value = previewPlayerLevel;
            levelSlider.RegisterValueChangedCallback(evt => {
                previewPlayerLevel = evt.newValue;
                RefreshUI();
            });
        }

        waveSlider = root.Q<SliderInt>("waveSlider");
        if (waveSlider != null)
        {
            waveSlider.value = previewWave;
            waveSlider.RegisterValueChangedCallback(evt => {
                previewWave = evt.newValue;
                RefreshUI();
            });
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (root == null) return;

        bool hasData = balanceData != null;
        
        // Update basic field values
        if (dataField != null && dataField.value != balanceData) 
        {
            dataField.SetValueWithoutNotify(balanceData);
        }

        if (!hasData) return;

        UpdatePlayerStats();
        UpdateMonsterAnalysis();
        UpdateWaveSimulator();
        UpdateProgressionProjection();
    }

    private void UpdatePlayerStats()
    {
        int lv = previewPlayerLevel;
        int hp = Mathf.RoundToInt(balanceData.PlayerBaseHP + (lv - 1) * balanceData.HPIncreasePerLevel);
        int atk = balanceData.PlayerBaseAttack + (lv - 1) * balanceData.AttackIncreasePerLevel;
        int nextReq = balanceData.ExpBaseRequirement + (lv - 1) * balanceData.ExpIncreasePerLevel;

        var hpLabel = root.Q<Label>("playerHpValue");
        if (hpLabel != null) hpLabel.text = hp.ToString();

        var atkLabel = root.Q<Label>("playerAtkValue");
        if (atkLabel != null) atkLabel.text = atk.ToString();

        var expLabel = root.Q<Label>("playerExpValue");
        if (expLabel != null) expLabel.text = $"{nextReq} EXP";
    }

    private void UpdateMonsterAnalysis()
    {
        var container = root.Q<VisualElement>("monsterAnalysisList");
        if (container == null) return;
        
        container.Clear();

        int playerAtk = balanceData.PlayerBaseAttack + (previewPlayerLevel - 1) * balanceData.AttackIncreasePerLevel;
        int playerHP = Mathf.RoundToInt(balanceData.PlayerBaseHP + (previewPlayerLevel - 1) * balanceData.HPIncreasePerLevel);

        if (balanceData.EnemyDefinitions == null || balanceData.EnemyDefinitions.Count == 0)
        {
            container.Add(new Label("No enemies defined."));
            return;
        }

        foreach (var enemy in balanceData.EnemyDefinitions)
        {
            if (string.IsNullOrEmpty(enemy.Name)) continue;

            float hitsToKillEnemy = playerAtk > 0 ? (float)enemy.HP / playerAtk : float.PositiveInfinity;
            float hitsToKillPlayer = enemy.ATK > 0 ? (float)playerHP / enemy.ATK : float.PositiveInfinity;

            var item = new VisualElement();
            item.AddToClassList("enemy-analysis-item");

            var nameLabel = new Label(enemy.Name);
            nameLabel.AddToClassList("enemy-analysis-name");
            item.Add(nameLabel);

            var statsLabel = new Label($"{hitsToKillEnemy:F1} hits to kill / {hitsToKillPlayer:F1} hits to die");
            statsLabel.AddToClassList("enemy-analysis-stats");
            item.Add(statsLabel);

            container.Add(item);
        }
    }

    private void UpdateWaveSimulator()
    {
        int budget = Mathf.FloorToInt(balanceData.InitialWaveBudget + (previewWave - 1) * balanceData.BudgetIncreasePerWave);
        
        var budgetLabel = root.Q<Label>("waveBudgetValue");
        if (budgetLabel != null) budgetLabel.text = budget.ToString();

        string composition = "";
        if (balanceData.EnemyDefinitions != null && balanceData.EnemyDefinitions.Count > 0)
        {
            Random.State oldState = Random.state;
            Random.InitState(previewWave * 100);

            int tempBudget = budget;
            int spawnCount = 0;
            int maxSpawn = 5;
            Dictionary<string, int> counts = new Dictionary<string, int>();
            float power = -1.0f + (float)previewWave / 10.0f;

            while (tempBudget >= 2 && spawnCount < maxSpawn)
            {
                var validEnemies = balanceData.EnemyDefinitions.FindAll(e => e.Level > 0 && e.Level <= tempBudget);
                if (validEnemies.Count == 0) break;

                float totalWeight = 0;
                foreach (var e in validEnemies) totalWeight += Mathf.Pow(e.Level, power);
                float r = Random.value * totalWeight;
                float cumulative = 0;
                GameBalanceData.EnemyDefinition selected = validEnemies[0];
                foreach (var e in validEnemies)
                {
                    cumulative += Mathf.Pow(e.Level, power);
                    if (r <= cumulative) { selected = e; break; }
                }
                tempBudget -= selected.Level;
                if (counts.ContainsKey(selected.Name)) counts[selected.Name]++;
                else counts[selected.Name] = 1;
                spawnCount++;
            }

            if (counts.Count > 0)
            {
                foreach (var pair in counts) composition += $"{pair.Value}x {pair.Key}, ";
                composition = composition.TrimEnd(' ', ',');
            }
            else composition = "None (Budget too low)";

            Random.state = oldState;
        }
        else
        {
            composition = "No enemies defined.";
        }

        var partyLabel = root.Q<Label>("partyCompositionText");
        if (partyLabel != null) partyLabel.text = composition;
    }

    private void UpdateProgressionProjection()
    {
        int simulatedLevel = 1;
        int simulatedExp = 0;

        for (int w = 1; w <= previewWave; w++)
        {
            int budget = Mathf.FloorToInt(balanceData.InitialWaveBudget + (w - 1) * balanceData.BudgetIncreasePerWave);
            int enemyExp = 5 * budget;
            int currentReq = balanceData.ExpBaseRequirement + (simulatedLevel - 1) * balanceData.ExpIncreasePerLevel;
            simulatedExp += (enemyExp + Mathf.RoundToInt(currentReq * 0.3f));

            while (simulatedExp >= GetThresholdForLevel(simulatedLevel + 1)) simulatedLevel++;
        }

        var estLevelLabel = root.Q<Label>("estLevelValue");
        if (estLevelLabel != null) estLevelLabel.text = simulatedLevel.ToString();

        var cumExpLabel = root.Q<Label>("cumulativeExpValue");
        if (cumExpLabel != null) cumExpLabel.text = simulatedExp.ToString();
    }

    private int GetThresholdForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        float total = 0;
        for (int i = 1; i < targetLevel; i++)
            total += (float)balanceData.ExpBaseRequirement + (i - 1) * balanceData.ExpIncreasePerLevel;
        return Mathf.RoundToInt(total);
    }
}
