using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 直线子弹 - 继承自BulletBase，实现直线移动
/// </summary>
public class StraightBullet : BulletBase
{
    [Header("直线子弹特定配置")]
    [SerializeField] private float height = 0f; // 直线子弹高度偏移
    
    // 碰撞检测 - 移除重复的碰撞检测，使用基类的实现
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (!gameObject.activeInHierarchy) return; // 防止重复处理
    //     HandleCollision(other.gameObject);
    // }
    
    // private void OnTriggerEnter(Collider other)
    // {
    //     if (!gameObject.activeInHierarchy) return; // 防止重复处理
    //     HandleCollision(other.gameObject);
    // }
    
    /// <summary>
    /// 子类特定的初始化逻辑
    /// </summary>
    protected override void OnInitialize()
    {
        // 直线子弹不需要特殊初始化
    }
    
    /// <summary>
    /// 实现抽象方法OnUpdate
    /// </summary>
    protected override void OnUpdate()
    {
        // 如果子弹已经返回对象池，不再更新
        if (!gameObject.activeInHierarchy) return;
        
        // 直线移动
        transform.position += direction * speed * Time.deltaTime;
    }
    
    /// <summary>
    /// 重置子弹状态
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        // 直线子弹不需要额外的重置逻辑
    }
} 