using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseUtility
{

    /// <summary>
    /// 计算矩形区域的中心坐标（向上取整，适用于奇数尺寸）
    /// </summary>
    /// <param name="width">矩形宽度</param>
    /// <param name="height">矩形高度</param>
    /// <returns>中心点cell坐标</returns>
        public static Vector3Int GetCenterCell(int width, int height)
    {
        int centerX = Mathf.FloorToInt(width / 2f);
        int centerY = Mathf.FloorToInt(height / 2f);
        return new Vector3Int(centerX, centerY, 0);
    }
    
    // public static Vector3Int GetCenterCell(Vector3 size)
    // {
    //     return new Vector3Int(Mathf.FloorToInt(size.x / 2f), Mathf.FloorToInt(size.y / 2f), 0);
    // }

    /// <summary>
    /// 将世界坐标偏移量转换为cell坐标
    /// </summary>
    /// <param name="offset">世界坐标偏移量</param>
    /// <returns>对应的cell坐标</returns>
    public static Vector3Int WorldOffsetToCell(Vector3 offset)
    {
        // 使用更精确的四舍五入计算
        int x = Mathf.RoundToInt(offset.x);
        int y = Mathf.RoundToInt(offset.y);
        
        // 添加更精确的格子对齐逻辑
        // 如果偏移量接近整数（误差在0.1以内），视为对齐
        if (Mathf.Abs(x - offset.x) < 0.1f) x = (int)offset.x;
        if (Mathf.Abs(y - offset.y) < 0.1f) y = (int)offset.y;
        
        return new Vector3Int(x, y, 0);
    }

    /// <summary>
    /// 点击屏幕坐标
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static GameObject GetFirstPickGameObject(Vector2 position)
    {
        EventSystem eventSystem = EventSystem.current;
        PointerEventData pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = position;
        //射线检测ui
        List<RaycastResult> uiRaycastResultCache = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerEventData, uiRaycastResultCache);
        if (uiRaycastResultCache.Count > 0)
            return uiRaycastResultCache[0].gameObject;
        return null;
    }

    /// <summary>
    /// Vector2Int[] 转 List<Vector3Int>
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static List<Vector3Int> Vector2IntArrayToVector3IntList(Vector2Int[] array)
    {
        List<Vector3Int> list = new List<Vector3Int>();
        foreach (Vector2Int item in array)
        {
            list.Add(new Vector3Int(item.x, item.y, 0));
        }

        return list;
    }

    public static float GetValue(float value, ValueType valueType)
    {
        if (valueType == ValueType.Percent)
            return value / 100f;
        return value;
    }

    public static float MultiplyValue(float value, float multiplier, ValueType valueType)
    {
        // 如果是百分比类型，则按百分比增加计算：value * (1 + multiplier/100)
        if (valueType == ValueType.Percent)
            return value * (1 + multiplier / 100f);
        // 否则作为绝对值直接相加
        return value + multiplier;
    }

  
}