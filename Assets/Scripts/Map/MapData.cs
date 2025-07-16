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
    [SerializeField, PropertyRange(0, "maxEnemy")]
    private float enemyHealthMultiplier = 1f;
    [SerializeField, PropertyRange(0, "maxEnemy")]
    private float enemyAttackMultiplier = 1f;
    #endregion
    #region 金钱设置
    [Header("金钱设置")]
    [SerializeField] private int startingMoney;
    //物品刷新钱数
    [SerializeField]
    private int itemRefreshMoney;
    //Block摧毁钱数
    [SerializeField]
    private int blockDestroyMoney;
    //Block建造钱数
    [SerializeField]
    private int blockBuildMoney;
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