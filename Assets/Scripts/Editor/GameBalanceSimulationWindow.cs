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

    private void OnGUI()
    {
        if (balanceData == null)
        {
            EditorGUILayout.HelpBox("Select a GameBalanceData asset and click 'Open Simulation Window'.", MessageType.Warning);
            balanceData = (GameBalanceData)EditorGUILayout.ObjectField("Balance Data", balanceData, typeof(GameBalanceData), false);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Simulation & Analysis: " + balanceData.name, EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        DrawPlayerProgressionTable();
        EditorGUILayout.Space(20);
        DrawMonsterAnalysis();
        EditorGUILayout.Space(20);
        DrawWaveSimulator();

        EditorGUILayout.EndScrollView();
    }

    private void DrawPlayerProgressionTable()
    {
        EditorGUILayout.LabelField("Player Progression Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Displays the progression of stats and EXP requirements for each level.", MessageType.Info);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Lv", GUILayout.Width(30));
        EditorGUILayout.LabelField("HP", GUILayout.Width(50));
        EditorGUILayout.LabelField("ATK", GUILayout.Width(50));
        EditorGUILayout.LabelField("Next Req", GUILayout.Width(70));
        EditorGUILayout.LabelField("Gem EXP", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        for (int lv = 1; lv <= 20; lv++)
        {
            int hp = Mathf.RoundToInt(balanceData.PlayerBaseHP + (lv - 1) * balanceData.HPIncreasePerLevel);
            int atk = balanceData.PlayerBaseAttack + (lv - 1) * balanceData.AttackIncreasePerLevel;
            int nextReq = balanceData.ExpBaseRequirement + (lv - 1) * balanceData.ExpIncreasePerLevel;
            int gemExp = Mathf.Max(1, nextReq / balanceData.GemExpDivisor);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(lv.ToString(), GUILayout.Width(30));
            EditorGUILayout.LabelField(hp.ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(atk.ToString(), GUILayout.Width(50));
            EditorGUILayout.LabelField(nextReq.ToString(), GUILayout.Width(70));
            EditorGUILayout.LabelField(gemExp.ToString(), GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }
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

        // Limit attempts to avoid infinite loops if data is misconfigured
        int attempts = 0;
        while (tempBudget >= 2 && attempts < 15)
        {
            var validEnemies = balanceData.EnemyDefinitions.FindAll(e => e.Level > 0 && e.Level <= tempBudget);
            if (validEnemies.Count == 0) break;

            var selected = validEnemies[Random.Range(0, validEnemies.Count)];
            tempBudget -= selected.Level;
            
            if (counts.ContainsKey(selected.Name)) counts[selected.Name]++;
            else counts[selected.Name] = 1;
            attempts++;
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
