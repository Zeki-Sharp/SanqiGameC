using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildPreviewEventArgs : EventArgs
{
    /// <summary>
    /// 方块在Tilemap中的cell坐标列表
    /// </summary>
    public List<Vector3Int> Positions;
    public BlockGenerationConfig Config;
    public List<TowerData> TowerDatas;
    public Transform Parent;
    public Tilemap Tilemap;

    /// <summary>
    /// 构建预览事件参数
    /// </summary>
    /// <param name="positions">方块在Tilemap中的cell坐标列表</param>
    /// <param name="config">方块生成配置</param>
    /// <param name="towerDatas">塔数据列表</param>
    /// <param name="parent">父级Transform</param>
    /// <param name="tilemap">Tilemap引用</param>
    public BuildPreviewEventArgs(List<Vector3Int> positions, BlockGenerationConfig config, List<TowerData> towerDatas, Transform parent, Tilemap tilemap)
    {
        Positions = positions;
        Config = config;
        TowerDatas = towerDatas;
        Parent = parent;
        Tilemap = tilemap;
    }
} 