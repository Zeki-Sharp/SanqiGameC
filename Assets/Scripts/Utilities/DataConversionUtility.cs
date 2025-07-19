using System;
using UnityEngine;

/// <summary>
/// 数据转换工具类
/// 
/// 提供类型转换和数据格式化功能，专注于安全的数据处理。
/// 包含安全的类型转换、枚举操作、数据验证等功能。
/// 
/// 主要功能：
/// - 安全转换：安全的数值、布尔值、枚举转换
/// - 枚举操作：获取枚举值、名称、验证
/// - 数据验证：数值有效性检查
/// - 类型转换：字符串到各种类型的转换
/// 
/// 使用示例：
/// ```csharp
/// // 安全转换字符串到整数
/// int value = DataConversionUtility.SafeParseInt("123", 0);
/// 
/// // 获取枚举的所有值
/// T[] enumValues = DataConversionUtility.GetEnumValues<T>();
/// 
/// // 验证数值有效性
/// bool isValid = DataConversionUtility.IsValidNumber("123.45");
/// ```
/// </summary>
public static class DataConversionUtility
{
    #region 类型转换

    /// <summary>
    /// 安全地将字符串转换为整数
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <param name="defaultValue">转换失败时的默认值</param>
    /// <returns>转换后的整数</returns>
    public static int SafeParseInt(string value, int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        if (int.TryParse(value, out int result))
            return result;

        Debug.LogWarning($"无法将字符串 '{value}' 转换为整数，使用默认值 {defaultValue}");
        return defaultValue;
    }

    /// <summary>
    /// 安全地将字符串转换为浮点数
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <param name="defaultValue">转换失败时的默认值</param>
    /// <returns>转换后的浮点数</returns>
    public static float SafeParseFloat(string value, float defaultValue = 0f)
    {
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        if (float.TryParse(value, out float result))
            return result;

        Debug.LogWarning($"无法将字符串 '{value}' 转换为浮点数，使用默认值 {defaultValue}");
        return defaultValue;
    }

    /// <summary>
    /// 安全地将字符串转换为布尔值
    /// </summary>
    /// <param name="value">要转换的字符串</param>
    /// <param name="defaultValue">转换失败时的默认值</param>
    /// <returns>转换后的布尔值</returns>
    public static bool SafeParseBool(string value, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        if (bool.TryParse(value, out bool result))
            return result;

        // 支持额外的布尔值表示
        string lowerValue = value.ToLower();
        if (lowerValue == "1" || lowerValue == "true" || lowerValue == "yes" || lowerValue == "on")
            return true;
        if (lowerValue == "0" || lowerValue == "false" || lowerValue == "no" || lowerValue == "off")
            return false;

        Debug.LogWarning($"无法将字符串 '{value}' 转换为布尔值，使用默认值 {defaultValue}");
        return defaultValue;
    }

    #endregion

    #region 枚举转换

    /// <summary>
    /// 安全地将字符串转换为枚举值
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <param name="value">要转换的字符串</param>
    /// <param name="defaultValue">转换失败时的默认值</param>
    /// <returns>转换后的枚举值</returns>
    public static T SafeParseEnum<T>(string value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        if (Enum.TryParse(value, true, out T result))
            return result;

        Debug.LogWarning($"无法将字符串 '{value}' 转换为枚举 {typeof(T).Name}，使用默认值 {defaultValue}");
        return defaultValue;
    }

    /// <summary>
    /// 获取枚举的所有值
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <returns>枚举值数组</returns>
    public static T[] GetEnumValues<T>() where T : struct, Enum
    {
        return (T[])Enum.GetValues(typeof(T));
    }

    /// <summary>
    /// 获取枚举的所有名称
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <returns>枚举名称数组</returns>
    public static string[] GetEnumNames<T>() where T : struct, Enum
    {
        return Enum.GetNames(typeof(T));
    }

    #endregion

    #region 数据验证

    /// <summary>
    /// 验证字符串是否为有效的数字
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <returns>是否为有效数字</returns>
    public static bool IsValidNumber(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return float.TryParse(value, out _);
    }

    /// <summary>
    /// 验证字符串是否为有效的整数
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <returns>是否为有效整数</returns>
    public static bool IsValidInteger(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return int.TryParse(value, out _);
    }

    #endregion
} 