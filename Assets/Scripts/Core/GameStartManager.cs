using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using MoreMountains.Feedbacks;
using Michsky.LSS;

/// <summary>
/// 游戏启动管理器 - 协调MMF Player动画和场景加载
/// 当点击开始游戏按钮时，播放MMF Player动画，同时将Main场景additive加载到Cover场景正下方
/// MMF Player结束时提供触发时机
/// </summary>
public class GameStartManager : MonoBehaviour
{
    [Header("LLS Manager")]
    [SerializeField] private LoadingScreenManager llsManager;
    
    [Header("MMF Player")]
    [SerializeField] private MMF_Player startAnimationPlayer;
    
    [Header("场景配置")]
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private Vector3 mainSceneOffset = new Vector3(0, -1000, 0); // Main场景相对Cover场景的偏移
    [SerializeField] private float sceneLoadWaitTime = 0.1f; // 等待场景加载的时间
    
    [Header("事件")]
    public UnityEvent OnAnimationComplete;
    public UnityEvent OnMainSceneLoaded;
    
    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 缓存组件引用，避免重复查找
    private Camera coverMainCamera;
    private AudioListener coverAudioListener;
    private Camera[] mainSceneCameras;
    private AudioListener[] mainSceneAudioListeners;
    
    private void Start()
    {
        InitializeComponents();
    }
    
    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitializeComponents()
    {
        // 自动查找LLS Manager
        if (llsManager == null)
        {
            llsManager = FindFirstObjectByType<LoadingScreenManager>();
            if (llsManager != null && enableDebugLog)
            {
                Debug.Log("GameStartManager: 自动找到LLS Manager");
            }
        }
        
        // 缓存Cover场景的组件引用
        CacheCoverSceneComponents();
        
        // 监听MMF Player的完成事件
        if (startAnimationPlayer != null)
        {
            startAnimationPlayer.Events.OnComplete.AddListener(OnMMFPlayerComplete);
            if (enableDebugLog)
            {
                Debug.Log("GameStartManager: MMF Player事件监听已设置");
            }
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning("GameStartManager: MMF Player未设置");
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"GameStartManager初始化完成 - LLS Manager: {(llsManager != null ? "已找到" : "未找到")}, MMF Player: {(startAnimationPlayer != null ? "已设置" : "未设置")}");
        }
    }
    
    /// <summary>
    /// 缓存Cover场景的组件引用
    /// </summary>
    private void CacheCoverSceneComponents()
    {
        // 缓存Cover场景的主镜头
        coverMainCamera = Camera.main;
        
        // 缓存Cover场景的AudioListener
        coverAudioListener = FindFirstObjectByType<AudioListener>();
        
        if (enableDebugLog)
        {
            Debug.Log($"GameStartManager: 缓存Cover场景组件 - Camera: {(coverMainCamera != null ? "已找到" : "未找到")}, AudioListener: {(coverAudioListener != null ? "已找到" : "未找到")}");
        }
    }
    
    /// <summary>
    /// 开始游戏序列：播放动画 + 加载Main场景
    /// </summary>
    public void StartGameSequence()
    {
        if (enableDebugLog)
        {
            Debug.Log("GameStartManager: 开始游戏序列");
        }
        
        // 检查必要组件
        if (llsManager == null)
        {
            Debug.LogError("GameStartManager: LLS Manager未找到！无法加载Main场景");
            return;
        }
        
        // 1. 开始播放MMF Player动画
        if (startAnimationPlayer != null)
        {
            startAnimationPlayer.PlayFeedbacks();
            if (enableDebugLog)
            {
                Debug.Log("GameStartManager: MMF Player动画开始播放");
            }
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning("GameStartManager: MMF Player未设置，跳过动画播放");
        }
        
        // 2. 同时使用LLS Manager加载Main场景
        llsManager.LoadAdditiveScene(mainSceneName);
        if (enableDebugLog)
        {
            Debug.Log($"GameStartManager: 开始加载Main场景: {mainSceneName}");
        }
        
        // 3. 设置Main场景位置到正下方
        StartCoroutine(SetMainScenePosition());
    }
    
    /// <summary>
    /// 设置Main场景位置到Cover场景正下方
    /// </summary>
    private IEnumerator SetMainScenePosition()
    {
        // 等待Main场景加载完成
        yield return new WaitForSeconds(sceneLoadWaitTime);
        
        // 检查场景是否已加载
        Scene mainScene = SceneManager.GetSceneByName(mainSceneName);
        if (!mainScene.isLoaded)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"GameStartManager: Main场景 {mainSceneName} 尚未加载完成，继续等待...");
            }
            
            // 继续等待直到场景加载完成
            yield return new WaitUntil(() => SceneManager.GetSceneByName(mainSceneName).isLoaded);
        }
        
        // 获取Main场景的根对象
        GameObject[] rootObjects = mainScene.GetRootGameObjects();
        
        if (rootObjects.Length == 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"GameStartManager: Main场景 {mainSceneName} 没有根对象");
            }
            yield break;
        }
        
        // 将Main场景的所有根对象移动到指定位置
        foreach (GameObject rootObj in rootObjects)
        {
            rootObj.transform.position = mainSceneOffset;
            if (enableDebugLog)
            {
                Debug.Log($"GameStartManager: 设置Main场景对象位置: {rootObj.name} -> {mainSceneOffset}");
            }
        }
        
        // 处理镜头和后处理问题
        FixCameraAndPostProcessingIssues(mainScene);
        
        if (enableDebugLog)
        {
            Debug.Log($"GameStartManager: Main场景位置设置完成，共处理 {rootObjects.Length} 个根对象");
        }
        
        // 触发Main场景加载完成事件
        OnMainSceneLoaded?.Invoke();
        
        // 通知SceneLayerManager更新层级（如果存在）
        if (SceneLayerManager.Instance != null)
        {
            SceneLayerManager.Instance.UpdateAllObjectLayers();
            if (enableDebugLog)
            {
                Debug.Log("GameStartManager: 已通知SceneLayerManager更新层级");
            }
        }
    }
    
    /// <summary>
    /// 修复镜头和后处理问题
    /// </summary>
    private void FixCameraAndPostProcessingIssues(Scene mainScene)
    {
        // 1. 禁用Cover场景的镜头，使用Main场景的镜头
        DisableCoverSceneCamera();
        
        // 2. 调整Main场景镜头位置
        AdjustMainSceneCameraPositions(mainScene);
        
        // 3. 启用Main场景的后处理
        EnableMainScenePostProcessing(mainScene);
        
        // 4. 处理AudioListener问题
        FixAudioListenerIssues(mainScene);
    }
    
    /// <summary>
    /// 禁用Cover场景的镜头
    /// </summary>
    private void DisableCoverSceneCamera()
    {
        // 使用缓存的Cover场景主镜头
        if (coverMainCamera != null)
        {
            coverMainCamera.enabled = false;
            
            if (enableDebugLog)
            {
                Debug.Log($"GameStartManager: 禁用Cover场景的主镜头: {coverMainCamera.name}");
            }
        }
    }
    
    /// <summary>
    /// 调整Main场景镜头位置
    /// </summary>
    private void AdjustMainSceneCameraPositions(Scene mainScene)
    {
        // 缓存Main场景中的所有Camera
        mainSceneCameras = mainScene.GetRootGameObjects()
            .SelectMany(obj => obj.GetComponentsInChildren<Camera>())
            .ToArray();
        
        foreach (Camera camera in mainSceneCameras)
        {
            // 调整镜头位置，使其能够看到移动到Y=-1000的Main场景内容
            Vector3 originalPosition = camera.transform.position;
            Vector3 adjustedPosition = new Vector3(
                originalPosition.x,
                originalPosition.y + mainSceneOffset.y, // 调整Y位置
                originalPosition.z
            );
            
            camera.transform.position = adjustedPosition;
            
            if (enableDebugLog)
            {
                Debug.Log($"GameStartManager: 调整Main场景镜头位置: {camera.name} {originalPosition} -> {adjustedPosition}");
            }
        }
        
        // 验证镜头位置是否正确
        ValidateCameraPositions(mainSceneCameras);
    }
    
    /// <summary>
    /// 验证镜头位置是否正确
    /// </summary>
    private void ValidateCameraPositions(Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            Vector3 currentPosition = camera.transform.position;
            
            // 检查Main Camera位置
            if (camera.name == "Main Camera")
            {
                Vector3 expectedPosition = new Vector3(0, mainSceneOffset.y, -10);
                if (Vector3.Distance(currentPosition, expectedPosition) > 0.1f)
                {
                    Debug.LogWarning($"GameStartManager: Main Camera位置不正确！当前: {currentPosition}, 期望: {expectedPosition}");
                    camera.transform.position = expectedPosition;
                }
            }
            // 检查PreviewCamera位置
            else if (camera.name == "PreviewCamera")
            {
                Vector3 expectedPosition = new Vector3(-20.287f, -0.88f + mainSceneOffset.y, -10);
                if (Vector3.Distance(currentPosition, expectedPosition) > 0.1f)
                {
                    Debug.LogWarning($"GameStartManager: PreviewCamera位置不正确！当前: {currentPosition}, 期望: {expectedPosition}");
                    camera.transform.position = expectedPosition;
                }
            }
        }
    }
    
    /// <summary>
    /// 启用Main场景的后处理
    /// </summary>
    private void EnableMainScenePostProcessing(Scene mainScene)
    {
        // 使用缓存的Main场景Camera数组
        if (mainSceneCameras != null)
        {
            foreach (Camera camera in mainSceneCameras)
            {
                // 启用后处理渲染
                var universalAdditionalCameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (universalAdditionalCameraData != null)
                {
                    universalAdditionalCameraData.renderPostProcessing = true;
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"GameStartManager: 启用Camera {camera.name} 的后处理渲染");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 修复AudioListener问题
    /// </summary>
    private void FixAudioListenerIssues(Scene mainScene)
    {
        // 缓存Main场景中的AudioListener
        mainSceneAudioListeners = mainScene.GetRootGameObjects()
            .SelectMany(obj => obj.GetComponentsInChildren<AudioListener>())
            .ToArray();
        
        // 如果Main场景没有AudioListener，保持Cover场景的AudioListener启用
        if (mainSceneAudioListeners.Length == 0)
        {
            if (enableDebugLog)
            {
                Debug.Log("GameStartManager: Main场景没有AudioListener，保持Cover场景的AudioListener启用");
            }
        }
        else
        {
            // 如果Main场景有AudioListener，禁用Cover场景的AudioListener
            if (coverAudioListener != null && !mainSceneAudioListeners.Contains(coverAudioListener))
            {
                coverAudioListener.enabled = false;
                
                if (enableDebugLog)
                {
                    Debug.Log("GameStartManager: 禁用Cover场景的AudioListener，使用Main场景的AudioListener");
                }
            }
        }
    }
    
    /// <summary>
    /// MMF Player动画完成回调
    /// </summary>
    private void OnMMFPlayerComplete()
    {
        if (enableDebugLog)
        {
            Debug.Log("GameStartManager: MMF Player动画播放完成");
        }
        
        // 触发动画完成事件
        OnAnimationComplete?.Invoke();
        
        // 可以在这里添加其他逻辑：
        // - 隐藏Cover场景的UI
        // - 激活Main场景
        // - 开始游戏逻辑等
    }
    
    /// <summary>
    /// 手动设置Main场景位置（供外部调用）
    /// </summary>
    public void SetMainScenePosition(Vector3 position)
    {
        Scene mainScene = SceneManager.GetSceneByName(mainSceneName);
        if (mainScene.isLoaded)
        {
            GameObject[] rootObjects = mainScene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                rootObj.transform.position = position;
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"GameStartManager: 手动设置Main场景位置: {position}");
            }
        }
        else if (enableDebugLog)
        {
            Debug.LogWarning($"GameStartManager: Main场景 {mainSceneName} 尚未加载，无法设置位置");
        }
    }
    
    /// <summary>
    /// 检查Main场景是否已加载
    /// </summary>
    public bool IsMainSceneLoaded()
    {
        Scene mainScene = SceneManager.GetSceneByName(mainSceneName);
        return mainScene.isLoaded;
    }
    
    /// <summary>
    /// 获取Main场景
    /// </summary>
    public Scene GetMainScene()
    {
        return SceneManager.GetSceneByName(mainSceneName);
    }
    
    /// <summary>
    /// 在编辑器中验证配置
    /// </summary>
    private void OnValidate()
    {
        if (llsManager == null)
        {
            llsManager = FindFirstObjectByType<LoadingScreenManager>();
        }
    }
}
