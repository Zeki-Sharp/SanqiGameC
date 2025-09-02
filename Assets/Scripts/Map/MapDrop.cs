using System.Collections;
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
    [Min(0.01f)] public float defaultDropDuration = 0.5f; // 下落速度，值越小越快
    public Ease dropEase = Ease.OutBounce;
    
    [Header("Bounce Settings")]
    [Min(0f)] public float bounceHeight = 0.2f; // 弹跳高度
    [Min(0.01f)] public float bounceDuration = 0.3f; // 弹跳持续时间
    
    [Header("Random Drop Settings")]
    [Min(0f)] public float maxRandomDelay = 0.8f; // 最大随机延迟时间

    [Header("Callbacks")]
    [Tooltip("所有下落动画播放完毕后触发（可在 Inspector 里把 ResetCenterTower 之类的方法拖进来）")]
    public UnityEvent onAllDropsCompleted;

    // 运行时
    private readonly Queue<DropTile> _queue = new Queue<DropTile>();
    private bool _isDropping; // 重入保护
    private int _activeDrops = 0; // 当前活跃的掉落动画数量

    /// <summary>
    /// 入队一个下落任务（可多次调用并发入队）
    /// </summary>
    public void AddDropTile(TileBase fallingTile, Vector3Int cell, float dropDuration = -1f)
    {
        if (tilemap == null || fallingTile == null) return;

        float dur = dropDuration > 0f ? dropDuration : defaultDropDuration;
        _queue.Enqueue(new DropTile(fallingTile, cell, dur));
    }

    /// <summary>
    /// 开始播放所有队列中的下落动画（所有方块同时开始，但有随机延迟）
    /// </summary>
    public void StartAllDrops()
    {
        if (_isDropping || _queue.Count == 0) return;

        _isDropping = true;
        _activeDrops = _queue.Count;
        
        // 为每个方块设置随机延迟开始时间
        var drops = _queue.ToArray();
        _queue.Clear();
        
        foreach (var drop in drops)
        {
            float randomDelay = Random.Range(0f, maxRandomDelay);
            StartCoroutine(PlayDropWithRandomDelay(drop, randomDelay));
        }
    }

    /// <summary>
    /// 带随机延迟播放单个下落动画
    /// </summary>
    private IEnumerator PlayDropWithRandomDelay(DropTile drop, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
            
        PlaySingleDrop(drop);
    }

    /// <summary>
    /// 播放单个下落动画
    /// </summary>
    private void PlaySingleDrop(DropTile drop)
    {
        // 若这个格子已被占用：这里选择直接替换（或你也可以选择跳过）
        if (tilemap.HasTile(drop.cell))
        {
            tilemap.SetTile(drop.cell, drop.tile);
            OnDropCompleted();
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

        // 开始下落动画
        ghost.transform
            .DOMove(endWorld, drop.duration)
            .SetEase(Ease.OutQuad) // 使用更平滑的下落曲线
            .OnComplete(() =>
            {
                // 下落完成后，开始弹跳动画
                StartBounceAnimation(ghost, endWorld, drop.cell, drop.tile);
            });
    }

    /// <summary>
    /// 开始弹跳动画
    /// </summary>
    private void StartBounceAnimation(GameObject ghost, Vector3 endWorld, Vector3Int cell, TileBase tile)
    {
        // 计算弹跳的顶点位置
        Vector3 bounceTop = endWorld + Vector3.up * bounceHeight;
        
        // 创建弹跳序列动画
        Sequence bounceSeq = DOTween.Sequence();
        
        // 向上弹跳
        bounceSeq.Append(ghost.transform.DOMove(bounceTop, bounceDuration * 0.4f)
            .SetEase(Ease.OutQuad));
        
        // 向下落回
        bounceSeq.Append(ghost.transform.DOMove(endWorld, bounceDuration * 0.6f)
            .SetEase(Ease.InQuad));
        
        // 弹跳完成后的回调
        bounceSeq.OnComplete(() =>
        {
            tilemap.SetTile(cell, tile);
            Destroy(ghost);
            OnDropCompleted();
        });
    }

    /// <summary>
    /// 当单个掉落动画完成时调用
    /// </summary>
    private void OnDropCompleted()
    {
        _activeDrops--;
        
        // 所有动画都完成了
        if (_activeDrops <= 0)
        {
            _isDropping = false;
            
            // 先调用本类方法（你把重置居中塔的逻辑写进去）
            ResetCenterTower();
            
            // 再触发可视化事件（可在 Inspector 绑定到其他对象/方法）
            onAllDropsCompleted?.Invoke();
        }
    }

    /// <summary>
    /// 逐个播放队列中的下落动画（保留原有方法以兼容其他调用）
    /// </summary>
    private void PlayNext()
    {
        // 队列空了，收尾（此时意味着"全部完成"）
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
        PlaySingleDrop(drop);
        
        // 继续处理后续队列
        PlayNext();
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
        // 通知UI面板刷新中心塔血量显示
        var uiManager = GameManager.Instance?.GetSystem<UIManager>();
        if (uiManager != null)
        {
            var mainTowerHealthPanel = uiManager.GetMainTowerHealthPanel();
            if (mainTowerHealthPanel != null)
            {
                // 延迟一点时间确保中心塔完全生成
                StartCoroutine(DelayedRefreshMainTowerUI(mainTowerHealthPanel));
            }
        }
    }
    
    /// <summary>
    /// 延迟刷新主塔UI
    /// </summary>
    private System.Collections.IEnumerator DelayedRefreshMainTowerUI(MainTowerHealthPanel panel)
    {
        // 等待确保中心塔完全生成和初始化
        yield return new WaitForSeconds(0.1f);
        
        // 显示主塔血量面板（面板会在OnShow时自动初始化）
        panel.Show();
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
