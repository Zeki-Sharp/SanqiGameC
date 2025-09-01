using UnityEngine;

/// <summary>
/// 基础视觉特效的抽象基类
/// </summary>
public abstract class BaseVisualEffect : MonoBehaviour
{
    [Header("基础配置")]
    public string effectName;
    public float duration = 1f;
    
    protected bool isActive = false;
    protected float timer = 0f;
    
    /// <summary>
    /// 激活特效
    /// </summary>
    public virtual void Activate()
    {
        isActive = true;
        timer = duration;
        OnActivate();
    }
    
    /// <summary>
    /// 停用特效
    /// </summary>
    public virtual void Deactivate()
    {
        isActive = false;
        OnDeactivate();
    }
    
    /// <summary>
    /// 更新特效
    /// </summary>
    public virtual void UpdateEffect(float deltaTime)
    {
        if (!isActive) return;
        
        timer -= deltaTime;
        if (timer <= 0f)
        {
            Deactivate();
            return;
        }
        
        OnUpdate(deltaTime);
    }
    
    /// <summary>
    /// 特效是否活跃
    /// </summary>
    public bool IsActive => isActive;
    
    /// <summary>
    /// 特效是否结束
    /// </summary>
    public bool IsFinished => timer <= 0f;
    
    /// <summary>
    /// Unity Update方法，管理特效生命周期
    /// </summary>
    private void Update()
    {
        if (isActive)
        {
            UpdateEffect(Time.deltaTime);
        }
    }
    
    // 抽象方法，子类实现具体逻辑
    protected abstract void OnActivate();
    protected abstract void OnDeactivate();
    protected abstract void OnUpdate(float deltaTime);
}

/// <summary>
/// 基础闪烁特效
/// </summary>
public class FlashEffect : BaseVisualEffect
{
    [Header("闪烁配置")]
    public float intensity = 0.8f;
    public float frequency = 8f;
    public Color flashColor = Color.white;
    
    [Header("闪烁模式")]
    public FlashMode flashMode = FlashMode.ColorFlash;
    
    /// <summary>
    /// 闪烁模式枚举
    /// </summary>
    public enum FlashMode
    {
        ColorFlash,     // 标准颜色闪烁（保持透明度）
        AlphaFlash      // 透明度闪烁（改变透明度）
    }
    
    private SpriteRenderer targetRenderer;
    private Color originalColor;
    private float flashTimer;
    
    protected override void OnActivate()
    {
        // 先尝试在当前对象上查找SpriteRenderer
        targetRenderer = GetComponent<SpriteRenderer>();
        
        // 如果没找到，尝试在子对象中查找
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (targetRenderer != null)
        {
            originalColor = targetRenderer.color;
            flashTimer = 0f;
            Debug.Log($"[FlashEffect] 找到SpriteRenderer: {targetRenderer.name}, 闪烁模式: {flashMode}");
        }
        else
        {
            Debug.LogError($"[FlashEffect] 未找到SpriteRenderer，对象: {gameObject.name}");
        }
    }
    
    protected override void OnUpdate(float deltaTime)
    {
        if (targetRenderer == null) return;
        
        flashTimer += deltaTime;
        float flashCycle = 1f / frequency;
        
        // 计算闪烁进度 (0-1)
        float flashProgress = (flashTimer % flashCycle) / flashCycle;
        
        if (flashMode == FlashMode.ColorFlash)
        {
            // 标准颜色闪烁模式：在原始颜色和闪烁颜色之间切换，保持透明度
            float flashIntensity = Mathf.Sin(flashProgress * Mathf.PI * 2f) * 0.5f + 0.5f;
            Color targetColor = Color.Lerp(originalColor, flashColor, flashIntensity * intensity);
            targetColor.a = originalColor.a; // 保持原始透明度
            targetRenderer.color = targetColor;
        }
        else
        {
            // 透明度闪烁模式：改变透明度，保持颜色
            float alphaIntensity = Mathf.Sin(flashProgress * Mathf.PI * 2f) * 0.5f + 0.5f;
            float targetAlpha = Mathf.Lerp(originalColor.a, 0.3f, alphaIntensity * intensity);
            Color targetColor = originalColor;
            targetColor.a = targetAlpha;
            targetRenderer.color = targetColor;
        }
    }
    
    protected override void OnDeactivate()
    {
        if (targetRenderer != null)
        {
            targetRenderer.color = originalColor;
        }
    }
}

/// <summary>
/// 基础击退特效
/// </summary>
public class KnockbackEffect : BaseVisualEffect
{
    [Header("击退配置")]
    public float force = 2f;
    public float knockbackDuration = 0.3f;
    
    private Vector3 originalPosition;
    private Vector3 knockbackDirection;
    private float knockbackTimer;
    
    protected override void OnActivate()
    {
        originalPosition = transform.position;
        knockbackDirection = GetKnockbackDirection();
        knockbackTimer = 0f;
    }
    
    /// <summary>
    /// 获取击退方向：敌人和中心塔连线，朝远离中心塔的方向
    /// </summary>
    private Vector3 GetKnockbackDirection()
    {
        // 查找中心塔
        GameObject centerTower = GameObject.FindGameObjectWithTag("CenterTower");
        if (centerTower != null)
        {
            // 计算从中心塔到敌人的方向向量
            Vector3 directionToEnemy = transform.position - centerTower.transform.position;
            
            // 如果距离太近，使用随机方向作为备选
            if (directionToEnemy.magnitude < 0.1f)
            {
                return Random.insideUnitCircle.normalized;
            }
            
            // 返回远离中心塔的方向
            return directionToEnemy.normalized;
        }
        
        // 如果找不到中心塔，使用随机方向作为备选
        return Random.insideUnitCircle.normalized;
    }
    
    protected override void OnUpdate(float deltaTime)
    {
        knockbackTimer += deltaTime;
        float progress = knockbackTimer / knockbackDuration;
        
        if (progress >= 1f)
        {
            // 击退完成，保持在击退后的位置，不回到原位置
            // 这样敌人会在击退后的位置继续移动或攻击
        }
        else
        {
            // 计算击退位置并实际改变位置
            Vector3 knockbackPosition = originalPosition + knockbackDirection * force * progress;
            transform.position = knockbackPosition;
        }
    }
    
    protected override void OnDeactivate()
    {
        // 击退效果结束时，保持在当前位置，不回到原位置
        // 这样敌人会在击退后的位置继续游戏逻辑
    }
}

/// <summary>
/// 基础缩放特效
/// </summary>
public class ScaleEffect : BaseVisualEffect
{
    [Header("缩放配置")]
    public Vector3 targetScale = Vector3.one * 1.2f;
    public bool elasticReturn = true;
    
    private Vector3 originalScale;
    private float scaleTimer;
    
    protected override void OnActivate()
    {
        originalScale = transform.localScale;
        scaleTimer = 0f;
    }
    
    protected override void OnUpdate(float deltaTime)
    {
        scaleTimer += deltaTime;
        float progress = scaleTimer / duration;
        
        if (progress >= 1f)
        {
            // 缩放完成
            if (elasticReturn)
            {
                transform.localScale = originalScale;
            }
        }
        else
        {
            // 计算缩放值
            Vector3 currentScale = Vector3.Lerp(originalScale, targetScale, progress);
            transform.localScale = currentScale;
        }
    }
    
    protected override void OnDeactivate()
    {
        transform.localScale = originalScale;
    }
}
