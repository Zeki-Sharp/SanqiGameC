using System;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockGenerationConfig", menuName = "Scriptable Objects/BlockGenerationConfig"),Serializable]
public class BlockGenerationConfig : ScriptableObject
{
    [SerializeField]
    private string shapeName;
    [SerializeField]
    private bool[,] blockGrid = new bool[4, 4];

    [ShowInInspector]
    [PropertySpace(5)]
    [TableMatrix(HorizontalTitle = "X", VerticalTitle = "Y")]
    public bool[,] BlockGrid
    {
        get { return blockGrid; }
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
                    blockGrid[y, x] = value[y, x];
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
    // [ShowInInspector]
    private int cellCount;
    // [ShowInInspector]
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
                Debug.Log(blockGrid[y, x]);
            }
        }
#endif

        Save();
    }

    public int GetCellCount(out int count)
    {
        count = 0;
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if (blockGrid[y, x])
                {
                    count++;
                }
            }
        }
        return count;
    }

    public Vector2Int[] GetCellCoords(int cellCount)
    {
        Vector2Int[] coords = new Vector2Int[cellCount];
        int index = 0;
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if (blockGrid[y, x])
                {
                    coords[index] = new Vector2Int(x, y);
                    index++;
                }
            }
        }
        return coords;
    }
}
