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
            Debug.Log($"{controller.name} 攻击状态：目标塔为空，重新查找");
            FindTargetTower();
            return;
        }
        
        // 检查目标是否在攻击范围内
        float distanceToTarget = Vector3.Distance(controller.transform.position, targetTower.transform.position);
        float attackRange = controller.AttackRange;
        
        Debug.Log($"{controller.name} 攻击状态：目标={targetTower.name}，距离={distanceToTarget:F2}，攻击范围={attackRange}");
        
        if (distanceToTarget <= attackRange)
        {
            // 执行攻击
            float attackCooldown = controller.AttackBehavior != null ? controller.AttackBehavior.GetAttackCooldown() : 1f;
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Debug.Log($"{controller.name} 执行攻击，目标: {targetTower.name}，距离: {distanceToTarget:F2}");
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }
        else
        {
            Debug.Log($"{controller.name} 攻击状态：不在攻击范围内，距离={distanceToTarget:F2} > {attackRange}");
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
        // 寻找攻击范围内最近的塔（包括中心塔和普通塔）
        GameObject closestTower = null;
        float closestDistance = float.MaxValue;
        
        // 检查中心塔
        GameObject centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower != null && !IsShowAreaTower(centerTower))
        {
            float distance = Vector3.Distance(controller.transform.position, centerTower.transform.position);
            if (distance <= controller.AttackRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTower = centerTower;
                Debug.Log($"{controller.name} 中心塔在攻击范围内，距离: {distance:F2}");
            }
        }
        
        // 检查普通塔
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        Debug.Log($"{controller.name} 找到 {towers.Length} 个普通塔");
        
        foreach (GameObject tower in towers)
        {
            // 过滤掉ShowArea塔
            if (IsShowAreaTower(tower))
            {
                Debug.Log($"{controller.name} 跳过ShowArea塔: {tower.name}");
                continue;
            }
                
            float distance = Vector3.Distance(controller.transform.position, tower.transform.position);
            Debug.Log($"{controller.name} 检查普通塔 {tower.name}，距离: {distance:F2}，攻击范围: {controller.AttackRange}");
            
            // 只考虑攻击范围内的塔
            if (distance <= controller.AttackRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTower = tower;
                Debug.Log($"{controller.name} 找到更近的目标塔: {tower.name}，距离: {distance:F2}");
            }
        }
        
        if (closestTower != null)
        {
            targetTower = closestTower;
            Debug.Log($"{controller.name} 最终选择目标塔: {closestTower.name}，距离: {closestDistance:F2}");
        }
        else
        {
            Debug.Log($"{controller.name} 没有找到攻击范围内的塔");
        }
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