using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI交互工具类
/// 
/// 提供UI交互相关功能，专注于用户界面的事件处理和组件查找。
/// 包含鼠标事件检测、UI组件查找、事件系统操作等功能。
/// 
/// 主要功能：
/// - 事件检测：鼠标悬停、点击检测
/// - 组件查找：按类型查找UI组件
/// - 事件处理：UI事件系统操作
/// - 交互验证：UI交互状态检查
/// 
/// 使用示例：
/// ```csharp
/// // 检查鼠标是否悬停在UI上
/// bool overUI = UIInteractionUtility.IsMouseOverUI();
/// 
/// // 获取鼠标下的第一个游戏对象
/// GameObject obj = UIInteractionUtility.GetFirstPickGameObject(Input.mousePosition);
/// 
/// // 查找子组件
/// Button button = UIInteractionUtility.FindComponentInChildren<Button>(transform);
/// ```
/// </summary>
public static class UIInteractionUtility
{
    #region UI射线检测

    /// <summary>
    /// 获取屏幕坐标点击的第一个UI对象
    /// </summary>
    /// <param name="position">屏幕坐标</param>
    /// <returns>点击的UI对象，如果没有则返回null</returns>
    public static GameObject GetFirstPickGameObject(Vector2 position)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogWarning("当前场景中没有EventSystem");
            return null;
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = position;
        
        // 射线检测UI
        List<RaycastResult> uiRaycastResultCache = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerEventData, uiRaycastResultCache);
        
        if (uiRaycastResultCache.Count > 0)
            return uiRaycastResultCache[0].gameObject;
        
        return null;
    }

    #endregion

    #region UI事件处理

    /// <summary>
    /// 检查指定位置是否在UI元素上
    /// </summary>
    /// <param name="position">屏幕坐标</param>
    /// <returns>是否在UI元素上</returns>
    public static bool IsPointerOverUI(Vector2 position)
    {
        return GetFirstPickGameObject(position) != null;
    }

    /// <summary>
    /// 检查鼠标是否在UI元素上
    /// </summary>
    /// <returns>是否在UI元素上</returns>
    public static bool IsMouseOverUI()
    {
        return IsPointerOverUI(Input.mousePosition);
    }

    #endregion

    #region UI组件查找

    /// <summary>
    /// 在指定对象及其子对象中查找指定类型的组件
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="root">根对象</param>
    /// <returns>找到的组件，如果没有则返回null</returns>
    public static T FindComponentInChildren<T>(GameObject root) where T : Component
    {
        if (root == null)
            return null;

        T component = root.GetComponent<T>();
        if (component != null)
            return component;

        return root.GetComponentInChildren<T>();
    }

    #endregion
} 