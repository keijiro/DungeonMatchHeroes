using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameBalanceData))]
public class GameBalanceDataEditor : Editor
{
    private int previewWave = 1;

    public override void OnInspectorGUI()
    {
        GameBalanceData data = (GameBalanceData)target;

        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Simulation & Analysis", EditorStyles.boldLabel);

        DrawPlayerProgressionTable(data);
        EditorGUILayout.Space(10);
        DrawMonsterAnalysis(data);
        EditorGUILayout.Space(10);
        DrawWaveSimulator(data);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(data);
        }
    }

    private void DrawPlayerProgressionTable(GameBalanceData data)
    {
        EditorGUILayout.LabelField("Player Progression Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Shows stats and EXP requirements for each level.", MessageType.Info);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Lv", GUILayout.Width(30));
        EditorGUILayout.LabelField("HP", GUILayout.Width(50));
        EditorGUILayout.LabelField("ATK", GUILayout.Width(50));
        EditorGUILayout.LabelField("Next Req", GUILayout.Width(70));
        EditorGUILayout.LabelField("Gem EXP", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        for (int lv = 1; lv <= 15; lv++)
        {
            int hp = Mathf.RoundToInt(data.PlayerBaseHP + (lv - 1) * data.HPIncreasePerLevel);
            int atk = data.PlayerBaseAttack + (lv - 1) * data.AttackIncreasePerLevel;
            int nextReq = data.ExpBaseRequirement + (lv - 1) * data.ExpIncreasePerLevel;
            int gemExp = Mathf.Max(1, nextReq / data.GemExpDivisor);

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

    private void DrawMonsterAnalysis(GameBalanceData data)
    {
        EditorGUILayout.LabelField("Monster Combat Analysis (vs Lv 1 Player)", EditorStyles.boldLabel);
        
        int playerAtk = data.PlayerBaseAttack;
        int playerHP = data.PlayerBaseHP;

        if (data.EnemyDefinitions == null || data.EnemyDefinitions.Count == 0)
        {
            EditorGUILayout.HelpBox("No enemies defined.", MessageType.Warning);
            return;
        }

        foreach (var enemy in data.EnemyDefinitions)
        {
            if (string.IsNullOrEmpty(enemy.Name)) continue;

            float hitsToKillEnemy = playerAtk > 0 ? (float)enemy.HP / playerAtk : float.PositiveInfinity;
            float hitsToKillPlayer = enemy.ATK > 0 ? (float)playerHP / enemy.ATK : float.PositiveInfinity;

            string analysis = string.Format("{0}: Takes {1:F1} hits to kill. Kills player in {2:F1} hits.", 
                enemy.Name, hitsToKillEnemy, hitsToKillPlayer);
            
            EditorGUILayout.LabelField(analysis);
        }
    }

    private void DrawWaveSimulator(GameBalanceData data)
    {
        EditorGUILayout.LabelField("Wave Simulator", EditorStyles.boldLabel);
        previewWave = EditorGUILayout.IntSlider("Preview Wave", previewWave, 1, 50);

        int budget = Mathf.FloorToInt(data.InitialWaveBudget + (previewWave - 1) * data.BudgetIncreasePerWave);
        EditorGUILayout.LabelField(string.Format("Budget for Wave {0}: {1}", previewWave, budget));

        if (data.EnemyDefinitions == null || data.EnemyDefinitions.Count == 0) return;

        // Show examples of what could fit in this budget
        string example = "Examples: ";
        int tempBudget = budget;
        int count = 0;
        foreach (var def in data.EnemyDefinitions)
        {
            if (def.Level <= 0) continue;
            if (def.Level <= tempBudget)
            {
                int num = tempBudget / def.Level;
                example += string.Format("{0}x {1}, ", num, def.Name);
                if (++count > 2) break;
            }
        }
        EditorGUILayout.LabelField(example.TrimEnd(' ', ','));
    }
}
