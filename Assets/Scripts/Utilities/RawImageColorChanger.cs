using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RawImageColorController : MonoBehaviour, IPointerClickHandler
{
    [Header("颜色配置")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(0.8f, 0.8f, 0.8f, 1f); // 灰色高亮
    [SerializeField] private float transitionDuration = 0.2f; // 颜色过渡时间
    
    private RawImage rawImage;
    private Coroutine currentTransition;

    private void Awake()
    {
        // 自动获取RawImage组件
        rawImage = GetComponent<RawImage>();
        
        // 初始化颜色
        rawImage.color = normalColor;
        
        // 验证必要组件
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogError($"必须为{gameObject.name}添加Collider2D组件以支持点击检测");
        }
    }

    // 实现IPointerClickHandler接口
    public void OnPointerClick(PointerEventData eventData)
    {
        // Debug.Log($"RawImage被点击: {gameObject.name}，触发动画过渡");
        StartColorTransition(highlightColor);
    }

    // 开始颜色过渡
    private void StartColorTransition(Color targetColor)
    {
        // 停止当前过渡协程（如果存在）
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        
        // 启动新的颜色过渡协程
        currentTransition = StartCoroutine(ColorTransitionCoroutine(targetColor));
    }

    // 颜色过渡协程
    private IEnumerator ColorTransitionCoroutine(Color targetColor)
    {
        Color startColor = rawImage.color;
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            rawImage.color = Color.Lerp(startColor, targetColor, elapsedTime / transitionDuration);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // 确保最终颜色精确到位
        rawImage.color = targetColor;
        currentTransition = null;
        ResetToNormalColorAfter(0.05f);
        // Debug.Log($"颜色过渡完成: {gameObject.name}，最终颜色: {targetColor}");
    }

    // 可选：添加点击后恢复原色的逻辑
    private void ResetToNormalColorAfter(float delay)
    {
        currentTransition = StartCoroutine(ResetToNormalColorCoroutine(delay));
    }

    private IEnumerator ResetToNormalColorCoroutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        StartColorTransition(normalColor);
    }
}
