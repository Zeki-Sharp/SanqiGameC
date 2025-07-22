using UnityEngine;

public class BezierCurve : MonoBehaviour
{
  [SerializeField] private float start;
  [SerializeField] private float end;
  [SerializeField] private float hight;

  public BezierCurve() { }
  public BezierCurve(float start, float end, float hight)
  {
    this.start = start;
    this.end = end;
    this.hight = hight;
  }
  /// <summary>
  /// 获取贝塞尔曲线点 2D
  /// </summary>
  /// <param name="t"></param>
  /// <returns> </returns>
  public Vector3 GetPoint(float t) => Vector3.Lerp(Vector3.Lerp(new Vector3(start, hight, 0f), new Vector3(end, hight, 0f), t), Vector3.Lerp(new Vector3(start, hight, 0f), new Vector3(end, hight, 0f), t), t);
}
