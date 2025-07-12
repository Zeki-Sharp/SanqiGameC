using UnityEngine;

[CreateAssetMenu(fileName = "New Bullet Config", menuName = "Tower Defense/Bullet Config")]
public class BulletConfig : ScriptableObject
{
    [Header("基础属性")]
    [SerializeField] private float speed = 10f;
    [SerializeField]private float lifeTime = 3f;

    public GameObject hitEffect;
    // 可扩展更多属性，如穿透、爆炸等


    //公共属性访问器
    public float BulletSpeed => speed;
    public float BulletLifeTime => lifeTime;
} 