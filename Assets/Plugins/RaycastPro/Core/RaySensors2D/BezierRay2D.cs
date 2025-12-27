namespace RaycastPro.RaySensors2D
{
    using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
    using Editor;
#endif
    using UnityEngine;
    using UnityEngine;

    [AddComponentMenu("RaycastPro/Rey Sensors/" + nameof(BezierRay2D))]
    public class BezierRay2D : PathRay2D, IRadius
    {
        [SerializeField] private Transform startTransform;
        [SerializeField] private float hight = 2f;
        [SerializeField] private Vector3 middenVector;
        [SerializeField] private Transform endTransform;
        [SerializeField] private int resolution;
        [SerializeField] private float radius = .1f;

        public Vector3 MiddenVector
        {

            get => (startTransform.position + endTransform.position) * .5f + (Vector3.up * hight);

        }
        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0, value);
        }
        public void SetStartEnd(Transform start, Transform end)
        {
            startTransform = start;
            endTransform = end;
        }
        protected override void OnCast()
        {
            UpdatePath();
            if (pathCast)
            {
                DetectIndex = AdvancePathCast(out hit, radius);
                isDetect = hit && FilterCheck(hit);
            }
        }
        protected override void UpdatePath()
        {
            if (startTransform != null && endTransform != null)
            {
                PathPoints.Clear();

                for (int i = 0; i <= resolution; i++)
                {
                    PathPoints.Add(GetBeizerPoint((float)i / resolution, startTransform.position, endTransform.position, MiddenVector));
                }
            }




        }

        private Vector3 GetBeizerPoint(float t, Vector3 start, Vector3 end, Vector3 mid)
        {
            return (1 - t) * (1 - t) * start + 2 * (1 - t) * t * mid + t * t * end;
        }

        #if UNITY_EDITOR

        internal override string Info => "使用贝塞尔曲线制作" + HAccurate + HDirectional + HPathRay + HIRadius;
        internal override void OnGizmos()
        {
            EditorUpdate();
            if (startTransform != null && endTransform != null)
            {
                AdvancePathDraw(radius, true);
            }
            DrawNormal2D(hit, z);
            DrawNormalFilter();
        }

        internal override void EditorPanel(SerializedObject _so, bool hasMain = true, bool hasGeneral = true,
            bool hasEvents = true,
            bool hasInfo = true)
        {
            if (hasMain)
            {
                // DirectionField(_so);
                BeginHorizontal();
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(startTransform)),
                    "Start Transform".ToContent());

                EndHorizontal();
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(endTransform)),
                    "End Transform".ToContent("Enemy TransForm"));

                EditorGUILayout.PropertyField(_so.FindProperty(nameof(hight)),
                    "Hight".ToContent("贝塞尔曲线的Y轴高度"));
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(resolution)), "resolution".ToContent("用于绘制贝塞尔曲线的点数。数值越大，曲线越平滑。\n"));

                StartRadiusField(_so);
                RadiusField(_so);
            }

            if (hasGeneral) PathRayGeneralField(_so);
            if (hasEvents) EventField(_so);
            if (hasInfo) HitInformationField();
        }

#endif
    }
}