using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameBalanceData))]
public class GameBalanceDataEditor : Editor
{
    private GUIStyle descriptionStyle;

    private void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        if (descriptionStyle == null)
        {
            descriptionStyle = new GUIStyle(EditorStyles.miniLabel);
            descriptionStyle.wordWrap = true;
            descriptionStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        }

        GameBalanceData data = (GameBalanceData)target;

        serializedObject.Update();

        if (GUILayout.Button("Open Simulation Window", GUILayout.Height(30)))
        {
            GameBalanceSimulationWindow.Open(data);
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Matching Rules", EditorStyles.boldLabel);
        DrawFieldWithDescription("BaseMatchCount", "The standard number of blocks that count as one 'unit' of action. (Usually 3).");
        DrawFieldWithDescription("JunkDivisor", "The multiplier for Junk blocks. (e.g., if set to 3.0, 3 Junk blocks count as 1 normal match).");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Player Scaling", EditorStyles.boldLabel);
DrawFieldWithDescription("PlayerBaseHP", "Maximum HP at Level 1.");
        DrawFieldWithDescription("PlayerBaseAttack", "Base Physical Attack power at Level 1.");
        DrawFieldWithDescription("HPIncreasePerLevel", "Amount of Max HP added per level up.");
        DrawFieldWithDescription("AttackIncreasePerLevel", "Amount of Physical Attack power added per level up.");
        DrawFieldWithDescription("MagicAttackRatio", "Ratio of Magic Attack power relative to Physical Attack. (e.g., 0.33 means 33%).");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Experience Scaling", EditorStyles.boldLabel);
        DrawFieldWithDescription("ExpBaseRequirement", "EXP required to level up from 1 to 2.");
        DrawFieldWithDescription("ExpIncreasePerLevel", "Amount added to the EXP requirement for each subsequent level.");
        DrawFieldWithDescription("GemExpDivisor", "Next level's total EXP requirement divided by this number equals the EXP per Gem block.");
        DrawFieldWithDescription("ChestExpDivisor", "Next level's total EXP requirement divided by this number equals the EXP per treasure chest.");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        DrawFieldWithDescription("HealAttackRatio", "Multiplier for Heal amount based on Attack power. (e.g., 0.6 means 60% of Attack).");
        DrawFieldWithDescription("ShieldMaxBlocksToReachMaxHP", "Number of blocks required to reach full shield (equal to Max HP).");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Enemies", EditorStyles.boldLabel);
        DrawFieldWithDescription("EnemyDefinitions", "List of base settings for each monster type.");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Wave Balancing", EditorStyles.boldLabel);
        DrawFieldWithDescription("InitialWaveBudget", "The combined level budget for the enemy party at Wave 1.");
        DrawFieldWithDescription("BudgetIncreasePerWave", "The increase in level budget for each subsequent wave.");
        DrawFieldWithDescription("WaveWeightShiftFactor", "Controls how fast the game shifts from weak to strong enemies. Higher = Slower shift.");
        DrawFieldWithDescription("FormationPenaltyFactor", "The attack frequency reduction factor for enemies in the back row. (e.g., 0.75 means 25% slower).");

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            // Force the simulation window to repaint if it is open, without stealing focus
            RepaintSimulatorIfOpen();
        }
        }

        private void RepaintSimulatorIfOpen()
        {
            GameBalanceSimulationWindow[] windows = Resources.FindObjectsOfTypeAll<GameBalanceSimulationWindow>();
            if (windows != null)
            {
                foreach (var window in windows)
                {
                    window.RefreshUI();
                    window.Repaint();
                }
            }
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
