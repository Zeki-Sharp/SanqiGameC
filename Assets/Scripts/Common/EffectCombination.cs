using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 单个特效的配置数据
/// </summary>
[System.Serializable]
public class EffectConfig
{
    [Header("特效类型")]
    public EffectType effectType;
    
    [Header("特效参数")]
    public float duration = 1f;
    public float delay = 0f; // 延迟启动时间
    
    [Header("闪烁特效参数")]
    [ShowIf("effectType", EffectType.Flash)]
    public float intensity = 0.8f;
    
    [ShowIf("effectType", EffectType.Flash)]
    public float frequency = 8f;
    
    [ShowIf("effectType", EffectType.Flash)]
    public Color flashColor = Color.white;
    
    [ShowIf("effectType", EffectType.Flash)]
    public FlashEffect.FlashMode flashMode = FlashEffect.FlashMode.ColorFlash;
    
    [Header("击退特效参数")]
    [ShowIf("effectType", EffectType.Knockback)]
    public float force = 2f;
    
    [ShowIf("effectType", EffectType.Knockback)]
    public float knockbackDuration = 0.3f;
    
    [Header("缩放特效参数")]
    [ShowIf("effectType", EffectType.Scale)]
    public Vector3 targetScale = Vector3.one * 1.2f;
    
    [ShowIf("effectType", EffectType.Scale)]
    public bool elasticReturn = true;
}

/// <summary>
/// 特效类型枚举
/// </summary>
public enum EffectType
{
    Flash,      // 闪烁
    Knockback,  // 击退
    Scale       // 缩放
}

/// <summary>
/// 特效组合配置
/// </summary>
[System.Serializable]
public class EffectCombination
{
    [Header("组合配置")]
    public string combinationName;
    
    [Header("执行方式")]
    public ExecutionMode executionMode = ExecutionMode.Parallel; // 默认并行执行
    
    [Header("特效列表")]
    public List<EffectConfig> effects = new List<EffectConfig>();
    
    /// <summary>
    /// 执行方式枚举
    /// </summary>
    public enum ExecutionMode
    {
        Parallel,   // 并行执行：所有特效同时开始
        Sequential  // 顺序执行：特效按顺序依次执行
    }
    
    /// <summary>
    /// 应用特效组合到目标对象
    /// </summary>
    public void ApplyToTarget(GameObject target, MonoBehaviour coroutineRunner)
    {
        if (target == null || effects.Count == 0) return;
        
        if (executionMode == ExecutionMode.Parallel)
        {
            // 并行执行：所有特效同时开始
            ApplyEffectsParallel(target, coroutineRunner);
        }
        else
        {
            // 顺序执行：特效按顺序依次执行
            coroutineRunner.StartCoroutine(ApplyEffectsSequential(target, coroutineRunner));
        }
    }
    
    /// <summary>
    /// 并行执行特效
    /// </summary>
    private void ApplyEffectsParallel(GameObject target, MonoBehaviour coroutineRunner)
    {
        foreach (var effectConfig in effects)
        {
            BaseVisualEffect effect = CreateEffectComponent(target, effectConfig);
            if (effect != null)
            {
                if (effectConfig.delay > 0)
                {
                    coroutineRunner.StartCoroutine(ActivateEffectWithDelay(effect, effectConfig.delay));
                }
                else
                {
                    effect.Activate();
                }
            }
        }
    }
    
    /// <summary>
    /// 顺序执行特效
    /// </summary>
    private System.Collections.IEnumerator ApplyEffectsSequential(GameObject target, MonoBehaviour coroutineRunner)
    {
        foreach (var effectConfig in effects)
        {
            BaseVisualEffect effect = CreateEffectComponent(target, effectConfig);
            if (effect != null)
            {
                // 等待延迟时间
                if (effectConfig.delay > 0)
                {
                    yield return new WaitForSeconds(effectConfig.delay);
                }
                
                // 激活特效
                effect.Activate();
                
                // 等待特效完成（如果配置了duration）
                if (effectConfig.duration > 0)
                {
                    yield return new WaitForSeconds(effectConfig.duration);
                }
            }
        }
    }
    
    /// <summary>
    /// 根据配置创建特效组件
    /// </summary>
    private BaseVisualEffect CreateEffectComponent(GameObject target, EffectConfig config)
    {
        BaseVisualEffect effect = null;
        
        // 清理同类型的旧特效组件，避免冲突
        CleanupOldEffects(target, config.effectType);
        
        switch (config.effectType)
        {
            case EffectType.Flash:
                effect = target.AddComponent<FlashEffect>();
                if (effect is FlashEffect flashEffect)
                {
                    flashEffect.intensity = config.intensity;
                    flashEffect.frequency = config.frequency;
                    flashEffect.flashColor = config.flashColor;
                    flashEffect.duration = config.duration;
                    flashEffect.flashMode = (FlashEffect.FlashMode)config.flashMode;
                }
                break;
                
            case EffectType.Knockback:
                effect = target.AddComponent<KnockbackEffect>();
                if (effect is KnockbackEffect knockbackEffect)
                {
                    knockbackEffect.force = config.force;
                    knockbackEffect.knockbackDuration = config.knockbackDuration;
                    knockbackEffect.duration = config.duration;
                }
                break;
                
            case EffectType.Scale:
                effect = target.AddComponent<ScaleEffect>();
                if (effect is ScaleEffect scaleEffect)
                {
                    scaleEffect.targetScale = config.targetScale;
                    scaleEffect.elasticReturn = config.elasticReturn;
                    scaleEffect.duration = config.duration;
                }
                break;
        }
        
        return effect;
    }
    
    /// <summary>
    /// 清理同类型的旧特效组件
    /// </summary>
    private void CleanupOldEffects(GameObject target, EffectType effectType)
    {
        switch (effectType)
        {
            case EffectType.Flash:
                var oldFlashEffects = target.GetComponents<FlashEffect>();
                foreach (var oldEffect in oldFlashEffects)
                {
                    if (oldEffect != null)
                    {
                        oldEffect.Deactivate();
                        Object.Destroy(oldEffect);
                    }
                }
                break;
                
            case EffectType.Knockback:
                var oldKnockbackEffects = target.GetComponents<KnockbackEffect>();
                foreach (var oldEffect in oldKnockbackEffects)
                {
                    if (oldEffect != null)
                    {
                        oldEffect.Deactivate();
                        Object.Destroy(oldEffect);
                    }
                }
                break;
                
            case EffectType.Scale:
                var oldScaleEffects = target.GetComponents<ScaleEffect>();
                foreach (var oldEffect in oldScaleEffects)
                {
                    if (oldEffect != null)
                    {
                        oldEffect.Deactivate();
                        Object.Destroy(oldEffect);
                    }
                }
                break;
        }
    }
    
    /// <summary>
    /// 延迟激活特效的协程
    /// </summary>
    private System.Collections.IEnumerator ActivateEffectWithDelay(BaseVisualEffect effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        effect.Activate();
    }
}

