using UnityEngine;

/// <summary>
/// 视效控制器 - 负责播放特效组合
/// </summary>
public class VisualEffectController : MonoBehaviour
{
    [Header("特效组合配置")]
    [SerializeField] private EffectCombinationPreset effectPreset;
    
    private void Awake()
    {
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }
    }
    
    // ===== 特效播放接口 =====
    
    /// <summary>
    /// 播放特效组合（通过名称）
    /// </summary>
    public void PlayEffect(string combinationName)
    {
        if (effectPreset == null) return;
        
        var combination = effectPreset.GetCombination(combinationName);
        if (combination != null)
        {
            combination.ApplyToTarget(gameObject, this);
        }
    }
    
    /// <summary>
    /// 播放特效组合（通过特效文件）
    /// </summary>
    public void PlayEffectFromPreset(EffectCombinationPreset preset, string combinationName)
    {
        if (preset == null) return;
        
        var combination = preset.GetCombination(combinationName);
        if (combination != null)
        {
            combination.ApplyToTarget(gameObject, this);
        }
    }
    
    /// <summary>
    /// 播放自定义特效组合
    /// </summary>
    public void PlayEffectCombination(EffectCombination combination)
    {
        if (combination != null)
        {
            combination.ApplyToTarget(gameObject, this);
        }
    }
    
    /// <summary>
    /// 获取所有可用的特效组合名称
    /// </summary>
    public System.Collections.Generic.List<string> GetAvailableEffects()
    {
        return effectPreset != null ? effectPreset.GetAllCombinationNames() : new System.Collections.Generic.List<string>();
    }
}
