using UnityEngine;

/// <summary>
/// 敌人数据 - ScriptableObject用于配置敌人属性
/// </summary>
[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Tower Defense/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("基础信息")]
    [SerializeField] private string enemyName = "Enemy";
    
    [Header("属性")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    
    [Header("奖励")]
    [SerializeField] private int goldReward = 10;
    
    // 公共属性
    public string EnemyName => enemyName;
    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float AttackRange => attackRange;
    public float AttackDamage => attackDamage;
    public float AttackCooldown => attackCooldown;
    public int GoldReward => goldReward;
} 