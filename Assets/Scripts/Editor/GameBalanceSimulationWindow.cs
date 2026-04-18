using UnityEngine;
using UnityEditor;

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
        EditorGUILayout.HelpBox("レベルごとのステータスと必要経験値の推移を表示します。", MessageType.Info);

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

    private void DrawMonsterAnalysis()
    {
        EditorGUILayout.LabelField("Monster Combat Analysis (vs Lv 1 Player)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Lv1プレイヤーとの戦闘シミュレーション。何回で倒せるかを表示します。", MessageType.Info);

        int playerAtk = balanceData.PlayerBaseAttack;
        int playerHP = balanceData.PlayerBaseHP;

        if (balanceData.EnemyDefinitions == null || balanceData.EnemyDefinitions.Count == 0)
        {
            EditorGUILayout.HelpBox("敵が定義されていません。", MessageType.Warning);
            return;
        }

        foreach (var enemy in balanceData.EnemyDefinitions)
        {
            if (string.IsNullOrEmpty(enemy.Name)) continue;

            float hitsToKillEnemy = playerAtk > 0 ? (float)enemy.HP / playerAtk : float.PositiveInfinity;
            float hitsToKillPlayer = enemy.ATK > 0 ? (float)playerHP / enemy.ATK : float.PositiveInfinity;

            string analysis = string.Format("{0}: 撃破まで {1:F1} 回 / プレイヤー死亡まで {2:F1} 回", 
                enemy.Name, hitsToKillEnemy, hitsToKillPlayer);
            
            EditorGUILayout.LabelField(analysis);
        }
    }

    private void DrawWaveSimulator()
    {
        EditorGUILayout.LabelField("Wave Simulator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("ウェーブごとの敵出現予算をシミュレーションします。", MessageType.Info);
        
        previewWave = EditorGUILayout.IntSlider("Preview Wave", previewWave, 1, 50);

        int budget = Mathf.FloorToInt(balanceData.InitialWaveBudget + (previewWave - 1) * balanceData.BudgetIncreasePerWave);
        EditorGUILayout.LabelField(string.Format("Wave {0} の予算: {1}", previewWave, budget));

        if (balanceData.EnemyDefinitions == null || balanceData.EnemyDefinitions.Count == 0) return;

        string example = "出現例: ";
        int tempBudget = budget;
        int count = 0;
        foreach (var def in balanceData.EnemyDefinitions)
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
