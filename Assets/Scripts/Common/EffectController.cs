using UnityEngine;
using System.Collections.Generic;

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
    
    // 治疗效果相关字段
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
        }
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
        }
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
    /// 检查是否在战斗阶段
    /// </summary>
    private bool IsInBattlePhase()
    {
        if (GameManager.Instance != null)
        {
            var currentPhase = GameManager.Instance.GetCurrentGamePhase();
            return currentPhase == GamePhase.CombatPhase;
        }
        return false;
    }
    
    /// <summary>
    /// 更新治疗效果
    /// </summary>
    private void UpdateHealEffect(GameObject target, float deltaTime)
    {
        if (!isHealEffectActive || Data.effectName != "Heal") return;
        
        // 检查是否在战斗阶段
        if (!IsInBattlePhase())
        {
            return;
        }
        
        healCooldownTimer += deltaTime;
        
        // 治疗冷却完成，执行治疗
        if (healCooldownTimer >= Data.healInterval)
        {
            healCooldownTimer = 0f;
            
            try
            {
                var healTargets = FindHealTargetsSimple(target);
                
                if (healTargets.Count > 0)
                {
                    foreach (var healTarget in healTargets)
                    {
                        if (healTarget != null && healTarget.TryGetComponent<DamageTaker>(out var damageTaker))
                        {
                            float oldHealth = damageTaker.currentHealth;
                            damageTaker.Heal(Data.healAmount);
                            float newHealth = damageTaker.currentHealth;
                            Debug.Log($"[Heal Debug] 治疗 {healTarget.name}: {oldHealth:F1} -> {newHealth:F1} (+{newHealth - oldHealth:F1})");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[Heal Debug] {target.name} 没有找到任何治疗目标！");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Heal Debug] {target.name} 治疗过程中发生异常: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 使用格子范围系统查找治疗目标 - 严格按照上下左右四格范围
    /// </summary>
    private List<GameObject> FindHealTargetsSimple(GameObject centerTarget)
    {
        var healTargets = new List<GameObject>();
        
        if (centerTarget == null) return healTargets;
        
        // 获取GameMap引用
        var gameMap = GameManager.Instance?.GetSystem<GameMap>();
        if (gameMap == null)
        {
            Debug.LogWarning("[Heal Debug] 无法获取GameMap");
            return healTargets;
        }
        
        // 将治疗塔的世界坐标转换为格子坐标
        Vector3Int towerCellPos = gameMap.WorldToCellPosition(centerTarget.transform.position);
        Debug.Log($"[Heal Debug] 治疗塔 {centerTarget.name} 格子坐标: {towerCellPos}");
        
        // 获取治疗范围类型
        var tower = centerTarget.GetComponent<Tower>();
        if (tower == null || tower.TowerData == null) 
        {
            Debug.LogWarning($"[Heal Debug] 治疗塔 {centerTarget.name} 没有Tower组件或TowerData");
            return healTargets;
        }
        
        HealRangeType rangeType = tower.TowerData.GetHealRangeType(tower.Level);
        
        // 获取需要检查的格子坐标（严格按照Adjacent4范围）
        var targetCells = HealRangeCalculator.GetHealTargetCells(towerCellPos, rangeType);
        
        // 🔧 添加治疗塔自己的格子到检查列表
        if (!targetCells.Contains(towerCellPos))
        {
            targetCells.Add(towerCellPos);
            Debug.Log($"[Heal Debug] 添加治疗塔自身格子: {towerCellPos}");
        }
        
        Debug.Log($"[Heal Debug] 治疗范围类型: {rangeType}, 需要检查的格子数量: {targetCells.Count} (包含自身)");
        
        // 🔧 直接使用直接检测逻辑，不依赖GameMap.IsCellOccupied()
        var placedBlocks = gameMap.GetAllPlacedBlocks();
        Debug.Log($"[Heal Debug] 总共有 {placedBlocks.Count} 个已放置的Block");
        
        // 检查每个格子中的塔
        foreach (var cellPos in targetCells)
        {
            Debug.Log($"[Heal Debug] ===== 开始检查格子: {cellPos} =====");
            
            bool cellIsOccupied = false;
            GameObject towerInThisCell = null;
            
            // 直接检查所有Block，找到覆盖目标格子的Block
            foreach (var block in placedBlocks.Values)
            {
                if (block != null && block.Config != null)
                {
                    var blockCoords = block.Config.GetCellCoords();
                    if (blockCoords != null)
                    {
                        Vector3Int blockCellPos = block.CellPosition;
                        
                        // 检查这个Block是否覆盖目标格子
                        foreach (Vector2Int coord in blockCoords)
                        {
                            Vector3Int coveredCell = blockCellPos + new Vector3Int(coord.x, coord.y, 0);
                            if (coveredCell == cellPos)
                            {
                                Debug.Log($"[Heal Debug] ✓ 发现Block {block.name} 覆盖目标格子 {cellPos}");
                                Debug.Log($"[Heal Debug] Block基础位置: {blockCellPos}");
                                cellIsOccupied = true;
                                
                                // 在这个Block中查找塔
                                var towerInBlock = block.GetTower(new Vector3Int(coord.x, coord.y, 0));
                                if (towerInBlock != null && IsValidHealTarget(towerInBlock.gameObject))
                                {
                                    Debug.Log($"[Heal Debug] 格子 {cellPos} 中找到塔: {towerInBlock.name}");
                                    towerInThisCell = towerInBlock.gameObject;
                                }
                                break;
                            }
                        }
                        if (cellIsOccupied) break;
                    }
                }
            }
            
            // 如果找到有效的治疗目标，添加到列表
            if (towerInThisCell != null)
            {
                Debug.Log($"[Heal Debug] 添加有效治疗目标: {towerInThisCell.name}");
                healTargets.Add(towerInThisCell);
            }
            else
            {
                Debug.Log($"[Heal Debug] 格子 {cellPos} 中没有找到有效的治疗目标");
            }
            
            Debug.Log($"[Heal Debug] ===== 完成检查格子: {cellPos} =====");
        }
        
        Debug.Log($"[Heal Debug] 最终找到 {healTargets.Count} 个治疗目标");
        return healTargets;
    }
    
    /// <summary>
    /// 检查是否为有效的治疗目标
    /// </summary>
    private bool IsValidHealTarget(GameObject target)
    {
        if (target == null) return false;
        
        // 检查是否为友方单位（塔或中心塔）
        bool isValid = target.CompareTag("Tower") || target.CompareTag("CenterTower");
        
        // 额外检查：确保目标有DamageTaker组件
        if (isValid)
        {
            var damageTaker = target.GetComponent<DamageTaker>();
            if (damageTaker == null)
            {
                isValid = false;
            }
        }
        
        return isValid;
    }
} 