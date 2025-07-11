using UnityEngine;

/// <summary>
/// 敌人攻击状态 - 抽象类，等待集成具体攻击逻辑
/// </summary>
public class EnemyAttackState : EnemyState
{
    private GameObject targetTower;
    private float attackRange = 1.5f;
    private float attackCooldown = 1f;
    private float lastAttackTime;
    
    public EnemyAttackState(EnemyController controller) : base(controller) { }
    
    public override void Enter()
    {
        Debug.Log($"{controller.name} 进入攻击状态");
        FindTargetTower();
        lastAttackTime = -attackCooldown; // 允许立即攻击
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
        
        if (distanceToTarget <= attackRange)
        {
            // 面向目标
            Vector3 direction = (targetTower.transform.position - controller.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                controller.transform.right = direction;
            }
            
            // 执行攻击
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
        }
    }
    
    /// <summary>
    /// 执行攻击 - 抽象方法，等待具体实现
    /// </summary>
    protected virtual void PerformAttack()
    {
        Debug.Log($"{controller.name} 攻击目标塔: {targetTower.name}");
        
        // TODO: 在这里集成具体的攻击逻辑
        // 例如：
        // - 播放攻击动画
        // - 造成伤害
        // - 播放音效
        // - 生成特效等
    }
} 