using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 直线子弹 - 继承自BulletBase，实现直线移动
/// </summary>
public class StraightBullet : BulletBase
{
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
} 