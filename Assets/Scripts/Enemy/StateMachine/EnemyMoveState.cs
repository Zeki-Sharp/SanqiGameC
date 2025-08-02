using UnityEngine;

/// <summary>
/// 敌人移动状态
/// </summary>
public class EnemyMoveState : EnemyState
{
    private GameObject targetTower;
    
    public EnemyMoveState(EnemyController controller) : base(controller) { }
    
    public override void Enter()
    {
        Debug.Log($"{controller.name} 进入移动状态");
        FindTargetTower();
    }
    
    public override void Update()
    {
        if (targetTower == null)
        {
            Debug.LogWarning($"{controller.name} 目标塔为null，尝试重新查找");
            FindTargetTower();
            // 如果仍然找不到目标塔，则提前返回
            if (targetTower == null)
            {
                Debug.LogError($"{controller.name} 无法找到任何目标塔");
                return;
            }
        }
        
        // 朝向目标塔移动
        Vector3 direction = (targetTower.transform.position - controller.transform.position).normalized;
        float speed = controller.MoveSpeed;
        Debug.Log($"{controller.name} 移动方向: {direction}, 速度: {speed}");
        controller.transform.position = Vector3.MoveTowards(controller.transform.position, targetTower.transform.position, speed * Time.deltaTime);
        
        // 确保Z轴位置正确
        Vector3 position = controller.transform.position;
        if (position.z != 0f)
        {
            position.z = 0f;
            controller.transform.position = position;
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
        
        // 检查攻击范围内是否有塔
        if (controller.IsTowerInAttackRange())
        {
            Debug.Log($"{controller.name} 检测到攻击范围内的塔，切换到攻击状态");
            controller.ChangeState(new EnemyAttackState(controller));
        }
    }
    
    private void FindTargetTower()
    {
        // 优先寻找中心塔
        GameObject centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower != null && !IsShowAreaTower(centerTower))
        {
            targetTower = centerTower;
            Debug.Log($"{controller.name} 找到中心塔作为目标: {targetTower.name}");
            return;
        }
        
        // 如果没有中心塔或中心塔是ShowArea，寻找最近的普通塔
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        if (towers.Length > 0)
        {
            // 找到最近的非ShowArea塔
            float closestDistance = float.MaxValue;
            GameObject closestTower = null;
            
            foreach (GameObject tower in towers)
            {
                // 过滤掉ShowArea塔
                if (IsShowAreaTower(tower))
                    continue;
                    
                float distance = Vector3.Distance(controller.transform.position, tower.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTower = tower;
                }
            }
            
            if (closestTower != null)
            {
                targetTower = closestTower;
                Debug.Log($"{controller.name} 找到目标塔: {targetTower.name}");
                return;
            }
        }
        
        Debug.LogWarning($"{controller.name} 没有找到可攻击的塔");
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
            // 使用OrdinalIgnoreCase比较避免ToLower()的性能开销
            if (parent.name.IndexOf("PrefabShowArea", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            parent = parent.parent;
        }
        
        return false;
    }
}
