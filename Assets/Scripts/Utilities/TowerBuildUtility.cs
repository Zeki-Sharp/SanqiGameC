using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TowerBuildUtility
{
    /// <summary>
    /// 统一的塔生成方法，支持预览和实际建造
    /// </summary>
    /// <param name="parent">父物体</param>
    /// <param name="towerPrefab">塔预制体</param>
    /// <param name="cell">目标cell坐标</param>
    /// <param name="tilemap">Tilemap引用</param>
    /// <param name="towerData">塔数据</param>
    /// <param name="isPreview">是否为预览塔</param>
    /// <param name="color">塔渲染颜色</param>
    /// <param name="hasCheck">是否需要碰撞检测</param>
    /// <returns>生成的Tower组件</returns>
    public static Tower GenerateTower(
        Transform parent,
        GameObject towerPrefab,
        Vector3Int cell,
        Tilemap tilemap,
        TowerData towerData,
        bool isPreview = false,
        Color? color = null,
        bool hasCheck = false)
    {
        if (towerPrefab == null)
        {
            Debug.LogError("塔预制体未找到");
            return null;
        }
        GameObject towerObj = Object.Instantiate(towerPrefab, parent);
        towerObj.SetActive(true);
        towerObj.tag = isPreview ? "PreviewTower" : "Tower";
        Vector3 worldPos = tilemap != null ? tilemap.GetCellCenterWorld(cell) : new Vector3(cell.x, cell.y, 0);
        towerObj.transform.position = worldPos;
        Tower towerComponent = towerObj.GetComponent<Tower>();
        if (towerComponent != null)
        {
            // 预览塔和展示区域塔都不进行游戏逻辑
            bool isShowArea = isPreview;
            towerComponent.Initialize(towerData, cell, hasCheck, isShowArea);
            const int BaseOrder = 1000;
            const int VerticalOffsetMultiplier = 10;
            int verticalOffset = Mathf.RoundToInt(-worldPos.y * VerticalOffsetMultiplier);
            int finalOrder = BaseOrder + verticalOffset;
            towerComponent.SetOrder(finalOrder);
        }
        // 设置颜色
        SpriteRenderer[] renderers = towerObj.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            foreach (var sr in renderers)
            {
                sr.color = color ?? Color.white;
                sr.enabled = false;
                sr.enabled = true;
            }
        }
        return towerComponent;
    }
    /// <summary>
    /// 将斜45度视角下的世界坐标转换为方向向量
    /// </summary>
    /// <param name="position">世界坐标位置</param>
    /// <returns>对应的方向向量</returns>
    public static Vector2 PositionToDirection(Vector2 position)
    {
        // 根据示例数据反推的缩放系数（需根据实际场景调整）
        const float scaleX = 1.22f / 6.03f;
        const float scaleY = 2.51f / 1.93f;
    
        // 应用缩放转换
        return new Vector2(position.x * scaleX, position.y * scaleY);
    }

} 