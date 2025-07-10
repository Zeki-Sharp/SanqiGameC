using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CreatePrefab : MonoBehaviour
{
    [Header("地图配置")]
    [SerializeField] private int mapWidth = 20;
    [SerializeField] private int mapHeight = 15;
    [SerializeField] private float cellSize = 1f;
    
    [Header("地图状态")]
    [SerializeField] private bool[,] occupiedCells; // 记录哪些格子被占用
    [SerializeField] private Dictionary<Vector2Int, Block> placedBlocks = new Dictionary<Vector2Int, Block>();

    
    [Header("Tilemap可视化")]
    public Tilemap tilemap; // 拖拽赋值
    public TileBase groundTile; // 拖拽你的grass瓦片
    
    [SerializeField] private GameObject prefabShowArea;
    
    // 公共属性
    public int MapWidth => mapWidth;
    public int MapHeight => mapHeight;
    public float CellSize => cellSize;
    private void Awake()
    {
        InitializeMap();
        Debug.Log("(0,0)"+tilemap.GetCellCenterWorld(new Vector3Int(4,0, 0)));
        Debug.Log("(0,1)"+tilemap.GetCellCenterWorld(new Vector3Int(4,1, 0)));
        Debug.Log("(1,0)"+tilemap.GetCellCenterWorld(new Vector3Int(3,0, 0)));
        Debug.Log("(1,1)"+tilemap.GetCellCenterWorld(new Vector3Int(3,1, 0)));
    }
    /// <summary>
    /// 初始化地图
    /// </summary>
    private void InitializeMap()
    {
        occupiedCells = new bool[mapWidth, mapHeight];
        placedBlocks.Clear();
        
        // 清空Tilemap
        if (tilemap != null)
            tilemap.ClearAllTiles();

        // 填充Tilemap
        if (tilemap != null && groundTile != null)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
                }
            }
        }
        Debug.Log($"地图初始化完成: {mapWidth}x{mapHeight}, 格子大小: {cellSize}");
    }
    public void CreateBlock(GameObject blockPrefab,List<TowerData> towerDatas,Vector2Int[] position,BlockGenerationConfig config)
    { 
        GameObject blockObj = Instantiate(blockPrefab,prefabShowArea.transform);
        Block block = blockObj.GetComponent<Block>(); 
        // 计算几何中心
        float centerX = 0, centerY = 0;
        foreach (var pos in position)
        {
            centerX += pos.x;
            centerY += pos.y;
        }
        centerX /= position.Length;
        centerY /= position.Length;

        // 取最近的整数坐标
        Vector2Int center = new Vector2Int(Mathf.RoundToInt(centerX), Mathf.RoundToInt(centerY));
        Debug.Log($"几何中心: {center}");
        block.transform.position = tilemap.GetCellCenterLocal(new Vector3Int(center.x, center.y, 0));
        block.Init(config);
        block.GenerateTowers(position,towerDatas.ToArray(),tilemap);
       
        GameMap.instance.PlaceBlock(position[0], block);
        
       
    }
}
