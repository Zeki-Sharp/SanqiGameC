using System.Collections.Generic;
using UnityEngine;

public class TowerBuildUtility  : MonoBehaviour
{
    // TODO:计算塔所用到的坐标，获得塔区域的坐标中心，并旋转
    public static bool[,] GetTowerArea(bool[,] BlockGrid)
    {
        int maxX = 0;
        int maxY = 0;
        for (int i = 0; i < BlockGrid.GetLength(0); i++)
        {
            for (int j = 0; j < BlockGrid.GetLength(1); j++)
            {
                if (BlockGrid[i, j])
                {
                    maxX = Mathf.Max(maxX, i);
                    maxY = Mathf.Max(maxY, j);
                }
            }
        }
        bool[,] towerArea = new bool[maxX+1, maxY+1];
        for (int i = 0; i <= maxX; i++)
        {
            for (int j = 0; j <= maxY; j++)
            {
                towerArea[i, j] = BlockGrid[i, j];
            }
        }
        Debug.Log($"[TowerBuildUtility] 塔区域大小：{maxX+1}x{maxY+1}");
        return towerArea;
    }
}
