using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class MapDrop : MonoBehaviour
{
    [Header("Refs")]
    public Tilemap tilemap;
    public Camera cam; // 可不绑，自动取 Camera.main

    [Header("Default")]
    [Min(0.01f)] public float defaultDropDuration = 0.7f;
    public Ease dropEase = Ease.OutBounce;

    [Header("Callbacks")]
    [Tooltip("所有下落动画播放完毕后触发（可在 Inspector 里把 ResetCenterTower 之类的方法拖进来）")]
    public UnityEvent onAllDropsCompleted;

    // 运行时
    private readonly Queue<DropTile> _queue = new Queue<DropTile>();
    private bool _isDropping; // 重入保护

    /// <summary>
    /// 入队一个下落任务（可多次调用并发入队）
    /// </summary>
    public void AddDropTile(TileBase fallingTile, Vector3Int cell, float dropDuration = -1f)
    {
        if (tilemap == null || fallingTile == null) return;

        float dur = dropDuration > 0f ? dropDuration : defaultDropDuration;
        _queue.Enqueue(new DropTile(fallingTile, cell, dur));

        // 若当前未在播放，启动之
        if (!_isDropping)
            PlayNext();
    }

    /// <summary>
    /// 逐个播放队列中的下落动画
    /// </summary>
    private void PlayNext()
    {
        // 队列空了，收尾（此时意味着“全部完成”）
        if (_queue.Count == 0)
        {
            _isDropping = false;

            // 先调用本类方法（你把重置居中塔的逻辑写进去）
            ResetCenterTower();

            // 再触发可视化事件（可在 Inspector 绑定到其他对象/方法）
            onAllDropsCompleted?.Invoke();
            return;
        }

        _isDropping = true;

        var drop = _queue.Dequeue();

        // 若这个格子已被占用：这里选择直接替换（或你也可以选择跳过）
        if (tilemap.HasTile(drop.cell))
        {
            tilemap.SetTile(drop.cell, drop.tile);
            // 继续处理后续队列
            PlayNext();
            return;
        }

        // 计算起点/终点
        if (cam == null) cam = Camera.main;
        Vector3 endWorld = tilemap.GetCellCenterWorld(drop.cell);
        Vector3 startWorld = ComputeSpawnAbove(cam, endWorld, 1.1f); // 从视口上方 10% 生成

        // 准备一个幽灵渲染体
        var ghost = new GameObject("DropTile_Ghost");
        var sr = ghost.AddComponent<SpriteRenderer>();
        ApplySpriteFromTile(drop.tile, drop.cell, tilemap, sr);

        // sorting 与 tilemap 一致并在其上方一层
        var tmr = tilemap.GetComponent<TilemapRenderer>();
        if (tmr != null)
        {
            sr.sortingLayerID = tmr.sortingLayerID;
            sr.sortingLayerName = tmr.sortingLayerName;
            sr.sortingOrder = tmr.sortingOrder + 1;
        }

        ghost.transform.position = startWorld;

        // 开始补间
        ghost.transform
            .DOMove(endWorld, drop.duration)
            .SetEase(dropEase)
            .OnComplete(() =>
            {
                tilemap.SetTile(drop.cell, drop.tile);
                Destroy(ghost);
                // 继续处理后续队列
                PlayNext();
            });
    }

    /// <summary>
    /// 在目标点正上方且仍在相机可见上边之外生成。适配透视/正交。
    /// </summary>
    private static Vector3 ComputeSpawnAbove(Camera cam, Vector3 targetWorld, float yViewport = 1.1f)
    {
        Vector3 vp = cam.WorldToViewportPoint(targetWorld); // (vx, vy, depth)
        Vector3 spawnVp = new Vector3(vp.x, yViewport, vp.z);
        return cam.ViewportToWorldPoint(spawnVp);
    }

    /// <summary>
    /// 尝试从 TileBase 获取真实 sprite（兼容 Tile / RuleTile / 自定义 TileBase）
    /// </summary>
    private static void ApplySpriteFromTile(TileBase tile, Vector3Int pos, Tilemap map, SpriteRenderer sr)
    {
        var data = new TileData();
#if UNITY_2021_3_OR_NEWER
        tile.GetTileData(pos, map, ref data);
#else
        if (tile is Tile t1)
        {
            sr.sprite = t1.sprite;
            return;
        }
#endif
        if (data.sprite != null)
        {
            sr.sprite = data.sprite;
            return;
        }

        if (tile is Tile t)
        {
            sr.sprite = t.sprite;
            return;
        }

        // 兜底：只在空格子里临时放上去取一次 sprite
        if (!map.HasTile(pos))
        {
            map.SetTile(pos, tile);
            var s = map.GetSprite(pos);
            map.SetTile(pos, null);
            sr.sprite = s;
        }
    }

    // ====== 当所有动画播放完毕后要执行的方法 ======
    // 你可以把 ResetCenterTower 的具体逻辑写在这里；
    // 或者把逻辑放到别的脚本里，通过 onAllDropsCompleted 在 Inspector 里绑定。
    private void ResetCenterTower()
    {
    
    }

    // ====== 数据结构 ======
    private struct DropTile
    {
        public readonly TileBase tile;
        public readonly Vector3Int cell;
        public readonly float duration;

        public DropTile(TileBase tile, Vector3Int cell, float duration)
        {
            this.tile = tile;
            this.cell = cell;
            this.duration = duration;
        }
    }
}
