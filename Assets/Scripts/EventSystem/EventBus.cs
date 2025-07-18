using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件总线系统，用于管理游戏内事件的订阅与发布
/// </summary>
public class EventBus : MonoBehaviour
{
    private static EventBus _instance;
    public static EventBus Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("EventBus");
                _instance = go.AddComponent<EventBus>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Dictionary<Type, List<Delegate>> _eventSubscribers = new Dictionary<Type, List<Delegate>>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("重复的EventBus实例，正在销毁新的实例");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("EventBus初始化成功");
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="handler">事件处理函数</param>
    public void Subscribe<T>(Action<T> handler) where T : EventArgs
    {
        if (handler == null)
        {
            Debug.LogError("尝试订阅空事件处理函数");
            return;
        }

        Type eventType = typeof(T);
        
        if (!_eventSubscribers.ContainsKey(eventType))
        {
            _eventSubscribers[eventType] = new List<Delegate>();
        }
        
        _eventSubscribers[eventType].Add(handler);
        
        Debug.Log($"新增事件订阅: {eventType.Name}");
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="handler">事件处理函数</param>
    public void Unsubscribe<T>(Action<T> handler) where T : EventArgs
    {
        Type eventType = typeof(T);
        
        if (!_eventSubscribers.ContainsKey(eventType))
        {
            Debug.LogWarning($"尝试取消未注册的事件订阅: {eventType.Name}");
            return;
        }
        
        _eventSubscribers[eventType].Remove(handler);
        
        Debug.Log($"移除事件订阅: {eventType.Name}");
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="sender">事件发送者</param>
    /// <param name="eventArgs">事件参数</param>
    public void Publish<T>(object sender, T eventArgs) where T : EventArgs
    {
        Type eventType = typeof(T);
        
        if (!_eventSubscribers.ContainsKey(eventType) || _eventSubscribers[eventType].Count == 0)
        {
            Debug.Log($"发布未订阅的事件: {eventType.Name}");
            return;
        }
        
        Debug.Log($"发布事件: {eventType.Name}");
        
        foreach (var handler in _eventSubscribers[eventType])
        {
            try
            {
                handler.DynamicInvoke(sender, eventArgs);
            }
            catch (Exception e)
            {
                Debug.LogError($"事件处理出错: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}