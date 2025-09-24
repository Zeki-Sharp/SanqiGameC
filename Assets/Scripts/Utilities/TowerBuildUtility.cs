using System.Collections.Generic;
using RaycastPro.Casters2D;
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
        Block blockComponent = towerObj.GetComponent<Block>();
        Tower towerComponent = towerObj.GetComponent<Tower>();
        if (towerComponent != null)
        {
            // 预览塔和展示区域塔都不进行游戏逻辑
            towerComponent.Initialize(towerData, cell, hasCheck, isPreview);
            
            // 只有非预览塔才需要设置子弹池
            if (!isPreview)
            {
                var bulletManager = GameManager.Instance?.GetSystem<BulletManager>();
                if (bulletManager != null)
                {
                    var caster = towerComponent.GetComponent<BasicCaster2D>();
                    if (caster != null)
                    {
                        caster.poolManager = bulletManager.GetPoolManager();
                    }
                }
            }
            
            // 设置名称以便调试
            towerObj.name = $"{(isPreview ? "Preview_" : "")}{towerData.TowerName}_{cell.x}_{cell.y}";
            // 注意：塔的层级现在由SceneLayerManager统一管理
            // 不再需要手动设置sortingOrder
        }
        // 设置颜色和可见性
        SpriteRenderer[] renderers = towerObj.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            foreach (var sr in renderers)
            {
                if (sr != null)
                {
                    sr.color = color ?? Color.white;
                    sr.enabled = true;
                }
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