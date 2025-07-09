using UnityEngine;

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


}
