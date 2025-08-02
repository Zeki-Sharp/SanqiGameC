using UnityEngine;

/// <summary>
/// 敌人攻击状态 - 使用配置的攻击行为
/// </summary>
public class EnemyAttackState : EnemyState
{
    private GameObject targetTower;
    private float lastAttackTime;
    
    public EnemyAttackState(EnemyController controller) : base(controller) { }
    
    public override void Enter()
    {
        Debug.Log($"{controller.name} 进入攻击状态");
        FindTargetTower();
        
        // 获取攻击冷却时间，允许立即攻击
        float attackCooldown = controller.AttackBehavior != null ? controller.AttackBehavior.GetAttackCooldown() : 1f;
        lastAttackTime = -attackCooldown;
    }
    
    public override void Update()
    {
        if (targetTower == null)
        {
            FindTargetTower();
            return;
        }
        
        // 检查目标是否在攻击范围内
        float distanceToTarget = Vector3.Distance(controller.transform.position, targetTower.transform.position);
        float attackRange = controller.AttackRange;
        
        if (distanceToTarget <= attackRange)
        {
            // 执行攻击
            float attackCooldown = controller.AttackBehavior != null ? controller.AttackBehavior.GetAttackCooldown() : 1f;
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }
    }
    
    public override void CheckTransitions()
    {
        // 首先检查中心塔是否被摧毁
        if (controller.IsCenterTowerDestroyed())
        {
            controller.ChangeState(new EnemyDefeatState(controller));
            return;
        }
        
        // 检查攻击范围内是否还有塔
        if (!controller.IsTowerInAttackRange())
        {
            controller.ChangeState(new EnemyMoveState(controller));
        }
    }
    
    private void FindTargetTower()
    {
        // 优先寻找中心塔
        GameObject centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower != null && !IsShowAreaTower(centerTower))
        {
            targetTower = centerTower;
            Debug.Log($"{controller.name} 找到中心塔目标: {centerTower.name}");
            return;
        }
        
        // 如果没有中心塔或中心塔是ShowArea，寻找攻击范围内最近的普通塔
        string[] tags = { "Tower" };
        foreach (string tag in tags)
        {
            GameObject[] towers = GameObject.FindGameObjectsWithTag(tag);
            Debug.Log($"{controller.name} 找到 {towers.Length} 个 {tag} 标签的塔");
            
            if (towers.Length > 0)
            {   
                // 找到攻击范围内最近的非ShowArea塔
                float closestDistance = float.MaxValue;
                GameObject closestTower = null;
                
                foreach (GameObject tower in towers)
                {
                    // 过滤掉ShowArea塔
                    if (IsShowAreaTower(tower))
                    {
                        Debug.Log($"{controller.name} 跳过ShowArea塔: {tower.name}");
                        continue;
                    }
                        
                    float distance = Vector3.Distance(controller.transform.position, tower.transform.position);
                    Debug.Log($"{controller.name} 检查塔 {tower.name}，距离: {distance:F2}，攻击范围: {controller.AttackRange}");
                    
                    // 只考虑攻击范围内的塔
                    if (distance <= controller.AttackRange && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTower = tower;
                    }
                }
                
                if (closestTower != null)
                {
                    targetTower = closestTower;
                    Debug.Log($"{controller.name} 找到目标塔: {closestTower.name}，距离: {closestDistance:F2}");
                    return;
                }
            }
        }
        
        Debug.Log($"{controller.name} 没有找到目标塔");
    }
    
    /// <summary>
    /// 检查是否为ShowArea塔
    /// </summary>
    private bool IsShowAreaTower(GameObject tower)
    {
        if (tower == null) return false;
        
        // 检查父物体名称是否包含"showarea"
        Transform parent = tower.transform.parent;
        while (parent != null)
        {
            if (parent.name.ToLower().Contains("showarea"))
            {
                return true;
            }
            parent = parent.parent;
        }
        
        return false;
    }
    
    
    /// <summary>
    /// 执行攻击 - 使用配置的攻击行为
    /// </summary>
    protected virtual void PerformAttack()
    {
        if (controller.AttackBehavior != null)
        {
            Debug.Log($"{controller.name} 执行攻击，目标: {targetTower?.name ?? "null"}");
            controller.AttackBehavior.PerformAttack(controller, targetTower);
        }
        else
        {
            Debug.LogWarning($"{controller.name} 没有配置攻击行为！");
        }
    }
}   