using UnityEngine;

/// <summary>
/// 敌人状态基类
/// </summary>
public abstract class EnemyState
{
    protected EnemyController controller;
    
    public EnemyState(EnemyController controller)
    {
        this.controller = controller;
    }
    
    /// <summary>
    /// 进入状态时调用
    /// </summary>
    public virtual void Enter() { }
    
    /// <summary>
    /// 状态更新时调用
    /// </summary>
    public virtual void Update() { }
    
    /// <summary>
    /// 退出状态时调用
    /// </summary>
    public virtual void Exit() { }
    
    /// <summary>
    /// 检查是否应该切换到其他状态
    /// </summary>
    public virtual void CheckTransitions() { }
} 