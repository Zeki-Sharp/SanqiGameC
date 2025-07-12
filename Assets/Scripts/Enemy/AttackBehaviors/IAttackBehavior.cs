using UnityEngine;

/// <summary>
/// 攻击行为接口 - 定义所有攻击行为需要实现的方法
/// </summary>
public interface IAttackBehavior
{
    /// <summary>
    /// 执行攻击
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="target">目标</param>
    void PerformAttack(EnemyController attacker, GameObject target);
    
    /// <summary>
    /// 检查是否可以攻击
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="target">目标</param>
    /// <returns>是否可以攻击</returns>
    bool CanAttack(EnemyController attacker, GameObject target);
    
    /// <summary>
    /// 获取攻击冷却时间
    /// </summary>
    /// <returns>攻击冷却时间</returns>
    float GetAttackCooldown();
} 