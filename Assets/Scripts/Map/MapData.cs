using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class MapData
{
    [SerializeField] private int id;
    [SerializeField, MultiLineProperty] private string description;
    [SerializeField] private DifficultyLevel difficulty;

    //对塔的加成
    [Header("塔加成设置"),Range(2, 5)] public int max = 2;

    [SerializeField, PropertyRange(0, "max")]
    private float towerHealthMultiplier = 1f;

    [SerializeField, PropertyRange(0, "max")]
    private float towerAttackMultiplier = 1f;

    [SerializeField, PropertyRange(0, "max")]
    private float towerAttackRangeMultiplier = 1f;

    [SerializeField, PropertyRange(0, "max")]
    private float towerAttackSpeedMultiplier = 1f;

    [SerializeField, PropertyRange(0, "max")]
    private float towerPhysicAttackMultiplier = 1f;

    [SerializeField, PropertyRange(0, "max")]
    private float towerAttackIntervalMultiplier = 1f;

    [SerializeField, PropertyRange(0, "max")]
    private float towerDamageMultiplier = 1f;
}

[System.Serializable]
//难度等级
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Custom
}