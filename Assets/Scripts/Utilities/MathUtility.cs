using UnityEngine;

/// <summary>
/// 数值计算工具类
/// 
/// 提供数值计算和转换功能，专注于数学运算和数值处理。
/// 包含基础数学运算、数值验证、随机数生成等功能。
/// 
/// 主要功能：
/// - 数值计算：基础数学运算、距离计算
/// - 数值验证：范围检查、有效性验证
/// - 随机生成：随机整数、随机浮点数
/// - 数值转换：类型转换、格式化
/// 
/// 使用示例：
/// ```csharp
/// // 计算两点距离
/// float distance = MathUtility.Distance(pos1, pos2);
/// 
/// // 检查数值是否在范围内
/// bool inRange = MathUtility.IsValueInRange(value, min, max);
/// 
/// // 生成随机数
/// int randomInt = MathUtility.RandomInt(min, max);
/// ```
/// </summary>
public static class MathUtility
{
    #region 数值转换

    /// <summary>
    /// 根据值类型获取实际值
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="valueType">值类型</param>
    /// <returns>转换后的值</returns>
    public static float GetValue(float value, ValueType valueType)
    {
        if (valueType == ValueType.Percent)
            return value / 100f;
        return value;
    }

    /// <summary>
    /// 根据值类型计算乘法结果
    /// </summary>
    /// <param name="value">基础值</param>
    /// <param name="multiplier">乘数</param>
    /// <param name="valueType">值类型</param>
    /// <returns>计算结果</returns>
    public static float MultiplyValue(float value, float multiplier, ValueType valueType)
    {
        // 如果是百分比类型，则按百分比增加计算：value * (1 + multiplier/100)
        if (valueType == ValueType.Percent)
            return value * (1 + multiplier / 100f);
        // 否则作为绝对值直接相加
        return value + multiplier;
    }

    #endregion

    #region 数值验证

    /// <summary>
    /// 验证数值是否在有效范围内
    /// </summary>
    /// <param name="value">要验证的数值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns>是否在有效范围内</returns>
    public static bool IsValueInRange(float value, float min, float max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// 验证数值是否为正数
    /// </summary>
    /// <param name="value">要验证的数值</param>
    /// <returns>是否为正数</returns>
    public static bool IsPositive(float value)
    {
        return value > 0;
    }

    #endregion

    #region 数学计算辅助

    /// <summary>
    /// 计算两点之间的距离
    /// </summary>
    /// <param name="point1">点1</param>
    /// <param name="point2">点2</param>
    /// <returns>距离</returns>
    public static float Distance(Vector2 point1, Vector2 point2)
    {
        return Vector2.Distance(point1, point2);
    }

    /// <summary>
    /// 计算两点之间的距离
    /// </summary>
    /// <param name="point1">点1</param>
    /// <param name="point2">点2</param>
    /// <returns>距离</returns>
    public static float Distance(Vector3 point1, Vector3 point2)
    {
        return Vector3.Distance(point1, point2);
    }

    /// <summary>
    /// 计算两点之间的平方距离（性能优化版本）
    /// </summary>
    /// <param name="point1">点1</param>
    /// <param name="point2">点2</param>
    /// <returns>平方距离</returns>
    public static float SqrDistance(Vector2 point1, Vector2 point2)
    {
        return (point1 - point2).sqrMagnitude;
    }

    /// <summary>
    /// 计算两点之间的平方距离（性能优化版本）
    /// </summary>
    /// <param name="point1">点1</param>
    /// <param name="point2">点2</param>
    /// <returns>平方距离</returns>
    public static float SqrDistance(Vector3 point1, Vector3 point2)
    {
        return (point1 - point2).sqrMagnitude;
    }

    #endregion

    #region 随机数生成

    /// <summary>
    /// 生成指定范围内的随机整数
    /// </summary>
    /// <param name="min">最小值（包含）</param>
    /// <param name="max">最大值（包含）</param>
    /// <returns>随机整数</returns>
    public static int RandomInt(int min, int max)
    {
        return Random.Range(min, max + 1);
    }

    /// <summary>
    /// 生成指定范围内的随机浮点数
    /// </summary>
    /// <param name="min">最小值（包含）</param>
    /// <param name="max">最大值（包含）</param>
    /// <returns>随机浮点数</returns>
    public static float RandomFloat(float min, float max)
    {
        return Random.Range(min, max);
    }

    #endregion
} 