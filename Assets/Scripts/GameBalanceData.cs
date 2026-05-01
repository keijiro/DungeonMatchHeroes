using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameBalanceData", menuName = "Combat/GameBalanceData")]
public class GameBalanceData : ScriptableObject
{
    public float BaseMatchCount = 3.0f;
    public float JunkDivisor = 3.0f;

    public int PlayerBaseHP = 100;
    public int PlayerBaseAttack = 10;
    public float HPIncreasePerLevel = 11.11f; // 100/9
    public float AttackIncreasePerLevel = 1.0f;
public float MagicAttackRatio = 0.33f;

    public int ExpBaseRequirement = 80;
    public int ExpIncreasePerLevel = 26;
    public int GemExpDivisor = 20;
    public int ChestExpDivisor = 8;

    public float HealAttackRatio = 0.6f;
    public int ShieldMaxBlocksToReachMaxHP = 20;

    public List<EnemyDefinition> EnemyDefinitions = new List<EnemyDefinition>();

    public float InitialWaveBudget = 6f;
    public float BudgetIncreasePerWave = 1.2f;
    public float WaveWeightShiftFactor = 10.0f;
    public float FormationPenaltyFactor = 0.75f;

    [System.Serializable]
    public class EnemyDefinition
    {
        public string Name;
        public int Level;
        public int HP;
        public float ATK;
        public float CD;
        public bool IsMagic;
    }
}
