using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class EffectController : MonoBehaviour
{
    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();

    public void AddEffect(EffectData effectData)
    {
        var effect = new ActiveEffect(effectData);
        activeEffects.Add(effect);
        effect.OnApply(this.gameObject);
    }

    public void RemoveEffect(string effectName)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].Data.effectName == effectName)
            {
                activeEffects[i].OnRemove(this.gameObject);
                activeEffects.RemoveAt(i);
            }
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].UpdateEffect(this.gameObject, deltaTime))
            {
                activeEffects[i].OnRemove(this.gameObject);
                activeEffects.RemoveAt(i);
            }
        }
    }
}

[System.Serializable]
public class EffectData
{
    public string effectName;
    public float modifier;
    public float duration;
    public GameObject effectFx;
    public float effectFxDuration;
    public AudioClip sfx;
    
    // 新增治疗效果相关字段
    [Header("治疗效果配置")]
    public float healAmount;
    public float healInterval;
    public HealRangeType healRangeType;
    public HealEffectType healEffectType;
}

public class ActiveEffect
{
    public EffectData Data { get; private set; }
    private float timer;
    private float originalSpeed;
    
    // 治疗效果相关字段
    private float healCooldownTimer;
    private bool isHealEffectActive;
    
    public ActiveEffect(EffectData data)
    {
        Data = data;
        timer = data.duration;
        healCooldownTimer = 0f;
        isHealEffectActive = false;
    }
    
    public void OnApply(GameObject target)
    {
        if (Data.effectName == "Speed")
        {
            var move = target.GetComponent<EnemyController>();
            if (move != null)
            {
                Debug.Log("Speed Effect Applied");
                originalSpeed = move.MoveSpeed;
                move.MoveSpeed += Data.modifier;
            }
        }
        else if (Data.effectName == "Heal")
        {
            // 激活治疗效果
            isHealEffectActive = true;
            healCooldownTimer = 0f;
            Debug.Log($"治疗效果激活：治疗量={Data.healAmount}，间隔={Data.healInterval}，范围={Data.healRangeType}");
        }
        // 可扩展：播放特效、音效等
    }
    
    public void OnRemove(GameObject target)
    {
        if (Data.effectName == "Speed")
        {
            var move = target.GetComponent<EnemyController>();
            if (move != null)
            {
                move.MoveSpeed = originalSpeed;
            }
        }
        else if (Data.effectName == "Heal")
        {
            // 停用治疗效果
            isHealEffectActive = false;
            Debug.Log("治疗效果已停用");
        }
        // 可扩展：移除特效、还原属性等
    }
    
    public bool UpdateEffect(GameObject target, float deltaTime)
    {
        // 修复：永久效果（duration <= 0）应该持续存在
        if (Data.duration > 0f)
        {
            timer -= deltaTime;
        }
        
        // 处理治疗效果
        if (isHealEffectActive && Data.effectName == "Heal")
        {
            UpdateHealEffect(target, deltaTime);
        }
        
        // 修复：只有非永久效果才在时间到期后移除
        return Data.duration > 0f && timer <= 0f;
    }
    
    /// <summary>
    /// 更新治疗效果
    /// </summary>
    private void UpdateHealEffect(GameObject target, float deltaTime)
    {
        if (!isHealEffectActive || Data.effectName != "Heal") return;
        
        healCooldownTimer += deltaTime;
        
        // 检查是否到达治疗间隔
        if (healCooldownTimer >= Data.healInterval)
        {
            healCooldownTimer = 0f;
            
            // 使用格子范围系统查找治疗目标
            var healTargets = FindHealTargetsByGrid(target);
            
            if (healTargets.Count > 0)
            {
                // 对每个目标进行治疗
                foreach (var healTarget in healTargets)
                {
                    if (healTarget != null && healTarget.TryGetComponent<DamageTaker>(out var damageTaker))
                    {
                        damageTaker.Heal(Data.healAmount);
                    }
                }
                
                Debug.Log($"范围治疗完成：治疗了 {healTargets.Count} 个目标，每个目标恢复生命值 {Data.healAmount}");
            }
        }
    }
    
    /// <summary>
    /// 使用格子范围系统查找治疗目标
    /// </summary>
    /// <returns>治疗目标列表</returns>
    private List<GameObject> FindHealTargetsByGrid(GameObject centerTarget)
    {
        var healTargets = new List<GameObject>();
        
        // 获取治疗塔的格子位置
        if (centerTarget == null) return healTargets;
        
        // 获取GameMap引用
        var gameMap = GameManager.Instance?.GetSystem<GameMap>();
        if (gameMap == null)
        {
            Debug.LogWarning("无法获取GameMap，使用物理检测作为备选方案");
            return FindHealTargetsByPhysics(centerTarget);
        }
        
        // 将治疗塔的世界坐标转换为格子坐标
        Vector3Int towerCellPos = gameMap.WorldToCellPosition(centerTarget.transform.position);
        
        // 获取治疗范围类型
        var tower = centerTarget.GetComponent<Tower>();
        if (tower == null || tower.TowerData == null) return healTargets;
        
        HealRangeType rangeType = tower.TowerData.GetHealRangeType(tower.Level);
        
        // 获取需要检查的格子坐标
        var targetCells = HealRangeCalculator.GetHealTargetCells(towerCellPos, rangeType);
        
        // 检查每个格子中的塔
        foreach (var cellPos in targetCells)
        {
            // 检查格子是否被占用
            if (gameMap.IsCellOccupied(cellPos))
            {
                // 获取该格子中的Block
                var placedBlocks = gameMap.GetAllPlacedBlocks();
                foreach (var block in placedBlocks.Values)
                {
                    if (block != null)
                    {
                        // 检查Block是否覆盖该格子
                        var blockCoords = block.Config.GetCellCoords();
                        Vector3Int blockCellPos = block.CellPosition;
                        
                        foreach (var coord in blockCoords)
                        {
                            Vector3Int absoluteCoord = blockCellPos + coord;
                            if (absoluteCoord == cellPos)
                            {
                                // 获取该格子中的塔
                                var towerInCell = block.GetTower(coord);
                                if (towerInCell != null && IsValidHealTarget(towerInCell.gameObject))
                                {
                                    healTargets.Add(towerInCell.gameObject);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        return healTargets;
    }
    
    /// <summary>
    /// 使用物理检测作为备选方案（保持向后兼容）
    /// </summary>
    /// <returns>治疗目标列表</returns>
    private List<GameObject> FindHealTargetsByPhysics(GameObject centerTarget)
    {
        var healTargets = new List<GameObject>();
        
        if (centerTarget == null) return healTargets;
        
        // 使用物理检测查找范围内的目标
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            centerTarget.transform.position,
            Vector2.one * 2f, // 使用固定范围作为备选
            0f
        );
        
        foreach (var collider in colliders)
        {
            if (IsValidHealTarget(collider.gameObject))
            {
                healTargets.Add(collider.gameObject);
            }
        }
        
        return healTargets;
    }
    
    /// <summary>
    /// 检查是否为有效的治疗目标
    /// </summary>
    private bool IsValidHealTarget(GameObject target)
    {
        // 检查是否为友方单位（塔或中心塔）
        return target.CompareTag("Tower") || target.CompareTag("CenterTower");
    }
} 