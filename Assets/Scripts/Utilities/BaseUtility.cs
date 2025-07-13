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
    /// <returns>中心点坐标</returns>
    public static Vector2Int GetCenter(int width, int height)
    {
        int centerX = Mathf.CeilToInt(width / 2f);
        int centerY = Mathf.CeilToInt(height / 2f);
        return new Vector2Int(centerX, centerY);
    }

    public static Vector3Int GetCenterCell(int width, int height)
    {
        int centerX = Mathf.CeilToInt(width / 2f);
        int centerY = Mathf.CeilToInt(height / 2f);
        return new Vector3Int(centerX, centerY, 0);
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
    

}
