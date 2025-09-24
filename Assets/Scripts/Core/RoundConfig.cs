using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Round配置 - ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New Round Config", menuName = "Tower Defense/Round Config")]
public class RoundConfig : ScriptableObject
{
    [Header("Round信息")]
    public int roundNumber;
    public List<Wave> waves = new List<Wave>();

    [Header("奖励")]
    public int rewardMoney = 100;
}
