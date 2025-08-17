using RaycastPro.Casters2D;
using RaycastPro.Detectors2D;
using RaycastPro.RaySensors2D;
using UnityEngine;

public class EnemyAttackState : EnemyState
{
    private float lastAttackTime;
    private float attackCoolDown; // seconds per attack

    private RangeDetector2D rangeDetector;
    public  BasicRay2D      raySensor;
    private BasicCaster2D   bulletCaster;

    private GameObject currentTarget;
    private float prevMoveSpeed = -1f; // freeze move during attack

    public EnemyAttackState(EnemyController controller) : base(controller) { }

    public override void Enter(RangeDetector2D rangeDetector, BasicRay2D raySensor, BasicCaster2D bulletCaster)
    {
        this.rangeDetector = rangeDetector;
        this.raySensor     = raySensor;
        this.bulletCaster  = bulletCaster;

        // 不再 AddListener(onDetect)，避免外部事件影响锁定
        if (this.rangeDetector != null)
        {
            this.rangeDetector.Radius = controller.AttackRange;
            this.rangeDetector.Cast(); // 若是手动更新，先刷一次
        }

        Debug.Log($"[Attack] ENTER {controller.name}");

        // 首次锁定（优先中心塔）
        currentTarget = controller.AcquireBestTargetInRange(preferCenter: true);
        if (currentTarget == null)
        {
            Debug.LogWarning("[Attack] no target in range on enter -> back to Move");
            controller.ChangeState(new EnemyMoveState(controller));
            return;
        }

        attackCoolDown = controller.AttackBehavior != null ? controller.AttackBehavior.GetAttackCooldown() : 1f;
        if (attackCoolDown <= 0f) attackCoolDown = 1f;
        lastAttackTime = Time.time - attackCoolDown; // 进来立刻能打一发

        // 冻结移动
        prevMoveSpeed = controller.MoveSpeed;
        controller.MoveSpeed = 0f;
    }

    public override void Exit()
    {
        // 无监听可移除
        if (prevMoveSpeed >= 0f)
            controller.MoveSpeed = prevMoveSpeed;
    }

    public override void Update()
    {
        if (controller.IsCenterTowerDestroyed())
            return;

        // 若 detector 是手动更新，低频刷新一次集合（不用于切换，仅供死亡后再找新目标）
        if (rangeDetector != null && (Time.frameCount % 10 == 0))
            rangeDetector.Cast();

        // —— 只看存活/有效，不看距离 ——（硬锁定）
        bool targetAliveAndValid = IsAliveAndValid(currentTarget);
        if (!targetAliveAndValid)
        {
            var oldName = currentTarget ? currentTarget.name : "null";
            currentTarget = controller.AcquireBestTargetInRange(preferCenter: true);

            Debug.Log($"[Attack] target lost/dead: {oldName} -> reacquire: {currentTarget?.name ?? "null"}");

            if (currentTarget == null)
            {
                controller.ChangeState(new EnemyMoveState(controller));
                return;
            }
        }

        if (IsAttackCooldownReady())
        {
            ExecuteAttack(currentTarget);
            ResetAttackCooldown();
        }
    }

    public override void CheckTransitions()
    {
        if (controller.IsCenterTowerDestroyed())
        {
            controller.ChangeState(new EnemyDefeatState(controller));
            return;
        }
        // 其余不在这里切，避免抖动
    }

    private bool IsAliveAndValid(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return false;
        // 若塔没有 DamageTaker，则按“有效”处理；有的话要求 > 0
        if (go.TryGetComponent<DamageTaker>(out var dt) && dt.currentHealth <= 0) return false;
        // 其他非法（ShowArea 等）不在这里判，避免误把已锁目标踢掉
        return true;
    }

    private void ExecuteAttack(GameObject target)
    {
        if (target == null || bulletCaster == null)
        {
            Debug.LogWarning("[Attack] skip cast: target or caster is null");
            return;
        }
        if (bulletCaster.bullets == null || bulletCaster.bullets.Length == 0)
        {
            Debug.LogWarning("[Attack] caster has NO bullets array");
            return;
        }

        // 2D: BasicCaster2D 沿 +X(right) 发射，必须对齐
        Vector2 dir = (target.transform.position - controller.transform.position).normalized;
        bulletCaster.transform.right = dir;

        if (raySensor != null)
            raySensor.SetHitPosition(target.transform.position - controller.transform.position);

        SetBulletDamage();

        bulletCaster.Cast(0);

        Debug.Log($"[Attack] CAST -> {target.name}, casterPos={bulletCaster.transform.position}, dir={dir}");
#if UNITY_EDITOR
        Debug.DrawLine(bulletCaster.transform.position,
                       bulletCaster.transform.position + (Vector3)dir * 1.5f, Color.yellow, 0.2f);
#endif
    }

    private void SetBulletDamage()
    {
        if (bulletCaster == null || bulletCaster.bullets == null) return;

        for (int i = 0; i < bulletCaster.bullets.Length; i++)
        {
            var b = bulletCaster.bullets[i];
            if (b == null) continue;

            if (controller.AttackBehavior is MeleeAttackBehavior melee)
            {
                b.damage = melee.Damage;
            }
            else if (controller.AttackBehavior is RangedAttackBehavior ranged)
            {
                b.damage = ranged.Damage;
                var collide = b.GetComponent<BulletCollide>();
                if (collide != null)
                    collide.Initial(ranged.BulletConfig, controller.gameObject);
            }
        }
    }

    private void ResetAttackCooldown() => lastAttackTime = Time.time;

    private bool IsAttackCooldownReady()
    {
        if (attackCoolDown <= 0f)
        {
            attackCoolDown = controller.AttackBehavior != null ? controller.AttackBehavior.GetAttackCooldown() : 1f;
            if (attackCoolDown <= 0f) attackCoolDown = 1f;
        }
        return Time.time - lastAttackTime >= attackCoolDown;
    }
}
