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
            // 面向目标
            Vector3 direction = (targetTower.transform.position - controller.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                controller.transform.right = direction;
            }
            
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
        // 检查攻击范围内是否还有塔
        if (!controller.IsTowerInAttackRange())
        {
            controller.ChangeState(new EnemyMoveState(controller));
        }
    }
    
    private void FindTargetTower()
    {
        string[] tags = { "CenterTower", "Tower" };
        foreach (string tag in tags)
        {
            GameObject[] towers = GameObject.FindGameObjectsWithTag(tag);
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
            }
        }
    }
    
    
    /// <summary>
    /// 执行攻击 - 使用配置的攻击行为
    /// </summary>
    protected virtual void PerformAttack()
    {
        if (controller.AttackBehavior != null)
        {
            controller.AttackBehavior.PerformAttack(controller, targetTower);
        }
        else
        {
            Debug.LogWarning($"{controller.name} 没有配置攻击行为！");
        }
    }
}   