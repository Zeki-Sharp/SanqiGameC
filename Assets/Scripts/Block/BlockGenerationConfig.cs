using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "BlockGenerationConfig", menuName = "Tower Defense/Block/BlockGenerationConfig"), Serializable]
public class BlockGenerationConfig : ScriptableObject
{
    [SerializeField]
    private string shapeName;
    
    // 使用一维数组保存 4x4 网格数据
    public bool[] blockGrid = new bool[16];

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
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            Save(); // 新增：每次修改后自动同步
        }
    }

    [Button("保存")]
    public void Save()
    {
        cellCount = GetCellCount(out int count);
        coordinates = GetCellCoords(count);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
    [HideInInspector]
    public int cellCount;
    [HideInInspector]
    public Vector2Int[] coordinates;

    public Vector2Int[] Coordinates => coordinates;
    public int CellCount => cellCount;
    
    public Vector2 offset;

    private void OnValidate()
    {
        shapeName = name;
        Save(); // 新增：每次编辑器变更后自动同步
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
        
    }
    [Button("旋转90°"), PropertySpace(10)]
    public void Rotate()
    {
        // 创建新的旋转后的网格数据
        bool[,] rotated = new bool[4, 4];
        
        // 实现顺时针旋转90度的算法
        for(int y = 0; y < 4; y++)
        {
            for(int x = 0; x < 4; x++)
            {
                rotated[x, 3 - y] = BlockGrid[y, x];
            }
        }
        
        // 更新一维数组
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                blockGrid[y * 4 + x] = rotated[y, x];
            }
        }
        
        // 更新坐标缓存
        // cellCount = GetCellCount(out int count);
        // coordinates = GetCellCoords(count);
        
// #if UNITY_EDITOR
//         if (!Application.isPlaying)
//         {
//             UnityEditor.EditorUtility.SetDirty(this);
//             UnityEditor.AssetDatabase.SaveAssets();
//         }
// #endif
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
        // 验证blockGrid数组长度
        if (blockGrid == null || blockGrid.Length != 16)
        {
            Debug.LogError($"[BlockGenerationConfig] blockGrid数组无效，长度应为16，当前长度：{blockGrid?.Length ?? 0}");
            return new Vector2Int[0];
        }

        // 验证cellCount是否有效
        if (cellCount <= 0)
        {
            Debug.LogError($"[BlockGenerationConfig] 无效的cellCount值：{cellCount}，无法生成坐标数组");
            return new Vector2Int[0];
        }
        //旋转
        int rotateTimes = Random.Range(0, 4);
        Rotate(rotateTimes);

        // 创建坐标数组
        Vector2Int[] coords = new Vector2Int[cellCount];
        int index = 0;
        for (int i = 0; i < 16; i++)
        {
            if (blockGrid[i])
            {
                // 增加越界保护
                if (index >= coords.Length)
                {
                    Debug.LogWarning($"[BlockGenerationConfig] 坐标数组溢出，可能与cellCount不一致（blockGrid中true值比cellCount多）当前i={i}, index={index}, coords.Length={coords.Length}");
                    break;
                }
                int x = i % 4;
                int y = i / 4;
                coords[index] = new Vector2Int(x, y);
                index++;
            }
        }
    
        // 如果实际有效格子数小于coords数组长度，裁剪数组
        if (index < coords.Length)
        {
            System.Array.Resize(ref coords, index);
        }
    
        return coords;
    }
    //旋转几次
    public void Rotate(int times)
    {
        for (int i = 0; i < times; i++)
        {
            Rotate();
        }
    }
}
