using UnityEngine;

public class EnemyMeleeAttack : EnemyState
{
    private GameObject target;
    private float attackRange;
    private float attackCooldown;
    private float lastAttackTime;
    private float attackDamage;

    public EnemyMeleeAttack(EnemyController controller) : base(controller)
    {
        attackRange = controller.AttackRange;
        attackCooldown = controller.data.AttackCooldown;
        attackDamage = controller.data.AttackDamage;
    }

    public override void Enter()
    {
        FindTarget();
        lastAttackTime = -attackCooldown;
    }

    public override void Update()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }

        float distance = Vector3.Distance(controller.transform.position, target.transform.position);
        if (distance > attackRange)
        {
            // ׷��Ŀ��
            Vector3 dir = (target.transform.position - controller.transform.position).normalized;
            controller.transform.position += dir * controller.MoveSpeed * Time.deltaTime;
        }
        else
        {
            // ����
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                var taker = target.GetComponent<DamageTaker>();
                if (taker != null)
                    taker.TakeDamage(attackDamage);
                lastAttackTime = Time.time;
            }
        }
    }

    public override void CheckTransitions()
    {
        if (!controller.IsTowerInAttackRange())
        {
            controller.ChangeState(new EnemyMoveState(controller));
        }
    }

    private void FindTarget()
    {
        // 寻找塔相关标签
        string[] tags = { "CenterTower", "Tower" };
        float minDist = float.MaxValue;
        GameObject closest = null;
        foreach (string tag in tags)
        {
            foreach (var obj in GameObject.FindGameObjectsWithTag(tag))
            {
                float dist = Vector3.Distance(controller.transform.position, obj.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = obj;
                }
            }
        }
        target = closest;
    }
}