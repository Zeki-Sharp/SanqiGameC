using RaycastPro.Casters2D;
using RaycastPro.Detectors2D;
using RaycastPro.RaySensors2D;
using UnityEngine;

/// <summary>
/// 敌人失败状态 - 当中心塔被摧毁时进入此状态
/// </summary>
public class EnemyDefeatState : EnemyState
{
    public EnemyDefeatState(EnemyController controller) : base(controller) { }

    public override void Enter(RangeDetector2D rangeDetector = null, BasicRay2D raySensor = null, BasicCaster2D bulletCaster = null)
    {
        Debug.Log($"{controller.name} 进入失败状态 - 中心塔已被摧毁");
        
        // 可以在这里添加失败动画、音效等
        // 例如：播放失败动画、停止移动音效等
    }
    
    public override void Update()
    {
        // 失败状态下敌人原地不动
        // 不执行任何移动或攻击逻辑
    }
    
    public override void CheckTransitions()
    {
        // 失败状态是终态，不会切换到其他状态
        // 除非有特殊需求（比如复活机制）
    }
    
    public override void Exit()
    {
        Debug.Log($"{controller.name} 退出失败状态");
    }
} 