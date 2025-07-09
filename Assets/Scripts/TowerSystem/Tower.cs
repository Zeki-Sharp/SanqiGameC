using System;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("塔数据")]
    [SerializeField] private TowerData towerData;
    
    [Header("状态")]
    [SerializeField] private float currentHealth;
    [SerializeField] private Vector2Int position;
    [SerializeField] private SpriteRenderer spriteRenderer;
     
    // 公共属性
    public TowerData TowerData => towerData;
    public float CurrentHealth => currentHealth;
    public Vector2Int Position => position;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// 初始化塔
    /// </summary>
    /// <param name="data">塔的数据</param>
    /// <param name="pos">塔的位置</param>
    public void Initialize(TowerData data, Vector2Int pos)
    {
        towerData = data;
        position = pos;
        currentHealth = data.Health;
        spriteRenderer.sprite = data.TowerSprite;
        // 设置位置
        transform.position = new Vector3(pos.x, pos.y, 0);
        
        Debug.Log($"塔初始化完成: {data.TowerName} 在位置 ({pos.x}, {pos.y})");
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log($"塔 {towerData.TowerName} 被摧毁");
            // TODO: 播放摧毁效果
        }
        else
        {
            Debug.Log($"塔 {towerData.TowerName} 受到 {damage} 点伤害，剩余生命值: {currentHealth}");
        }
    }
    
    /// <summary>
    /// 攻击目标
    /// </summary>
    /// <param name="target">目标</param>
    public void Attack(GameObject target)
    {
        if (target == null) return;
        
        // TODO: 实现攻击逻辑
        Debug.Log($"塔 {towerData.TowerName} 攻击目标");
    }
    
    /// <summary>
    /// 检查是否存活
    /// </summary>
    /// <returns>是否存活</returns>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
} 