using System.Collections;
using UnityEngine;

/// <summary>
/// 预警触手 - 用简单黑色矩形表示触手，从生成点指向中心塔
/// </summary>
public class WarningTentacle : MonoBehaviour
{
    [Header("触手属性")]
    [SerializeField] private SpriteRenderer tentacleRenderer;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    // 私有字段
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float tentacleLength;
    private float tentacleWidth;
    private float strength;
    private bool isVisible = false;
    
    private void Awake()
    {
        // 如果没有指定渲染器，自动查找
        if (tentacleRenderer == null)
        {
            tentacleRenderer = GetComponent<SpriteRenderer>();
        }
        
        // 确保有SpriteRenderer组件
        if (tentacleRenderer == null)
        {
            tentacleRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // 设置默认材质和颜色
        SetupDefaultAppearance();
    }
    
    /// <summary>
    /// 初始化触手
    /// </summary>
    public void Initialize(Vector3 start, Vector3 end, float length, float width, float enemyStrength)
    {
        startPosition = start;
        endPosition = end;
        tentacleLength = length;
        tentacleWidth = width;
        strength = enemyStrength;
        
        // 设置触手位置和旋转
        UpdateTentacleTransform();
        
        // 设置触手外观
        UpdateTentacleAppearance();
        
        // 显示触手
        Show();
    }
    
    /// <summary>
    /// 更新触手的位置和旋转
    /// </summary>
    private void UpdateTentacleTransform()
    {
        // 计算触手中心位置
        Vector3 centerPosition = (startPosition + endPosition) / 2f;
        transform.position = centerPosition;
        
        // 计算触手朝向
        Vector3 direction = (endPosition - startPosition).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // 设置触手大小
        transform.localScale = new Vector3(tentacleLength, tentacleWidth, 1f);
    }
    
    /// <summary>
    /// 更新触手外观
    /// </summary>
    private void UpdateTentacleAppearance()
    {
        if (tentacleRenderer == null) return;
        
        // 根据强度设置颜色
        Color tentacleColor = GetTentacleColorByStrength(strength);
        tentacleRenderer.color = tentacleColor;
        
        // 设置排序层级，确保触手在敌人之上但在UI之下
        tentacleRenderer.sortingOrder = 5;
    }
    
    /// <summary>
    /// 根据敌人强度获取触手颜色
    /// </summary>
    private Color GetTentacleColorByStrength(float enemyStrength)
    {
        // 基础黑色
        Color baseColor = Color.black;
        
        // 根据强度调整透明度
        float alpha = Mathf.Lerp(0.3f, 0.8f, Mathf.Clamp01(enemyStrength / 1000f));
        baseColor.a = alpha;
        
        // 根据强度添加颜色变化
        if (enemyStrength > 800f)
        {
            // 高强度：添加红色
            baseColor = Color.Lerp(baseColor, Color.red, 0.3f);
        }
        else if (enemyStrength > 500f)
        {
            // 中强度：添加橙色
            baseColor = Color.Lerp(baseColor, new Color(1f, 0.5f, 0f), 0.2f);
        }
        
        return baseColor;
    }
    
    /// <summary>
    /// 设置默认外观
    /// </summary>
    private void SetupDefaultAppearance()
    {
        if (tentacleRenderer == null) return;
        
        // 优先使用预制体自带的Sprite，只有在没有Sprite时才创建默认方块
        if (tentacleRenderer.sprite == null)
        {
            tentacleRenderer.sprite = CreateDefaultSprite();
        }
        
        tentacleRenderer.color = Color.black;
        tentacleRenderer.sortingOrder = 5;
    }
    
    /// <summary>
    /// 创建默认的Sprite
    /// </summary>
    private Sprite CreateDefaultSprite()
    {
        // 创建一个简单的白色方块纹理
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // 创建Sprite
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        
        return sprite;
    }
    
    /// <summary>
    /// 显示触手
    /// </summary>
    public void Show()
    {
        if (isVisible) return;
        
        isVisible = true;
        gameObject.SetActive(true);
        
        // 淡入效果
        StartCoroutine(FadeIn());
    }
    
    /// <summary>
    /// 隐藏触手
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;
        
        isVisible = false;
        
        // 淡出效果
        StartCoroutine(FadeOut());
    }
    
    /// <summary>
    /// 淡入效果
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (tentacleRenderer == null) yield break;
        
        Color startColor = tentacleRenderer.color;
        startColor.a = 0f;
        Color endColor = tentacleRenderer.color;
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            tentacleRenderer.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        
        tentacleRenderer.color = endColor;
    }
    
    /// <summary>
    /// 淡出效果
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (tentacleRenderer == null) yield break;
        
        Color startColor = tentacleRenderer.color;
        Color endColor = startColor;
        endColor.a = 0f;
        
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            
            tentacleRenderer.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        
        tentacleRenderer.color = endColor;
        
        // 完全隐藏后销毁
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 获取触手强度（用于调试）
    /// </summary>
    public float GetStrength()
    {
        return strength;
    }
    
    /// <summary>
    /// 获取触手长度（用于调试）
    /// </summary>
    public float GetLength()
    {
        return tentacleLength;
    }
    
    /// <summary>
    /// 获取触手宽度（用于调试）
    /// </summary>
    public float GetWidth()
    {
        return tentacleWidth;
    }
    
    private void OnDrawGizmos()
    {
        // 在Scene视图中绘制触手路径
        if (Application.isPlaying && isVisible)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startPosition, endPosition);
            
            // 绘制触手起点和终点
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startPosition, 0.1f);
            Gizmos.DrawWireSphere(endPosition, 0.1f);
        }
    }
}
