using UnityEngine;

/// <summary>
/// 敌人移动状态
/// </summary>
public class EnemyMoveState : EnemyState
{
    private GameObject targetTower;
    private float moveSpeed = 2f;
    
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
            FindTargetTower();
            return;
        }
        
        // 朝向目标塔移动
        Vector3 direction = (targetTower.transform.position - controller.transform.position).normalized;
        controller.transform.position += direction * moveSpeed * Time.deltaTime;
        
        // 更新朝向
        if (direction != Vector3.zero)
        {
            controller.transform.right = direction;
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
        string[] tags = { "Tower" };
        foreach (string tag in tags)
        {
            GameObject[] towers = GameObject.FindGameObjectsWithTag(tag);
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
            if (parent.name.ToLower().Contains("PrefabShowArea"))
            {
                return true;
            }
            parent = parent.parent;
        }
        
        return false;
    }
} 