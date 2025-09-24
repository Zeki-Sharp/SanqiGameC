using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 抛物线子弹 - 继承自BulletBase，实现抛物线移动
/// </summary>
public class ParabolaBullet : BulletBase
{
    // [Header("抛物线子弹特定配置")]
    // [SerializeField] private float height = 2f; // 抛物线高度
    //
    // // 抛物线计算相关
    // private Vector3 start;
    // private Vector3 end;
    // private float t;
    // private float totalTime;
    
    // 碰撞检测 - 移除重复的碰撞检测，使用基类的实现
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (!gameObject.activeInHierarchy) return; // 防止重复处理
        HandleCollision(other.gameObject);
    }
    
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
        // // 设置抛物线起点和终点
        // start = transform.position;
        // end = target != null ? target.transform.position : (transform.position + direction * 5f);
        //
        // // 计算抛物线总时间
        // float distance = Vector3.Distance(start, end);
        // totalTime = distance / speed;
        // t = 0f;
    }
    
    /// <summary>
    /// 实现抽象方法OnUpdate
    /// </summary>
    protected override void OnUpdate()
    {
        // // 如果子弹已经返回对象池，不再更新
        // if (!gameObject.activeInHierarchy) return;
        //
        // // 更新抛物线参数
        // t += Time.deltaTime / totalTime;
        //
        // // 计算抛物线位置
        // Vector3 pos = Vector3.Lerp(start, end, t);
        // pos.y += height * 4 * (t - t * t);
        // transform.position = pos;
    }
    
    /// <summary>
    /// 重置子弹状态
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        // t = 0f;
        // totalTime = 0f;
    }
} 