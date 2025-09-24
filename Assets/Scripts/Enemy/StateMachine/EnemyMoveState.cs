using RaycastPro.Casters2D;
using RaycastPro.Detectors2D;
using RaycastPro.RaySensors2D;
using UnityEngine;

/// <summary>
/// 移动：最终目标指向中心塔；若路径上被塔阻挡则优先那座塔；
/// 靠近目标时减速并停在 AttackRange 边缘，避免冲刺/贴脸；
/// 进入攻距后由 AttackState 接管（不移动，硬锁定）。
/// </summary>
public class EnemyMoveState : EnemyState
{
    // 最终要去的点（优先中心塔）
    private GameObject finalGoal;
    // 当前实际移动目标（挡路塔或 finalGoal）
    private GameObject moveTarget;

    // 运行时配置（不依赖 Inspector）
    private LayerMask towerMask;          // 包含 Tower / CenterTower 的 Layer
    private float pathCheckRadius = 0.35f; // 路径走廊半径（敌人半宽 + 余量）
    private float recheckInterval = 0.10f; // 路径重检间隔（秒）
    private float nextRecheckTime;

    private const float stopBuffer = 0.25f; // 停在攻距边缘的提前量（调整为0.25f，适合小攻击范围）

    public EnemyMoveState(EnemyController controller) : base(controller) { }

    public override void Enter(RangeDetector2D rangeDetector = null, BasicRay2D raySensor = null, BasicCaster2D bulletCaster = null)
    {
        Debug.Log($"{controller.name} Enter MoveState");

        EnsureRuntimeConfig();
        AcquireFinalGoal();
        UpdateMoveTargetByPathBlocking();
        nextRecheckTime = Time.time + recheckInterval;
    }

    public override void Update()
    {
        if (finalGoal == null)
        {
            AcquireFinalGoal();
            if (finalGoal == null) return;
        }

        // 定期检查路径阻挡，必要时把 moveTarget 切到挡路塔
        if (Time.time >= nextRecheckTime)
        {
            UpdateMoveTargetByPathBlocking();
            nextRecheckTime = Time.time + recheckInterval;
        }

        if (moveTarget == null) moveTarget = finalGoal;

        // —— 靠近减速并停在攻距边缘（相对 moveTarget）——
        Vector3 from = controller.transform.position;
        Vector3 to   = moveTarget.transform.position;

        float step = controller.MoveSpeed * Time.deltaTime;
        float distanceToTarget = Vector3.Distance(from, to);
        
        // 简化逻辑：匀速运动，只在攻击范围边缘停止
        float distToStop = Mathf.Max(0f, distanceToTarget - controller.AttackRange);
        float moveThisFrame = Mathf.Min(step, distToStop);

        if (moveThisFrame > 0f)
        {
            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len > 1e-5f) dir /= len;
            controller.transform.position = from + dir * moveThisFrame;
            
            // 同步朝向和精灵翻转状态
            controller.SetDirection(dir);
        }

        // 2D：Z 轴归零
        var p = controller.transform.position;
        if (Mathf.Abs(p.z) > 0.0001f) { p.z = 0f; controller.transform.position = p; }
    }

    public override void CheckTransitions()
    {
        if (controller.IsCenterTowerDestroyed())
        {
            controller.ChangeState(new EnemyDefeatState(controller));
            return;
        }

        // 到达攻距边缘后，若探测到有可攻击塔，则切到攻击（由 AttackState 硬锁定）
        if (controller.AcquireBestTargetInRange(preferCenter: true) != null)
        {
            controller.ChangeState(new EnemyAttackState(controller));
        }
    }

    // ——— helpers ———

    private void EnsureRuntimeConfig()
    {
        // 塔层
        towerMask = LayerMask.GetMask("Tower", "CenterTower");
        if (towerMask == 0) Debug.LogWarning($"{controller.name}: towerMask=0，请确认 Layer 名为 'Tower' / 'CenterTower' 或自行修改这里的 GetMask。");

        // 估算走廊半径：从自身 Collider2D 计算（半宽 + 余量）
        var col = controller.GetComponentInChildren<Collider2D>();
        float scale = Mathf.Max(controller.transform.lossyScale.x, controller.transform.lossyScale.y);
        if (col is CircleCollider2D cc)
            pathCheckRadius = Mathf.Max(0.01f, cc.radius * scale + 0.15f);
        else if (col is CapsuleCollider2D cap)
            pathCheckRadius = Mathf.Max(0.01f, Mathf.Max(cap.size.x, cap.size.y) * 0.5f * scale + 0.15f);
        else if (col is BoxCollider2D box)
            pathCheckRadius = Mathf.Max(0.01f, Mathf.Max(box.size.x, box.size.y) * 0.5f * scale + 0.15f);
        else if (pathCheckRadius <= 0f)
            pathCheckRadius = 0.35f;

        if (recheckInterval <= 0f) recheckInterval = 0.10f;
    }

    // 设定最终目标：优先中心塔（存在且有效），否则最近的普通塔
    private void AcquireFinalGoal()
    {
        finalGoal = null;

        GameObject center = GameObject.FindGameObjectWithTag("CenterTower");
        if (IsValidTowerLocal(center))
        {
            finalGoal = center;
            return;
        }

        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        float best = float.MaxValue;
        GameObject bestGo = null;

        foreach (var t in towers)
        {
            if (!IsValidTowerLocal(t)) continue;
            float d = (t.transform.position - controller.transform.position).sqrMagnitude;
            if (d < best) { best = d; bestGo = t; }
        }
        finalGoal = bestGo;
    }

    // 沿“我→finalGoal”的走廊取所有命中：优先最近的非 finalGoal 塔，否则退而取 finalGoal
    private void UpdateMoveTargetByPathBlocking()
    {
        moveTarget = finalGoal;
        if (finalGoal == null) return;

        Vector2 origin = controller.transform.position;
        Vector2 delta  = finalGoal.transform.position - controller.transform.position;
        float dist = delta.magnitude;
        if (dist < 0.0001f) return;

        Vector2 dir = delta / dist;

        var selfCols = controller.GetComponentsInChildren<Collider2D>(true);
        var hits = Physics2D.CircleCastAll(origin, pathCheckRadius, dir, dist, towerMask);

        RaycastHit2D? best = null;
        foreach (var h in hits)
        {
            if (h.collider == null) continue;

            bool isSelf = false;
            foreach (var sc in selfCols) { if (h.collider == sc) { isSelf = true; break; } }
            if (isSelf) continue;

            var go = h.collider.gameObject;
            if (!IsValidTowerLocal(go)) continue;

            if (go == finalGoal)
            {
                if (!best.HasValue) best = h; // 先暂存 finalGoal
                continue;
            }

            if (!best.HasValue || h.distance < best.Value.distance)
                best = h;
        }

        if (best.HasValue)
            moveTarget = best.Value.collider.gameObject;
    }

    // 与 Controller 的规则一致（标签 / ShowArea / 存活）
    private bool IsValidTowerLocal(GameObject go)
    {
        if (go == null) return false;
        if (!(go.CompareTag("CenterTower") || go.CompareTag("Tower"))) return false;

        if (IsShowAreaLocal(go)) return false;

        var dt = go.GetComponent<DamageTaker>();
        if (dt != null && dt.currentHealth <= 0) return false;

        return true;
    }

    private bool IsShowAreaLocal(GameObject tower)
    {
        if (tower == null) return false;
        Transform p = tower.transform.parent;
        while (p != null)
        {
            if (p.name.IndexOf("ShowArea", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            p = p.parent;
        }
        return false;
    }
}
