using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GameBalanceSimulationWindow : EditorWindow
{
    private GameBalanceData balanceData;
    private int previewWave = 1;
    private int previewPlayerLevel = 1;
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

        if (GUI.changed) Repaint();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Simulation & Analysis: " + balanceData.name, EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // --- PLAYER SECTION ---
        EditorGUILayout.LabelField("--- Player & Combat Analysis ---", EditorStyles.miniBoldLabel);
        previewPlayerLevel = EditorGUILayout.IntSlider("Target Player Level", previewPlayerLevel, 1, 50);
        EditorGUILayout.Space(5);
        DrawPlayerStats();
        EditorGUILayout.Space(10);
        DrawMonsterCombatAnalysis();

        EditorGUILayout.Space(20);

        // --- WAVE SECTION ---
        EditorGUILayout.LabelField("--- Wave & Progression Projection ---", EditorStyles.miniBoldLabel);
        previewWave = EditorGUILayout.IntSlider("Target Wave", previewWave, 1, 100);
        EditorGUILayout.Space(5);
        DrawWaveSimulator();
        EditorGUILayout.Space(10);
        DrawLevelProjection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawPlayerStats()
    {
        EditorGUILayout.LabelField("Player Stats Preview", EditorStyles.boldLabel);
        
        int lv = previewPlayerLevel;
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

    private void DrawMonsterCombatAnalysis()
    {
        EditorGUILayout.LabelField("Monster vs. Player Analysis", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"Combat simulation vs a Lv{previewPlayerLevel} player.", MessageType.Info);

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
            EditorGUILayout.LabelField($"{enemy.Name}: {hitsToKillEnemy:F1} hits to kill / {hitsToKillPlayer:F1} hits to die");
        }
    }

    private void DrawWaveSimulator()
    {
        EditorGUILayout.LabelField("Wave Content Simulator", EditorStyles.boldLabel);
        
        int budget = Mathf.FloorToInt(balanceData.InitialWaveBudget + (previewWave - 1) * balanceData.BudgetIncreasePerWave);
        EditorGUILayout.LabelField($"Budget for Wave {previewWave}: {budget}");

        if (balanceData.EnemyDefinitions == null || balanceData.EnemyDefinitions.Count == 0) return;

        Random.State oldState = Random.state;
        Random.InitState(previewWave * 100);

        string composition = "Example Party: ";
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
        else composition += "None (Budget too low)";

        GUIStyle labelStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
        EditorGUILayout.LabelField(composition, labelStyle);
        Random.state = oldState;
    }

    private void DrawLevelProjection()
    {
        EditorGUILayout.LabelField("Progression Projection", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"Estimates status at end of Wave {previewWave}.", MessageType.Info);

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

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Estimated Player Level: {simulatedLevel}");
        EditorGUILayout.LabelField($"Cumulative Experience: {simulatedExp}");
        EditorGUILayout.EndVertical();
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
