using UnityEngine;

/// <summary>
/// 预警触手 - 用于显示敌人生成的预警效果
/// </summary>
public class WarningTentacle : MonoBehaviour
{
    [Header("渲染设置")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color startColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private Color endColor = new Color(1f, 0f, 0f, 0f);
    
    [Header("动画设置")]
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseMinAlpha = 0.2f;
    [SerializeField] private float pulseMaxAlpha = 0.8f;
    
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float strength;
    private float currentPulseTime;
    
    /// <summary>
    /// 初始化触手
    /// </summary>
    public void Initialize(Vector3 start, Vector3 end, float length, float width, float strength)
    {
        startPosition = start;
        endPosition = end;
        this.strength = strength;
        
        // 设置位置和旋转
        transform.position = start;
        Vector3 direction = (end - start).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 设置缩放（长度和宽度）
        transform.localScale = new Vector3(length, width, 1f);
        
        // 设置初始颜色
        if (spriteRenderer != null)
        {
            spriteRenderer.color = startColor;
        }
        
        // 开始脉冲动画
        currentPulseTime = 0f;
    }
    
    private void Update()
    {
        if (spriteRenderer != null)
        {
            // 更新脉冲动画
            currentPulseTime += Time.deltaTime * pulseSpeed;
            float pulseAlpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, 
                (Mathf.Sin(currentPulseTime) + 1f) * 0.5f);
            
            // 根据强度调整颜色
            float strengthFactor = Mathf.Clamp01(strength / 1000f);
            Color currentColor = Color.Lerp(startColor, endColor, strengthFactor);
            currentColor.a = pulseAlpha;
            
            spriteRenderer.color = currentColor;
        }
    }
    
    /// <summary>
    /// 隐藏触手
    /// </summary>
    public void Hide()
    {
        // 渐隐动画
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}