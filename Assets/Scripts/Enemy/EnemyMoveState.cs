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
        // 检查攻击范围内是否有塔
        if (controller.IsTowerInAttackRange())
        {
            controller.ChangeState(new EnemyAttackState(controller));
        }
    }
    
    private void FindTargetTower()
    {
        GameObject[] towers = GameObject.FindGameObjectsWithTag("centerTower");
        
        if (towers.Length > 0)
        {
            // 找到最近的塔
            float closestDistance = float.MaxValue;
            GameObject closestTower = null;
            
            foreach (GameObject tower in towers)
            {
                float distance = Vector3.Distance(controller.transform.position, tower.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTower = tower;
                }
            }
            
            targetTower = closestTower;
            Debug.Log($"{controller.name} 找到目标塔: {targetTower.name}");
        }
        else
        {
            Debug.LogWarning("场景中没有找到centerTower标签的物体");
        }
    }
} 