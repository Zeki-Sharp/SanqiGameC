using UnityEngine;

public class BulletLayerController : MonoBehaviour
{
    [Header("层级设置")]
    [SerializeField] private SortingLayer bulletLayer;
    [SerializeField] private int bulletOrderInLayer;
    [SerializeField] private int maxBulletOrderInLayer;
    [SerializeField] private int minBulletOrderInLayer;
    [SerializeField] private BezierCurve layerConfigure;
    public void Initialize(SortingLayer sortingLayer, int orderInLayer, int maxOrderInLayer, int minOrderInLayer, BezierCurve layerConfigure)
    {
        this.bulletLayer = sortingLayer;
        this.bulletOrderInLayer = orderInLayer;
        this.maxBulletOrderInLayer = maxOrderInLayer;
        this.minBulletOrderInLayer = minOrderInLayer;
        this.layerConfigure = layerConfigure;
        Debug.Log("BulletLayerController 初始化完成");
    }
    public Vector3 SetBulletOrderInLayer(float t)
    {
        int orderInLayer = Mathf.RoundToInt(Mathf.Lerp(minBulletOrderInLayer, maxBulletOrderInLayer, t));
        GetComponent<Renderer>().sortingOrder = orderInLayer;
        return layerConfigure.GetPoint(t);
    }
}