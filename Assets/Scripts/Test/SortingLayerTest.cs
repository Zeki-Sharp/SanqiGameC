using UnityEngine;

/// <summary>
/// 测试Sorting Layer设置的简单脚本
/// </summary>
public class SortingLayerTest : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private string testSortingLayer = "SceneObject";
    [SerializeField] private int testSortingOrder = 100;
    
    void Start()
    {
        // 获取所有渲染器组件
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                // 测试设置Sorting Layer
                string oldLayer = renderer.sortingLayerName;
                int oldOrder = renderer.sortingOrder;
                
                renderer.sortingLayerName = testSortingLayer;
                renderer.sortingOrder = testSortingOrder;
                
                Debug.Log($"[SortingLayerTest] {renderer.name}: {oldLayer} -> {renderer.sortingLayerName}, {oldOrder} -> {renderer.sortingOrder}");
                
                // 验证设置是否成功
                if (renderer.sortingLayerName == testSortingLayer)
                {
                    Debug.Log($"[SortingLayerTest] ✅ {renderer.name} 成功设置到 {testSortingLayer} 层");
                }
                else
                {
                    Debug.LogError($"[SortingLayerTest] ❌ {renderer.name} 设置失败，当前层: {renderer.sortingLayerName}");
                }
            }
        }
    }
}
