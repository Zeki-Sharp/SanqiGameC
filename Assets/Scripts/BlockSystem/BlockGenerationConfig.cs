using System;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockGenerationConfig", menuName = "Scriptable Objects/BlockGenerationConfig"), Serializable]
public class BlockGenerationConfig : ScriptableObject
{
    [SerializeField]
    private string shapeName;
    
    // 使用一维数组保存 4x4 网格数据
    private bool[] blockGrid = new bool[16];

    [ShowInInspector]
    [PropertySpace(5)]
    [TableMatrix(HorizontalTitle = "X", VerticalTitle = "Y")]
    public bool[,] BlockGrid
    {
        get
        {
            // 从一维数组构建二维数组用于显示
            bool[,] grid = new bool[4, 4];
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    grid[y, x] = blockGrid[y * 4 + x];
                }
            }
            return grid;
        }
        set
        {
            if (value == null || value.GetLength(0) != 4 || value.GetLength(1) != 4)
            {
                Debug.LogError("BlockGrid 必须是一个 4x4 的二维数组");
                return;
            }

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    blockGrid[y * 4 + x] = value[y, x];
                }
            }
        }
    }

    [Button("保存")]
    public void Save()
    {
        cellCount = GetCellCount(out int count);
        coordinates = GetCellCoords(count);
    }

    private int cellCount;
    private Vector2Int[] coordinates;

    public Vector2Int[] Coordinates => coordinates;
    public int CellCount => cellCount;

    private void OnValidate()
    {
        shapeName = name;

#if UNITY_EDITOR
        // 调试信息只在编辑器下启用
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                Debug.Log(BlockGrid[y, x]);
            }
        }
#endif

        Save();
    }

    public int GetCellCount(out int count)
    {
        count = 0;
        for (int i = 0; i < 16; i++)
        {
            if (blockGrid[i])
            {
                count++;
            }
        }
        return count;
    }

    public Vector2Int[] GetCellCoords(int cellCount)
    {
        Vector2Int[] coords = new Vector2Int[cellCount];
        int index = 0;
        for (int i = 0; i < 16; i++)
        {
            if (blockGrid[i])
            {
                int x = i % 4;
                int y = i / 4;
                coords[index] = new Vector2Int(x, y);
                index++;
            }
        }
        return coords;
    }
}
