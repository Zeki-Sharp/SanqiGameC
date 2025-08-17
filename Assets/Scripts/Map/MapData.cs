using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class MapData
{
    #region 基础属性
    [SerializeField] public int id;
    [SerializeField] public string name;
   
    [SerializeField, MultiLineProperty] public string description;
    [SerializeField] public DifficultyLevel difficulty;
    //创建item的数量限定
    [SerializeField]
    [Range(1, 5)]
    public int itemLimitCount;
    public string Description
    {
        get
        {
            return description;
        }
    }
    public DifficultyLevel Difficulty
    {
        get
        {
            return difficulty;
        }
    }
    #endregion
    #region 我方防御塔设置
    //对塔的加成
    [SerializeField, Header("防御塔设置"),Range(2, 5)] private int max = 2;
    public int Max => max; // 添加访问器

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
    
    public float GetTowerHealthMultiplier()
    {
        return towerHealthMultiplier;
    }
    public float GetTowerAttackMultiplier()
    {
        return towerAttackMultiplier;
    }
    public float GetTowerAttackRangeMultiplier()
    {
        return towerAttackRangeMultiplier;
    }
    public float GetTowerAttackSpeedMultiplier()
    {
        return towerAttackSpeedMultiplier;
    }
    public float GetTowerPhysicAttackMultiplier()
    {
        return towerPhysicAttackMultiplier;
    }
    public float GetTowerAttackIntervalMultiplier()
    {
        return towerAttackIntervalMultiplier;
    }
    public float GetTowerDamageMultiplier()
    {
        return towerDamageMultiplier;
    }
    #endregion
    #region 敌方加成设置
    [SerializeField, Header("敌方加成设置"),Range(2, 5)] private int maxEnemy = 2;
    public int MaxEnemy => maxEnemy; // 添加访问器以供将来使用
    [SerializeField, PropertyRange(0, "maxEnemy")]
    private float enemyHealthMultiplier = 1f;
    
    public float GetEnemyHealthMultiplier() => enemyHealthMultiplier;
    #endregion
    #region 金钱设置
    [Header("基础金钱设置")]
    [SerializeField, Tooltip("游戏开始时的初始金币")] 
    private int startingMoney = 100;

    [SerializeField, Tooltip("刷新物品和道具所需的金币")] 
    private int itemRefreshMoney = 2;

    [SerializeField, Tooltip("拆除Block时返还的金币")] 
    private int blockDestroyMoney = 7;

    [SerializeField, Tooltip("建造Block所需的金币")] 
    private int blockBuildMoney = 2;
    
    [Header("刷新系统")]
    [SerializeField, Tooltip("基础刷新费用")] 
    private int baseRefreshCost = 3;    

    [SerializeField, Tooltip("最大刷新费用")] 
    private int maxRefreshCost = 6;     

    [SerializeField, Tooltip("每次刷新费用增加值")] 
    private int refreshCostIncrement = 1; 

    [SerializeField, Tooltip("每轮最大刷新次数")] 
    private int maxRefreshPerRound = 3;   
    
    [Header("道具系统")]
    [SerializeField, Tooltip("道具基础价格")] 
    private int itemBaseCost = 10;      
    
    [Header("Block系统")]
    [SerializeField, Tooltip("Block基础价格")] 
    private int blockBaseCost = 5;      

    [SerializeField, Tooltip("最大Block槽位数")] 
    private int maxBlockSlots = 5;
    public int StartingMoney
    {
        get
        {
            return startingMoney;
        }
    }
    public int ItemRefreshMoney
    {
        get
        {
            return itemRefreshMoney;
        }
    }

    public int BlockDestroyMoney
    {
        get
        {
            return blockDestroyMoney;
        }
    }
    
    public int BlockBuildMoney
    {
        get
        {
            return blockBuildMoney;
        }
    }

    // 新增的属性访问器
    public int BaseRefreshCost => baseRefreshCost;
    public int MaxRefreshCost => maxRefreshCost;
    public int RefreshCostIncrement => refreshCostIncrement;
    public int MaxRefreshPerRound => maxRefreshPerRound;
    public int ItemBaseCost => itemBaseCost;
    public int BlockBaseCost => blockBaseCost;
    public int MaxBlockSlots => maxBlockSlots;

    /// <summary>
    /// 计算Block价格（每个塔位5金币）
    /// </summary>
    public int CalculateBlockCost(int towerSlots)
    {
        // 每个塔位5金币
        return towerSlots * 5;
    }

    /// <summary>
    /// 计算当前刷新费用
    /// </summary>
    public int CalculateRefreshCost(int refreshCount)
    {
        int cost = BaseRefreshCost + (refreshCount * RefreshCostIncrement);
        return Mathf.Min(cost, MaxRefreshCost);
    }
    #endregion
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