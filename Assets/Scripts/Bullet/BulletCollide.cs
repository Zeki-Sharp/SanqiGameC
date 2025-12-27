using System;
using System.Collections.Generic;
using RaycastPro.Bullets2D;
using Sirenix.OdinInspector;
using UnityEngine;

public class BulletCollide : MonoBehaviour
{
    [Header("基础配置")]
    [SerializeField] protected BulletConfig bulletConfig;
    [SerializeField] protected GameObject owner;
    [SerializeField] protected string[] targetTags;
    [SerializeField] protected Bullet2D bullet;
    
    [ShowInInspector] private HashSet<string> validTargetTags = new HashSet<string>();

    public void Initial( BulletConfig _bulletConfig,GameObject _owner)
    {
        bulletConfig = _bulletConfig;
        owner = _owner;
    }
    private void Awake()
    {
        bullet = GetComponent<Bullet2D>();
        if (bullet == null)
            Debug.LogError($"Bullet2D component missing on {name}");
            
        // 初始化目标标签集合
        if (targetTags != null && targetTags.Length > 0)
        {
            foreach (var tag in targetTags)
            {
                validTargetTags.Add(tag);
            }
        }
    }
    /// <summary>
    /// 处理碰撞
    /// </summary>
    protected virtual void HandleCollision(GameObject hitObject)
    {
        if (hitObject == owner) return;
        
        // 检查目标标签
        if (targetTags == null || targetTags.Length == 0 || !validTargetTags.Contains(hitObject.tag))
            return;
        
        // 根据目标类型处理
        switch (bulletConfig?.TargetType ?? TargetType.Single)
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
        if (this.gameObject.activeInHierarchy)
        {
            Destroy(this.gameObject);
        }
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
            taker.TakeDamage(bullet.damage);
        }
        
        // 2. 播放击中特效
        PlayHitEffect(target.transform.position);
        
        // 3. 再分发所有效果
        var effectControllers = GetComponents<IBulletEffectDispatcher>();
        foreach (var dispatcher in effectControllers)
        {
            dispatcher.DispatchEffect(target, owner);
        }
    }
    
    /// <summary>
    /// 播放击中特效
    /// </summary>
    private void PlayHitEffect(Vector3 position)
    {
        if (bulletConfig?.HitEffectPrefab != null)
        {
            Vector3 effectPosition = position + bulletConfig.HitEffectOffset;
            GameObject effect = Instantiate(bulletConfig.HitEffectPrefab, effectPosition, Quaternion.identity);
            
            Debug.Log($"[BulletCollide] 播放击中特效: {bulletConfig.HitEffectPrefab.name} 在位置: {effectPosition}");
        }
    }
    
    /// <summary>
    /// 处理范围目标
    /// </summary>
    protected virtual void ProcessAoeTarget(GameObject centerTarget)
    {
        float radius = 1.5f;
        if (bulletConfig != null && bulletConfig.TargetType == TargetType.Aoe)
            radius = bulletConfig.AoeRadius;
        
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
}
