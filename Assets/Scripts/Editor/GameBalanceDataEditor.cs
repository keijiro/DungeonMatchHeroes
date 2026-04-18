using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameBalanceData))]
public class GameBalanceDataEditor : Editor
{
    private GUIStyle descriptionStyle;

    private void OnEnable()
    {
        descriptionStyle = new GUIStyle(EditorStyles.miniLabel);
        descriptionStyle.wordWrap = true;
        descriptionStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
    }

    public override void OnInspectorGUI()
    {
        GameBalanceData data = (GameBalanceData)target;

        serializedObject.Update();

        if (GUILayout.Button("Open Simulation Window", GUILayout.Height(30)))
        {
            GameBalanceSimulationWindow.Open(data);
        }

        EditorGUILayout.Space(10);

        DrawFieldWithDescription("SkaDivisor", "お邪魔ブロック1個が何個の通常マッチとしてカウントされるか（3.0なら3個で1マッチ分）。");
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Player Scaling", EditorStyles.boldLabel);
        DrawFieldWithDescription("PlayerBaseHP", "レベル1時点での最大ヒットポイント。");
        DrawFieldWithDescription("PlayerBaseAttack", "レベル1時点での物理攻撃力。");
        DrawFieldWithDescription("HPIncreasePerLevel", "レベルが1上がるごとに加算される最大HPの量。");
        DrawFieldWithDescription("AttackIncreasePerLevel", "レベルが1上がるごとに加算される物理攻撃力の量。");
        DrawFieldWithDescription("MagicAttackRatio", "物理攻撃力に対する魔法攻撃力の割合（0.33なら33%）。");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Experience Scaling", EditorStyles.boldLabel);
        DrawFieldWithDescription("ExpBaseRequirement", "レベル1から2へ上がるために必要な経験値。");
        DrawFieldWithDescription("ExpIncreasePerLevel", "レベルが上がるごとに必要経験値に加算される量。");
        DrawFieldWithDescription("GemExpDivisor", "1レベル分の必要経験値をこの数で割った値がGem1個あたりのEXPになります。");
        DrawFieldWithDescription("ChestExpDivisor", "1レベル分の必要経験値をこの数で割った値が宝箱1個あたりのEXPになります。");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        DrawFieldWithDescription("HealAttackRatio", "攻撃力に対する回復量の倍率（0.6なら攻撃力の60%回復）。");
        DrawFieldWithDescription("ShieldMaxBlocksToReachMaxHP", "シールドを最大HP分貯めるのに必要なブロック数。");

        EditorGUILayout.Space(10);
        DrawFieldWithDescription("EnemyDefinitions", "モンスターの種類ごとの基本設定リスト。");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Wave Balancing", EditorStyles.boldLabel);
        DrawFieldWithDescription("InitialWaveBudget", "ウェーブ1時点での敵パーティーの合計レベル上限。");
        DrawFieldWithDescription("BudgetIncreasePerWave", "ウェーブが進むごとに増加する予算。");
        DrawFieldWithDescription("FormationPenaltyFactor", "後方に配置された敵の攻撃頻度の減衰率（0.75なら25%減少）。");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawFieldWithDescription(string propertyName, string description)
    {
        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.PropertyField(prop, true);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(description, descriptionStyle);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }
    }
}
