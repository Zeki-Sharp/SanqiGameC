using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

/// <summary>
/// 抛物线子弹 - 继承自BulletBase，实现抛物线移动
/// </summary>

public class ParabolaBullet : BulletBase
{
    [Header("抛物线子弹特定配置")]
    [SerializeField] private float height = 2f; // 抛物线高度
    
    // 抛物线计算相关
    private Vector3 start;
    private Vector3 end;
    private float t;
    private float totalTime;
    private float moveSpeed = 5f;
    private float totalDistance;
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
        // 设置抛物线起点和终点
        start = transform.position;
        end = target != null ? target.transform.position : (transform.position + direction * 5f);
        endPosition = end;
        totalDistance = Vector3.Distance(start, end);
        // 计算抛物线总时间
        float distance = Vector3.Distance(start, end);
        totalTime = distance / speed;
        t = 0f;
    }
    
    /// <summary>
    /// 实现抽象方法OnUpdate
    /// </summary>
    protected override void OnUpdate()
    {
        if (!gameObject.activeInHierarchy) return;

        // 当前沿线的进度 [0,1]
        float traveled = Vector3.Distance(start, transform.position);
        float progress = Mathf.Clamp01(traveled / totalDistance);

        // 计算当前目标点位置
        Vector3 nextPos = Vector3.MoveTowards(transform.position, end, moveSpeed * Time.deltaTime);

        // 计算抛物线 Y 偏移
        float arc = height * 4 * (progress - progress * progress);
        nextPos.y = Mathf.Lerp(start.y, end.y, progress) + arc;

        transform.position = nextPos;

        // // 检查是否到达目标点
        if (MathUtility.IsValueInRange(Mathf.Abs(Vector3.Distance(transform.position, endPosition)), -0.5f, 0.5f))
        {
            Debug.Log($"起点{transform.position} 结束 {endPosition}");
            OnCheckGroundCollision();
        }
    }
    public override void OnCheckGroundCollision()
    {
       // Tilemap tilemap =  MapUtility.FindTilemapBySortingLayer("Ground");
       //
       // if (tilemap != null)
       // {
       //     Vector2 velocity = direction * speed;
       //     CoordinateUtility.PredictParabolaImpact(start,velocity,tilemap)
       // }
       ReturnToPool();
    }
    
    /// <summary>
    /// 重置子弹状态
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        t = 0f;
        totalTime = 0f;
    }
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 确保起点终点有效
        if (!Application.isPlaying && target == null) return;

        Vector3 p0 = transform.position;
        Vector3 p1 = target != null ? target.transform.position : (transform.position + direction * 5f);

        int segments = 30;
        for (int i = 0; i < segments; i++)
        {
            float t1 = i / (float)segments;
            float t2 = (i + 1) / (float)segments;

            Vector3 a = Vector3.Lerp(p0, p1, t1);
            Vector3 b = Vector3.Lerp(p0, p1, t2);

            a.y += height * 4 * (t1 - t1 * t1);
            b.y += height * 4 * (t2 - t2 * t2);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(a, b);
        }
    }
#endif
} 