using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics.Geometry;
using UnityEngine.Tilemaps;
using Math = System.Math;

/// <summary>
/// 子弹基类 - 统一管理子弹生命周期和对象池集成
/// </summary>
[RequireComponent(typeof(BulletLayerController))]
public abstract class BulletBase : MonoBehaviour, IBullet
{
    [Header("基础配置")]
    [SerializeField] protected BulletConfig bulletConfig;

    [Header("运行时状态")]
    [SerializeField] protected Vector3 direction;
    [SerializeField] protected float speed;
    [SerializeField] protected float spawnTime;
    [SerializeField] protected GameObject owner;
    [SerializeField] protected float damage;
    [SerializeField] protected string[] targetTags;
    [SerializeField] protected GameObject target;
    [SerializeField] protected Vector3 endPosition;
    [Header("子弹配置")]
    [SerializeField] protected float height; // 直线子弹高度偏移


    // [Header("Debug")]
    // [SerializeField] protected ParabolaDebugger parabolaDebugger;
    // 对象池相关
    protected bool isFromPool = false;
    protected string poolKey = "";
    protected Vector3 originalScale;

    // 公共属性
    public BulletConfig BulletConfig => bulletConfig;
    public bool IsFromPool => isFromPool;
    public string PoolKey => poolKey;
    public Vector3 OriginalScale => originalScale;

    // private void Awake()
    // {
    //     parabolaDebugger = GetComponent<ParabolaDebugger>();
    //     if (parabolaDebugger != null)
    //     {
    //         parabolaDebugger.tilemap = MapUtility.FindTilemapBySortingLayer("Ground");
    //     }
    // }
    /// <summary>
    /// 初始化子弹（IBullet接口实现）
    /// </summary>
    public virtual void Initialize(Vector3 direction, float speed, GameObject owner, GameObject target = null, string[] targetTags = null, float damage = 0f)
    {


        this.direction = direction.normalized;
        this.speed = speed > 0 ? speed : (bulletConfig != null ? bulletConfig.DefaultSpeed : 10f);
        this.owner = owner;
        this.target = target;
        this.damage = damage;
        this.spawnTime = Time.time;

        // parabolaDebugger.initialHeight = height;
        // Debug.Log("向量"+this.direction * this.speed);
        // parabolaDebugger.initialVelocity = this.direction * this.speed;
        // parabolaDebugger.origin = transform.position;
        
        // Tilemap tilemap = MapUtility.FindTilemapBySortingLayer("Ground");
        //
        // if (tilemap != null)
        // {
        //     Vector2 velocity = this.direction * this.speed;
        //     endPosition = CoordinateUtility.PredictParabolaImpact(this.transform.position, height, velocity, tilemap);
        // }
        // 设置目标标签
        if (targetTags != null && targetTags.Length > 0)
        {
            this.targetTags = targetTags;
        }
        else if (bulletConfig != null)
        {
            this.targetTags = bulletConfig.TargetTags;
        }
        else
        {
            this.targetTags = new string[]
            {
                "Enemy"
            };
        }

        // 设置朝向
        if (direction != Vector3.zero)
        {
            transform.right = direction;
        }

        // 确保缩放正确
        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
        }

        // 调用子类特定的初始化
        OnInitialize();
    }

    /// <summary>
    /// 子类特定的初始化逻辑
    /// </summary>
    protected virtual void OnInitialize()
    {
        // 子类可以重写此方法实现特定的初始化逻辑
    }

    /// <summary>
    /// 从对象池获取时的设置
    /// </summary>
    public virtual void SetFromPool(string poolKey)
    {
        this.isFromPool = true;
        this.poolKey = poolKey;

        // 保存原始缩放（只在第一次时保存）
        if (originalScale == Vector3.zero)
        {
            originalScale = transform.localScale;
        }
    }

    /// <summary>
    /// 返回对象池
    /// </summary>
    public virtual void ReturnToPool()
    {
        // 防止重复返回
        if (!gameObject.activeInHierarchy) return;
        
        Debug.Log($"子弹 {name} 返回对象池");
        
        if (isFromPool && !string.IsNullOrEmpty(poolKey))
        {
            var bulletManager = GameManager.Instance?.GetSystem<BulletManager>();
            if (bulletManager != null)
            {
                bulletManager.ReturnBullet(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 检查生命周期
    /// </summary>
    protected virtual void CheckLifetime()
    {
        float lifetime = bulletConfig != null ? bulletConfig.Lifetime : 5f;
        if (Time.time - spawnTime > lifetime)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 检查地面碰撞
    /// </summary>
    protected virtual void CheckGroundCollision()
    {

        // // 检查是否击中地面（Y坐标小于等于0）
        // if (transform.position.y <= 0f)
        // {
        //     ReturnToPool();
        // }

        // if (MathUtility.IsValueInRange(Mathf.Abs(transform.position.y - endPosition.y), 0, 0.5f))
        // {
        //     ReturnToPool();
        // }
        // OnCheckGroundCollision();
    }
    public abstract void OnCheckGroundCollision();
    /// <summary>
    /// 检查是否飞出屏幕
    /// </summary>
    protected virtual void CheckOutOfScreen()
    {
        // 获取主摄像机
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 将子弹位置转换为屏幕坐标
        Vector3 screenPos = mainCamera.WorldToViewportPoint(transform.position);

        // 检查是否飞出屏幕（添加一些边距）
        float margin = 0.1f;
        if (screenPos.x < -margin || screenPos.x > 1f + margin ||
            screenPos.y < -margin || screenPos.y > 1f + margin)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// 处理碰撞
    /// </summary>
    protected virtual void HandleCollision(GameObject hitObject)
    {
        if (hitObject == owner) return;

        // 检查目标标签
        bool isValidTarget = false;
        foreach (var tag in targetTags)
        {
            if (hitObject.CompareTag(tag))
            {
                isValidTarget = true;
                break;
            }
        }

        if (!isValidTarget) return;

        // 根据目标类型处理
        if (bulletConfig != null)
        {
            switch (bulletConfig.TargetType)
            {
                case TargetType.Single:
                    ProcessSingleTarget(hitObject);
                    break;
                case TargetType.Aoe:
                    ProcessAoeTarget(hitObject);
                    break;
                case TargetType.Chain:
                    ProcessChainTarget(hitObject);
                    break;
            }
        }
        else
        {
            ProcessSingleTarget(hitObject);
        }

        // 返回对象池
        ReturnToPool();
    }

    /// <summary>
    /// 处理单目标
    /// </summary>
    protected virtual void ProcessSingleTarget(GameObject target)
    {
        // 1. 先造成伤害
        var taker = target.GetComponent<DamageTaker>();
        if (taker != null)
        {
            taker.TakeDamage(damage);
        }

        // 2. 再分发所有效果
        var effectControllers = GetComponents<IBulletEffectDispatcher>();
        foreach (var dispatcher in effectControllers)
        {
            dispatcher.DispatchEffect(target, owner);
        }
    }

    /// <summary>
    /// 处理范围目标
    /// </summary>
    protected virtual void ProcessAoeTarget(GameObject centerTarget)
    {
        float radius = bulletConfig != null ? bulletConfig.AoeRadius : 1.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == owner) continue;

            bool isValidTarget = false;
            foreach (var tag in targetTags)
            {
                if (hit.CompareTag(tag))
                {
                    isValidTarget = true;
                    break;
                }
            }

            if (isValidTarget)
            {
                ProcessSingleTarget(hit.gameObject);
            }
        }
    }

    /// <summary>
    /// 处理链式目标（待实现）
    /// </summary>
    protected virtual void ProcessChainTarget(GameObject firstTarget)
    {
        // TODO: 实现链式伤害逻辑
        ProcessSingleTarget(firstTarget);
    }

    /// <summary>
    /// 碰撞检测（子类实现）
    /// </summary>
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // 防止重复处理碰撞
        if (!gameObject.activeInHierarchy) return;

        Debug.Log($"子弹 {name} 碰撞到 {other.name}");
        HandleCollision(other.gameObject);
    }

    /// <summary>
    /// 更新逻辑（子类实现）
    /// </summary>
    protected virtual void Update()
    {
        // 如果子弹已经返回对象池，不再更新
        if (!gameObject.activeInHierarchy) return;

        CheckLifetime();
        CheckGroundCollision();
        CheckOutOfScreen();
        OnUpdate();
    }

    /// <summary>
    /// 子类特定的更新逻辑
    /// </summary>
    protected abstract void OnUpdate();

    /// <summary>
    /// 重置状态（返回对象池时调用）
    /// </summary>
    public virtual void Reset()
    {
        direction = Vector3.zero;
        speed = 0f;
        spawnTime = 0f;
        owner = null;
        damage = 0f;
        target = null;
        targetTags = null;
        isFromPool = false;
        poolKey = "";
        endPosition = Vector3.zero;

        // 重置为原始缩放，而不是Vector3.one
        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
        }
        else
        {
            transform.localScale = Vector3.one;
        }

        // 调用子类特定的重置逻辑
        OnReset();
    }

    /// <summary>
    /// 子类特定的重置逻辑
    /// </summary>
    protected virtual void OnReset()
    {
        // 子类可以重写此方法实现特定的重置逻辑
    }
}