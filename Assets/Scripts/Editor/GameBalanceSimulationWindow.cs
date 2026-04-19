using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GameBalanceSimulationWindow : EditorWindow
{
private GameBalanceData balanceData;
    private int previewWave = 1;
    private Vector2 scrollPos;

    public static void Open(GameBalanceData data)
    {
        GameBalanceSimulationWindow window = GetWindow<GameBalanceSimulationWindow>("Balance Simulator");
        window.balanceData = data;
        window.Show();
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        Repaint();
    }

    private void OnGUI()
    {
        if (balanceData == null)
        {
            EditorGUILayout.HelpBox("Select a GameBalanceData asset and click 'Open Simulation Window'.", MessageType.Warning);
            balanceData = (GameBalanceData)EditorGUILayout.ObjectField("Balance Data", balanceData, typeof(GameBalanceData), false);
            return;
        }

        // Repaint if the target asset has been modified in the inspector
        if (GUI.changed)
        {
            Repaint();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Simulation & Analysis: " + balanceData.name, EditorStyles.boldLabel);
EditorGUILayout.Space(10);

        DrawPlayerProgressionTable();
        EditorGUILayout.Space(20);
        DrawMonsterAnalysis();
        EditorGUILayout.Space(20);
        DrawWaveSimulator();
        EditorGUILayout.Space(20);
        DrawLevelProjection();

        EditorGUILayout.EndScrollView();
        }

        private void DrawLevelProjection()
        {
            EditorGUILayout.LabelField("Player Level Projection", EditorStyles.boldLabel);
            previewProjectionWave = EditorGUILayout.IntSlider("Target Wave", previewProjectionWave, 1, 100);
            EditorGUILayout.HelpBox("Estimates the player's status at the end of the selected wave.", MessageType.Info);

            int simulatedLevel = 1;
            int simulatedExp = 0;
            int targetWave = previewProjectionWave;

            for (int w = 1; w <= targetWave; w++)
            {
                int budget = Mathf.FloorToInt(balanceData.InitialWaveBudget + (w - 1) * balanceData.BudgetIncreasePerWave);
                int enemyExp = 5 * budget;
                int currentReq = balanceData.ExpBaseRequirement + (simulatedLevel - 1) * balanceData.ExpIncreasePerLevel;
                int puzzleExp = Mathf.RoundToInt(currentReq * 0.3f);
                simulatedExp += (enemyExp + puzzleExp);

                while (simulatedExp >= GetThresholdForLevel(simulatedLevel + 1))
                {
                    simulatedLevel++;
                }
            }

            int finalBudget = Mathf.FloorToInt(balanceData.InitialWaveBudget + (targetWave - 1) * balanceData.BudgetIncreasePerWave);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Estimated Player Level: {simulatedLevel}");
            EditorGUILayout.LabelField($"Cumulative Experience: {simulatedExp}");
            EditorGUILayout.LabelField($"Wave Budget (Difficulty): {finalBudget}");
            EditorGUILayout.EndVertical();
        }

        private int GetThresholdForLevel(int targetLevel)
        {
        if (targetLevel <= 1) return 0;
        float total = 0;
        for (int i = 1; i < targetLevel; i++)
        {
            total += (float)balanceData.ExpBaseRequirement + (i - 1) * balanceData.ExpIncreasePerLevel;
        }
        return Mathf.RoundToInt(total);
        }

    private int previewProgressionLevel = 1;
    private int previewProjectionWave = 1;

    private void DrawPlayerProgressionTable()
    {
        EditorGUILayout.LabelField("Player Progression Preview", EditorStyles.boldLabel);
        previewProgressionLevel = EditorGUILayout.IntSlider("Target Level", previewProgressionLevel, 1, 50);
        EditorGUILayout.HelpBox("Displays stats and requirements for the selected level.", MessageType.Info);

        int lv = previewProgressionLevel;
        int hp = Mathf.RoundToInt(balanceData.PlayerBaseHP + (lv - 1) * balanceData.HPIncreasePerLevel);
        int atk = balanceData.PlayerBaseAttack + (lv - 1) * balanceData.AttackIncreasePerLevel;
        int nextReq = balanceData.ExpBaseRequirement + (lv - 1) * balanceData.ExpIncreasePerLevel;
        int gemExp = Mathf.Max(1, nextReq / balanceData.GemExpDivisor);
        int totalToNext = GetThresholdForLevel(lv + 1);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"HP: {hp}");
        EditorGUILayout.LabelField($"Attack: {atk}");
        EditorGUILayout.LabelField($"Next Level Req: {nextReq} EXP");
        EditorGUILayout.LabelField($"Gem/Key Match Value: ~{gemExp} EXP");
        EditorGUILayout.LabelField($"Cumulative EXP for Lv {lv+1}: {totalToNext}");
        EditorGUILayout.EndVertical();
    }

    private int previewPlayerLevel = 1;

    private void DrawMonsterAnalysis()
    {
        EditorGUILayout.LabelField("Monster Combat Analysis", EditorStyles.boldLabel);
        previewPlayerLevel = EditorGUILayout.IntSlider("Player Level", previewPlayerLevel, 1, 20);
        EditorGUILayout.HelpBox(string.Format("Combat simulation vs a Lv{0} player. Shows how many hits are needed to kill/die.", previewPlayerLevel), MessageType.Info);

        int playerAtk = balanceData.PlayerBaseAttack + (previewPlayerLevel - 1) * balanceData.AttackIncreasePerLevel;
        int playerHP = Mathf.RoundToInt(balanceData.PlayerBaseHP + (previewPlayerLevel - 1) * balanceData.HPIncreasePerLevel);

        if (balanceData.EnemyDefinitions == null || balanceData.EnemyDefinitions.Count == 0)
        {
            EditorGUILayout.HelpBox("No enemies defined.", MessageType.Warning);
            return;
        }

        foreach (var enemy in balanceData.EnemyDefinitions)
        {
            if (string.IsNullOrEmpty(enemy.Name)) continue;

            float hitsToKillEnemy = playerAtk > 0 ? (float)enemy.HP / playerAtk : float.PositiveInfinity;
            float hitsToKillPlayer = enemy.ATK > 0 ? (float)playerHP / enemy.ATK : float.PositiveInfinity;

            string analysis = string.Format("{0}: {1:F1} hits to kill / {2:F1} hits to die", 
                enemy.Name, hitsToKillEnemy, hitsToKillPlayer);
            
            EditorGUILayout.LabelField(analysis);
        }
    }

    private void DrawWaveSimulator()
    {
        EditorGUILayout.LabelField("Wave Simulator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Simulates the enemy spawn budget for each wave.", MessageType.Info);
        
        previewWave = EditorGUILayout.IntSlider("Preview Wave", previewWave, 1, 50);

        int budget = Mathf.FloorToInt(balanceData.InitialWaveBudget + (previewWave - 1) * balanceData.BudgetIncreasePerWave);
        EditorGUILayout.LabelField(string.Format("Budget for Wave {0}: {1}", previewWave, budget));

        if (balanceData.EnemyDefinitions == null || balanceData.EnemyDefinitions.Count == 0) return;

        // Save current random state and init with a deterministic seed for this wave
        Random.State oldState = Random.state;
        Random.InitState(previewWave * 100);

        string composition = "Example Party: ";
        int tempBudget = budget;
        Dictionary<string, int> counts = new Dictionary<string, int>();

        int spawnCount = 0;
        int maxSpawn = 5; // Matches EnemySpawnPoints.Length in CombatManager

        // Matching CombatManager's dynamic weighting logic
        // Starts negative (favor weak) -> zero (uniform) -> positive (favor strong)
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
                if (r <= cumulative)
                {
                    selected = e;
                    break;
                }
            }

            tempBudget -= selected.Level;
            
            if (counts.ContainsKey(selected.Name)) counts[selected.Name]++;
            else counts[selected.Name] = 1;
            
            spawnCount++;
        }

        if (counts.Count > 0)
        {
            foreach (var pair in counts)
            {
                composition += string.Format("{0}x {1}, ", pair.Value, pair.Key);
            }
            composition = composition.TrimEnd(' ', ',');
        }
        else
        {
            composition += "None (Budget too low)";
        }

        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.wordWrap = true;
        EditorGUILayout.LabelField(composition, labelStyle);
        
        // Restore random state
Random.state = oldState;
    }
}
