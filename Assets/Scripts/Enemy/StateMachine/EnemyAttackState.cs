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
        Debug.Log($"敌人 {controller.name} 进入攻击状态，冷却时间: {attackCoolDown:F2}s");

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

        // 检查目标是否仍然有效（存活且在攻击范围内）
        bool targetValid = IsTargetValid(currentTarget);
        if (!targetValid)
        {
            var oldName = currentTarget ? currentTarget.name : "null";
            currentTarget = controller.AcquireBestTargetInRange(preferCenter: true);

            Debug.Log($"[Attack] target lost/dead/out_of_range: {oldName} -> reacquire: {currentTarget?.name ?? "null"}");

            if (currentTarget == null)
            {
                controller.ChangeState(new EnemyMoveState(controller));
                return;
            }
        }

        if (IsAttackCooldownReady())
        {
            Debug.Log($"[Attack Debug] {controller.name} 攻击冷却就绪，执行攻击");
            ExecuteAttack(currentTarget);
            ResetAttackCooldown();
        }
        else
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            Debug.Log($"[Attack Debug] {controller.name} 攻击冷却中，距离上次攻击: {timeSinceLastAttack:F2}s，需要等待: {attackCoolDown:F2}s");
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

    /// <summary>
    /// 检查目标是否有效（存活且在攻击范围内）
    /// </summary>
    private bool IsTargetValid(GameObject target)
    {
        // 首先检查目标是否存活和有效
        if (!IsAliveAndValid(target)) return false;
        
        // 然后检查是否在攻击范围内
        float distance = Vector3.Distance(controller.transform.position, target.transform.position);
        bool inRange = distance <= controller.AttackRange;
        
        if (!inRange)
        {
            Debug.Log($"[Attack] {controller.name} 目标 {target.name} 超出攻击范围，距离: {distance:F2}, 攻击范围: {controller.AttackRange:F2}");
        }
        
        return inRange;
    }

    private void ExecuteAttack(GameObject target)
    {
        if (target == null) return;

        // 同步敌人朝向和精灵翻转状态
        Vector2 dir = (target.transform.position - controller.transform.position).normalized;
        controller.SetDirection(dir);

        // 根据攻击行为类型分别处理
        if (controller.AttackBehavior is MeleeAttackBehavior melee)
        {
            // 近战攻击：直接调用攻击行为，不发射子弹
            ExecuteMeleeAttack(target, melee);
        }
        else if (controller.AttackBehavior is RangedAttackBehavior ranged)
        {
            // 远程攻击：发射子弹
            ExecuteRangedAttack(target, ranged, dir);
        }
        else
        {
            // 默认攻击行为：使用原来的逻辑
            ExecuteDefaultAttack(target, dir);
        }

        AudioManager.Instance.PlayAttackSound();
    }

    private void ExecuteMeleeAttack(GameObject target, MeleeAttackBehavior melee)
    {
        // 近战攻击：直接调用攻击行为
        melee.PerformAttack(controller, target);
        Debug.Log($"[Melee Attack] {controller.name} 对 {target.name} 进行近战攻击");
    }

    private void ExecuteRangedAttack(GameObject target, RangedAttackBehavior ranged, Vector2 dir)
    {
        if (bulletCaster == null)
        {
            Debug.LogWarning("[Attack] skip ranged attack: caster is null");
            return;
        }
        if (bulletCaster.bullets == null || bulletCaster.bullets.Length == 0)
        {
            Debug.LogWarning("[Attack] caster has NO bullets array");
            return;
        }

        // 设置raySensor的方向，这样子弹就能正确发射
        if (raySensor != null)
        {
            raySensor.direction = dir;
        }

        SetBulletDamage();

        bulletCaster.Cast(0);

        Debug.Log($"[Ranged Attack] CAST -> {target.name}, casterPos={bulletCaster.transform.position}, dir={dir}");
#if UNITY_EDITOR
        Debug.DrawLine(bulletCaster.transform.position,
                       bulletCaster.transform.position + (Vector3)dir * 1.5f, Color.yellow, 0.2f);
#endif
    }

    private void ExecuteDefaultAttack(GameObject target, Vector2 dir)
    {
        if (bulletCaster == null)
        {
            Debug.LogWarning("[Attack] skip default attack: caster is null");
            return;
        }
        if (bulletCaster.bullets == null || bulletCaster.bullets.Length == 0)
        {
            Debug.LogWarning("[Attack] caster has NO bullets array");
            return;
        }

        // 设置raySensor的方向，这样子弹就能正确发射
        if (raySensor != null)
        {
            raySensor.direction = dir;
        }

        SetBulletDamage();

        bulletCaster.Cast(0);

        Debug.Log($"[Default Attack] CAST -> {target.name}, casterPos={bulletCaster.transform.position}, dir={dir}");
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

    private void ResetAttackCooldown()
    {
        lastAttackTime = Time.time;
        Debug.Log($"敌人 {controller.name} 重置攻击冷却时间: {lastAttackTime:F2}");
    }

    private bool IsAttackCooldownReady()
    {
        if (attackCoolDown <= 0f)
        {
            attackCoolDown = controller.AttackBehavior != null ? controller.AttackBehavior.GetAttackCooldown() : 1f;
            if (attackCoolDown <= 0f) attackCoolDown = 1f;
        }
        
        float timeSinceLastAttack = Time.time - lastAttackTime;
        bool isReady = timeSinceLastAttack >= attackCoolDown;
        
        if (isReady)
        {
            Debug.Log($"敌人 {controller.name} 攻击冷却就绪 - 间隔: {timeSinceLastAttack:F2}s，冷却时间: {attackCoolDown:F2}s");
        }
        
        return isReady;
    }
}
