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
        block.Init(config);
        block.GenerateTowers(position,towerDatas.ToArray(),tilemap);
       
        GameMap.instance.PlaceBlock(position[0], block);
        
        // block.transform.position = tilemap.CellToWorld(new Vector3Int(position.x, position.y, 0));
    }
}
