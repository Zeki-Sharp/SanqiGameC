using UnityEngine;

/// <summary>
/// 子弹配置 - 统一管理所有子弹类型的配置
/// </summary>
[CreateAssetMenu(fileName = "BulletConfig", menuName = "Tower Defense/Bullet Config")]
public class BulletConfig : ScriptableObject
{
    [Header("基础配置")]
    [SerializeField] private string bulletName = "DefaultBullet";
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private BulletType bulletType = BulletType.Straight;
    [SerializeField] private BulletCategory bulletCategory = BulletCategory.TowerBullet;
    
    [Header("移动配置")]
    [SerializeField] private float defaultSpeed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private BulletMovementType movementType = BulletMovementType.Linear;
    
    [Header("目标配置")]
    [SerializeField] private string[] targetTags = new string[] { "Enemy" };
    [SerializeField] private TargetType targetType = TargetType.Single;
    [SerializeField] private float aoeRadius = 1.5f;
    
    [Header("对象池配置")]
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 100;
    
    [Header("击中特效配置")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Vector3 hitEffectOffset = Vector3.up * 0.1f;
    
    // 公共属性
    public string BulletName => bulletName;
    public GameObject BulletPrefab => bulletPrefab;
    public BulletType BulletType => bulletType;
    public BulletCategory BulletCategory => bulletCategory;
    public float DefaultSpeed => defaultSpeed;
    public float Lifetime => lifetime;
    public BulletMovementType MovementType => movementType;
    public string[] TargetTags => targetTags;
    public TargetType TargetType => targetType;
    public float AoeRadius => aoeRadius;
    public int InitialPoolSize => initialPoolSize;
    public int MaxPoolSize => maxPoolSize;
    
    // 击中特效属性
    public GameObject HitEffectPrefab => hitEffectPrefab;
    public Vector3 HitEffectOffset => hitEffectOffset;
}

/// <summary>
/// 子弹分类枚举
/// </summary>
public enum BulletCategory
{
    TowerBullet,    // 塔子弹
    EnemyBullet,    // 敌人子弹
    NeutralBullet   // 中立子弹
}

/// <summary>
/// 子弹类型枚举
/// </summary>
public enum BulletType
{
    Straight,       // 直线
    Parabola,       // 抛物线
    Homing,         // 追踪
    Spread          // 散射
}

/// <summary>
/// 目标类型枚举
/// </summary>
public enum TargetType
{
    Single,         // 单目标
    Aoe,            // 范围
    Chain           // 链式
}

/// <summary>
/// 子弹移动类型枚举
/// </summary>
public enum BulletMovementType
{
    Linear,         // 线性移动
    Parabolic,      // 抛物线移动
    Homing,         // 追踪移动
    Spread          // 散射移动
} 