using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 场景层级管理器 - 统一管理enemy、tower、bullet、场景设施的层级
/// 通过sortingOrder自动管理前后遮挡关系，实现基于位置的正确渲染顺序
/// </summary>
public class SceneLayerManager : MonoBehaviour
{
    [Header("层级设置")]
    [SerializeField] private string sortingLayerName = "SceneObject";
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool enableAutoSortingLayer = false; // 暂时禁用自动Sorting Layer设置
    [SerializeField] private bool forceSetSortingLayer = true; // 强制设置Sorting Layer
    
    [Header("标签配置")]
    [SerializeField] private string[] sceneObjectTags = { "SceneObject", "Block" };
    [SerializeField] private string[] enemyTags = { "Enemy" };
    [SerializeField] private string[] towerTags = { "Tower", "CenterTower" };
    [SerializeField] private string[] bulletTags = { "Bullet" };
    
    // 注册的对象列表
    private List<GameObject> registeredObjects = new List<GameObject>();
    private Dictionary<GameObject, Renderer[]> objectRenderers = new Dictionary<GameObject, Renderer[]>();
    
    // 单例实例
    private static SceneLayerManager instance;
    public static SceneLayerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SceneLayerManager>();
                if (instance == null)
                {
                    Debug.LogError("SceneLayerManager未找到！请在场景中添加SceneLayerManager组件。");
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Log("SceneLayerManager已初始化");
        }
        else if (instance != this)
        {
            Debug.LogWarning("发现重复的SceneLayerManager，销毁当前实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 注册到GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSystem(this);
        }
        
        // 验证Sorting Layer配置
        ValidateSortingLayerConfiguration();
        
        // 强制设置所有对象的Sorting Layer
        if (forceSetSortingLayer)
        {
            Log("强制设置所有对象的Sorting Layer为: " + sortingLayerName);
            ForceSetAllObjectsSortingLayer();
            
            // 特殊处理预览塔：设置到UI层，确保显示在最前面
            ForceSetPreviewTowersToUILayer();
        }
        
        // 暂时禁用自动初始化，避免破坏现有渲染
        if (enableAutoSortingLayer)
        {
            InitializeLayerManagement();
        }
        else
        {
            Log("自动Sorting Layer设置已禁用，只进行对象注册");
            // 只扫描对象，不修改Sorting Layer
            ScanExistingObjectsForRegistration();
        }
    }
    
    void Update()
    {
        // 持续监控预览塔，确保新创建的预览塔被正确设置层级
        MonitorPreviewTowers();
        
        // 暂时禁用自动检测，避免破坏现有渲染
        if (enableAutoSortingLayer)
        {
            AutoDetectAndRegisterObjects();
        }
    }
    
    /// <summary>
    /// 初始化层级管理
    /// </summary>
    private void InitializeLayerManagement()
    {
        Log("开始初始化场景层级管理...");
        
        // 设置Sorting Layer
        SetupSortingLayer();
        
        // 扫描场景中现有的对象
        ScanExistingObjects();
        
        // 应用初始层级
        UpdateAllObjectLayers();
        
        Log($"场景层级管理初始化完成，已注册 {registeredObjects.Count} 个对象");
    }
    
    /// <summary>
    /// 只扫描对象进行注册，不修改Sorting Layer
    /// </summary>
    private void ScanExistingObjectsForRegistration()
    {
        Log("开始扫描场景对象（仅注册，不修改Sorting Layer）...");
        
        // 扫描塔
        ScanObjectsByTagsForRegistration(towerTags);
        
        // 扫描敌人
        ScanObjectsByTagsForRegistration(enemyTags);
        
        // 扫描子弹
        ScanObjectsByTagsForRegistration(bulletTags);
        
        // 扫描场景物体
        ScanObjectsByTagsForRegistration(sceneObjectTags);
        
        Log($"扫描完成，已注册 {registeredObjects.Count} 个对象（未修改Sorting Layer）");
    }
    
    /// <summary>
    /// 根据标签扫描对象（仅注册，不修改Sorting Layer）
    /// </summary>
    private void ScanObjectsByTagsForRegistration(string[] tags)
    {
        foreach (string tag in tags)
        {
            // 跳过子弹标签，让子弹保持自己的层级设置
            if (tag == "Bullet")
            {
                Log($"跳过子弹标签 {tag}，让子弹保持自己的层级设置");
                continue;
            }
            
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                RegisterObjectForMonitoring(obj);
            }
        }
    }
    
    /// <summary>
    /// 强制设置所有对象的Sorting Layer
    /// </summary>
    private void ForceSetAllObjectsSortingLayer()
    {
        Log("开始强制设置所有对象的Sorting Layer...");
        
        // 强制设置塔的Sorting Layer
        ForceSetObjectsByTag("Tower");
        ForceSetObjectsByTag("CenterTower");
        ForceSetObjectsByTag("Enemy");
        ForceSetObjectsByTag("Bullet");
        ForceSetObjectsByTag("SceneObject");
        ForceSetObjectsByTag("Block");
        
        Log("强制设置Sorting Layer完成");
    }
    
    /// <summary>
    /// 强制设置指定标签对象的Sorting Layer
    /// </summary>
    private void ForceSetObjectsByTag(string tag)
    {
        // 跳过子弹，让子弹保持自己的层级设置（由PathBullet2D插件管理）
        if (tag == "Bullet") 
        {
            Log($"跳过子弹标签 {tag}，让子弹保持自己的层级设置");
            return;
        }
        
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        Log($"找到 {objects.Length} 个 {tag} 标签的对象");
        
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                ForceSetObjectSortingLayer(obj);
            }
        }
    }
    
    /// <summary>
    /// 持续监控预览塔，确保新创建的预览塔被正确设置层级
    /// </summary>
    private void MonitorPreviewTowers()
    {
        GameObject[] previewTowers = GameObject.FindGameObjectsWithTag("PreviewTower");
        
        foreach (GameObject previewTower in previewTowers)
        {
            if (previewTower != null)
            {
                // 检查预览塔是否已经设置了正确的层级
                Renderer[] renderers = previewTower.GetComponentsInChildren<Renderer>();
                bool needsUpdate = false;
                
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null && (renderer.sortingLayerName != "UI"))
                    {
                        needsUpdate = true;
                        break;
                    }
                }
                
                // 如果需要更新，则设置正确的层级
                if (needsUpdate)
                {
                    Log($"发现需要更新层级的预览塔: {previewTower.name}");
                    ForceSetPreviewTowerToUILayer(previewTower);
                }
            }
        }
    }
    
    /// <summary>
    /// 特殊处理预览塔：设置到UI层，确保显示在最前面
    /// </summary>
    private void ForceSetPreviewTowersToUILayer()
    {
        GameObject[] previewTowers = GameObject.FindGameObjectsWithTag("PreviewTower");
        Log($"找到 {previewTowers.Length} 个预览塔，设置到UI层");
        
        foreach (GameObject previewTower in previewTowers)
        {
            if (previewTower != null)
            {
                ForceSetPreviewTowerToUILayer(previewTower);
            }
        }
    }
    
    /// <summary>
    /// 设置单个预览塔到UI层
    /// </summary>
    private void ForceSetPreviewTowerToUILayer(GameObject previewTower)
    {
        if (previewTower == null) return;
        
        Log($"开始设置预览塔 {previewTower.name} 的层级...");
        
        // 获取所有渲染器组件
        Renderer[] renderers = previewTower.GetComponentsInChildren<Renderer>();
        Log($"预览塔 {previewTower.name} 找到 {renderers.Length} 个Renderer组件");
        
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[SceneLayerManager] 预览塔 {previewTower.name} 没有Renderer组件！");
            return;
        }
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                string oldLayer = renderer.sortingLayerName;
                int oldOrder = renderer.sortingOrder;
                
                Log($"设置Renderer {renderer.name}: 当前层={oldLayer}, 当前Order={oldOrder}");
                
                // 先尝试设置到"UI"层
                try
                {
                    renderer.sortingLayerName = "UI";
                    Log($"尝试设置到UI层: {oldLayer} -> {renderer.sortingLayerName}");
                    
                    // 验证设置是否成功
                    if (renderer.sortingLayerName == "UI")
                    {
                        Log($"✅ 预览塔 {previewTower.name} 成功设置到UI层");
                    }
                    else
                    {
                        Debug.LogError($"[SceneLayerManager] ❌ 预览塔设置失败！{previewTower.name}: {oldLayer} -> {renderer.sortingLayerName}");
                        
                        // 如果UI层设置失败，尝试使用"Default"层但设置很高的Order
                        Log($"尝试使用Default层 + 高Order作为备选方案");
                        renderer.sortingLayerName = "Default";
                        renderer.sortingOrder += 1000;
                        Log($"备选方案: 设置到Default层，Order={renderer.sortingOrder}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SceneLayerManager] 设置预览塔层级时发生异常: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[SceneLayerManager] 预览塔 {previewTower.name} 的Renderer组件为null");
            }
        }
    }
    
    /// <summary>
    /// 强制设置单个对象的Sorting Layer
    /// </summary>
    private void ForceSetObjectSortingLayer(GameObject obj)
    {
        if (obj == null) return;
        
        // 获取所有渲染器组件
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                string oldLayer = renderer.sortingLayerName;
                renderer.sortingLayerName = sortingLayerName;
                
                // 验证设置是否成功
                if (renderer.sortingLayerName == sortingLayerName)
                {
                    Log($"✅ 强制设置 {obj.name} 的Sorting Layer: {oldLayer} -> {sortingLayerName}");
                    
                    // 同时计算并设置Order in Layer
                    int sortingOrder = CalculateSortingOrder(obj.transform.position);
                    renderer.sortingOrder = sortingOrder;
                    Log($"✅ 设置 {obj.name} 的Order in Layer: {sortingOrder} (位置: {obj.transform.position})");
                }
                else
                {
                    Debug.LogError($"[SceneLayerManager] ❌ 强制设置失败！{obj.name}: {oldLayer} -> {renderer.sortingLayerName} (期望: {sortingLayerName})");
                }
            }
        }
    }
    
    /// <summary>
    /// 验证Sorting Layer配置
    /// </summary>
    private void ValidateSortingLayerConfiguration()
    {
        Log($"验证Sorting Layer配置: {sortingLayerName}");
        
        // 检查Sorting Layer名称是否为空
        if (string.IsNullOrEmpty(sortingLayerName))
        {
            Debug.LogError("[SceneLayerManager] sortingLayerName为空！");
            return;
        }
        
        // 尝试获取Sorting Layer ID
        int layerID = SortingLayer.NameToID(sortingLayerName);
        if (layerID == -1)
        {
            Debug.LogError($"[SceneLayerManager] 无法找到Sorting Layer: {sortingLayerName}");
            Debug.LogError("[SceneLayerManager] 请检查TagManager.asset中的m_SortingLayers配置");
        }
        else
        {
            Log($"✅ Sorting Layer '{sortingLayerName}' 验证成功，ID: {layerID}");
        }
    }
    
    /// <summary>
    /// 设置Sorting Layer
    /// </summary>
    private void SetupSortingLayer()
    {
        // 确保所有注册的对象都使用正确的Sorting Layer
        Log($"设置Sorting Layer: {sortingLayerName}");
    }
    
    /// <summary>
    /// 扫描场景中现有的对象
    /// </summary>
    private void ScanExistingObjects()
    {
        // 扫描塔
        ScanObjectsByTags(towerTags);
        
        // 扫描敌人
        ScanObjectsByTags(enemyTags);
        
        // 扫描子弹
        ScanObjectsByTags(bulletTags);
        
        // 扫描场景物体
        ScanObjectsByTags(sceneObjectTags);
        
        Log($"扫描完成，找到 {registeredObjects.Count} 个对象");
    }
    
    /// <summary>
    /// 根据标签扫描对象
    /// </summary>
    private void ScanObjectsByTags(string[] tags)
    {
        foreach (string tag in tags)
        {
            // 跳过子弹标签，让子弹保持自己的层级设置
            if (tag == "Bullet")
            {
                Log($"跳过子弹标签 {tag}，让子弹保持自己的层级设置");
                continue;
            }
            
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                RegisterObject(obj);
            }
        }
    }
    
    /// <summary>
    /// 自动检测并注册新对象
    /// </summary>
    private void AutoDetectAndRegisterObjects()
    {
        if (!enableAutoSortingLayer) return;
        
        // 自动检测塔
        ScanObjectsByTags(towerTags);
        
        // 自动检测敌人
        ScanObjectsByTags(enemyTags);
        
        // 跳过子弹，让子弹保持自己的层级设置
        Log("跳过子弹标签，让子弹保持自己的层级设置");
        
        // 自动检测场景物体
        ScanObjectsByTags(sceneObjectTags);
    }
    
    /// <summary>
    /// 注册对象到层级管理器（仅监控，不修改Sorting Layer）
    /// </summary>
    private void RegisterObjectForMonitoring(GameObject obj)
    {
        if (obj == null || registeredObjects.Contains(obj))
            return;
        
        // 检查对象是否有渲染器组件
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;
        
        // 注册对象（仅监控）
        registeredObjects.Add(obj);
        objectRenderers[obj] = renderers;
        
        Log($"注册对象（仅监控）: {obj.name} (标签: {obj.tag})");
    }
    
    /// <summary>
    /// 注册对象到层级管理器
    /// </summary>
    public void RegisterObject(GameObject obj)
    {
        if (obj == null || registeredObjects.Contains(obj))
            return;
        
        // 检查对象是否有渲染器组件
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;
        
        // 注册对象
        registeredObjects.Add(obj);
        objectRenderers[obj] = renderers;
        
        // 只有在启用自动Sorting Layer时才设置
        if (enableAutoSortingLayer)
        {
            // 设置Sorting Layer
            SetObjectSortingLayer(obj, renderers);
            
            // 计算并设置层级
            UpdateObjectLayer(obj);
        }
        
        Log($"注册对象: {obj.name} (标签: {obj.tag})");
    }
    
    /// <summary>
    /// 设置对象的Sorting Layer
    /// </summary>
    private void SetObjectSortingLayer(GameObject obj, Renderer[] renderers)
    {
        if (!enableAutoSortingLayer) return;
        
        // 验证Sorting Layer名称是否有效
        if (string.IsNullOrEmpty(sortingLayerName))
        {
            Debug.LogError("[SceneLayerManager] sortingLayerName为空！");
            return;
        }
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                string oldLayer = renderer.sortingLayerName;
                renderer.sortingLayerName = sortingLayerName;
                
                // 验证设置是否成功
                if (renderer.sortingLayerName == sortingLayerName)
                {
                    Log($"成功设置 {obj.name} 的Sorting Layer: {oldLayer} -> {sortingLayerName}");
                }
                else
                {
                    Debug.LogError($"[SceneLayerManager] 设置失败！{obj.name}: {oldLayer} -> {renderer.sortingLayerName} (期望: {sortingLayerName})");
                }
            }
        }
    }
    
    /// <summary>
    /// 更新所有对象的层级
    /// </summary>
    public void UpdateAllObjectLayers()
    {
        if (!enableAutoSortingLayer) return;
        
        foreach (GameObject obj in registeredObjects.ToList())
        {
            if (obj != null)
            {
                UpdateObjectLayer(obj);
            }
            else
            {
                // 清理已销毁的对象
                registeredObjects.Remove(obj);
                objectRenderers.Remove(obj);
            }
        }
    }
    
    /// <summary>
    /// 更新单个对象的层级
    /// </summary>
    public void UpdateObjectLayer(GameObject obj)
    {
        if (obj == null || !objectRenderers.ContainsKey(obj) || !enableAutoSortingLayer)
            return;
        
        // 计算基于Y轴位置的层级
        int sortingOrder = CalculateSortingOrder(obj.transform.position);
        
        // 应用层级到所有渲染器
        Renderer[] renderers = objectRenderers[obj];
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.sortingOrder = sortingOrder;
            }
        }
        
        if (enableDebugLog)
        {
            Log($"更新对象层级: {obj.name} -> sortingOrder: {sortingOrder} (位置: {obj.transform.position})");
        }
    }
    
    /// <summary>
    /// 计算基于Y轴位置的层级
    /// </summary>
    private int CalculateSortingOrder(Vector3 position)
    {
        // 公式：sortingOrder = -Y坐标值 * 10（Y越大，值越小，越靠后）
        // 乘以10增加层级差异，避免Y坐标相近时层级相同
        // 所有游戏对象（场景物体、敌人、塔、子弹）都使用相同的计算方式
        // 这样可以保持正确的空间遮挡关系
        return Mathf.RoundToInt(-position.y * 10);
    }
    
    /// <summary>
    /// 手动注册对象（供外部调用）
    /// </summary>
    public void ManualRegisterObject(GameObject obj)
    {
        if (obj != null)
        {
            RegisterObject(obj);
        }
    }
    
    /// <summary>
    /// 手动更新对象层级（供外部调用）
    /// </summary>
    public void ManualUpdateObjectLayer(GameObject obj)
    {
        if (obj != null)
        {
            UpdateObjectLayer(obj);
        }
    }
    
    /// <summary>
    /// 获取已注册的对象数量
    /// </summary>
    public int GetRegisteredObjectCount()
    {
        return registeredObjects.Count;
    }
    
    /// <summary>
    /// 获取已注册的对象列表
    /// </summary>
    public List<GameObject> GetRegisteredObjects()
    {
        return new List<GameObject>(registeredObjects);
    }
    
    /// <summary>
    /// 清理已销毁的对象
    /// </summary>
    public void CleanupDestroyedObjects()
    {
        registeredObjects.RemoveAll(obj => obj == null);
        
        var keysToRemove = new List<GameObject>();
        foreach (var kvp in objectRenderers)
        {
            if (kvp.Key == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            objectRenderers.Remove(key);
        }
    }
    
    /// <summary>
    /// 调试日志
    /// </summary>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SceneLayerManager] {message}");
        }
    }
    
    /// <summary>
    /// 在编辑器中显示调试信息
    /// </summary>
    void OnGUI()
    {
        if (!enableDebugLog) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"SceneLayerManager 调试信息");
        GUILayout.Label($"已注册对象: {registeredObjects.Count}");
        GUILayout.Label($"Sorting Layer: {sortingLayerName}");
        GUILayout.Label($"强制设置: {(forceSetSortingLayer ? "启用" : "禁用")}");
        GUILayout.Label($"自动Sorting Layer: {(enableAutoSortingLayer ? "启用" : "禁用")}");
        GUILayout.EndArea();
    }
}
