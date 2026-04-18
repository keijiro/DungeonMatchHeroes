using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameBalanceData", menuName = "Combat/GameBalanceData")]
public class GameBalanceData : ScriptableObject
{
    [Header("Matching Rules")]
    [Tooltip("Effective count += matchCount + (skaCount / divisor)")]
    public float SkaDivisor = 3.0f;

    [Header("Player Scaling (Level 1 Base)")]
    public int PlayerBaseHP = 100;
    public int PlayerBaseAttack = 10;
    public float HPIncreasePerLevel = 11.11f; // 100/9
    public int AttackIncreasePerLevel = 1;
    public float MagicAttackRatio = 0.33f;

    [Header("Experience Scaling")]
    public int ExpBaseRequirement = 80;
    public int ExpIncreasePerLevel = 26;
    [Tooltip("Requirement / divisor = EXP per block")]
    public int GemExpDivisor = 20;
    [Tooltip("Requirement / divisor = EXP per chest")]
    public int ChestExpDivisor = 8;

    [Header("Actions")]
    public float HealAttackRatio = 0.6f;
    [Tooltip("MaxHP / divisor = Shield per block")]
    public int ShieldMaxBlocksToReachMaxHP = 20;

    [Header("Enemies")]
    public List<EnemyDefinition> EnemyDefinitions = new List<EnemyDefinition>();

    [Header("Wave Balancing")]
    public float InitialWaveBudget = 6f;
    public float BudgetIncreasePerWave = 1.2f;
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
