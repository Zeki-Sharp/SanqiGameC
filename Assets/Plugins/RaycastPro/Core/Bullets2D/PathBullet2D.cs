namespace RaycastPro.Bullets2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using RaySensors2D;

#if UNITY_EDITOR
    using UnityEditor;
#endif


    [AddComponentMenu("RaycastPro/Bullets/" + nameof(PathBullet2D))]
    public sealed class PathBullet2D : Bullet2D, IPath<Vector2>
    {
        public List<Vector2> Path { get; internal set; } = new List<Vector2>();
        
        public float duration = 1;
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        
        [SerializeField]
        private AxisRun axisRun = new AxisRun();
        
        [SerializeField]
        private Rigidbody2D rigidBody;
        
        private float pathLength;

        [SerializeField] private bool local;
        
        // 层级管理相关
        [Header("层级管理")]
        [SerializeField] private Vector3 startPosition; // 发射位置
        [SerializeField] private GameObject targetObject; // 目标对象
        [SerializeField] private bool hasUpdatedLayer = false; // 是否已更新层级
        
        // Cached Variables
        private Vector3 _pos, _dir;
        private float _dt;
        internal override void Cast<R>(BaseCaster _caster, R raySensor)
        {
#if UNITY_EDITOR
            alphaCharge = AlphaLifeTime;
#endif
            caster = _caster;
            
            raySource = raySensor as RaySensor2D;
            
            // 记录发射位置和目标
            startPosition = transform.position;
            if (raySource != null && raySource.hit.transform != null)
            {
                targetObject = raySource.hit.transform.gameObject;
            }
            
            if (!raySource)
            {
                transform.position = caster.transform.position;
                transform.forward = caster.transform.right;
            }
            
            OnCast(); // Auto Setup 3D Bullet
            onCast?.Invoke(caster);
            if (collisionRay)
            {
                collisionRay.enabled = false;
            }

            onCast?.Invoke(caster);
        }
        
        protected override void OnCast() => PathSetup(raySource);
        
        private void PathSetup(RaySensor2D raySensor)
        {
            Path = new List<Vector2>();
            do
            {
                if (raySensor is PathRay2D _pathRay)
                {
                    if (_pathRay.DetectIndex > -1)
                    {
                        for (var i = 0; i <= _pathRay.DetectIndex; i++)
                        {
                            Path.Add(_pathRay.PathPoints[i]);
                        }
                        Path.Add(raySensor.hit.point);
                    }
                    else
                    {
                        Path.AddRange(local ? new List<Vector2>(_pathRay.PathPoints) : _pathRay.PathPoints);
                    }
                }
                else
                {
                    Path.Add(raySensor.Base);
                    Path.Add(raySensor.TipTarget);
                }
                
                raySensor = raySensor.cloneRaySensor;
                
            } while (raySensor);

            pathLength = Path.GetPathLength();
        }

        private float posM;

        internal override void RuntimeUpdate()
        {
            position = Mathf.Clamp01(position);
            
            if (moveType == MoveType.Curve)
            {
                posM = curve.Evaluate(position) * pathLength;
            }
            else
            {
                posM = position * pathLength;
            }
            _dt = GetDelta(timeMode);
            UpdateLifeProcess(_dt);
            
            // 更新层级管理
            UpdateBulletLayer();
            
            switch (moveType)
            {
                case MoveType.Speed:
                    position += _dt * speed / pathLength;
                    break;
                case MoveType.Duration:
                    position += _dt / duration;
                    break;
                case MoveType.Curve:
                    position += _dt / duration;
                    break;
            }
            if (position >= 1) OnEndCast(caster);
            for (var i = 1; i < Path.Count; i++)
            {
                lineDistance = Path.GetEdgeLength(i);
                if (posM <= lineDistance)
                {
                    _pos = Vector3.Lerp(Path[i - 1], Path[i], posM / lineDistance);
                    _dir = Path[i] - Path[i - 1];

                    break;
                }
                posM -= lineDistance;
            }
            if (rigidBody) rigidBody.MovePosition(_pos);
            else transform.position = _pos.ToDepth(Z);
            if (axisRun.syncAxis) axisRun.SyncAxis(transform, _dir);
            if (collisionRay) CollisionRun(_dt);
            transform.eulerAngles = Vector3.zero;
        }
        private float lineDistance;
        
        /// <summary>
        /// 更新子弹层级（基于飞行阶段）
        /// </summary>
        private void UpdateBulletLayer()
        {
            if (startPosition == Vector3.zero || targetObject == null) return;
            
            // 计算最高点（发射点和目标点的较高位置 + 额外高度）
            float maxHeight = Mathf.Max(startPosition.y, targetObject.transform.position.y) + 2f;
            
            // 判断子弹当前飞行阶段
            float currentHeight = transform.position.y;
            
            // 只在关键节点更新层级
            bool shouldUpdateLayer = false;
            int newSortingOrder = 0;
            
            if (currentHeight < maxHeight)
            {
                // 子弹在上升阶段：使用出发点（塔）的层级
                newSortingOrder = Mathf.RoundToInt(-startPosition.y * 10);
                shouldUpdateLayer = !hasUpdatedLayer; // 发射后立即更新一次
            }
            else
            {
                // 子弹在下落阶段：使用目标点（敌人）的层级
                newSortingOrder = Mathf.RoundToInt(-targetObject.transform.position.y * 10);
                shouldUpdateLayer = !hasUpdatedLayer; // 到达最高点后更新一次
            }
            
            // 只在需要时更新层级
            if (shouldUpdateLayer)
            {
                // 获取所有渲染器组件
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                        {
                            renderer.sortingOrder = newSortingOrder;
                        }
                    }
                    
                    hasUpdatedLayer = true;
                    Debug.Log($"子弹 {name} 更新层级: {newSortingOrder} (阶段: {(currentHeight < maxHeight ? "上升" : "下落")})");
                }
            }
        }
        
#if UNITY_EDITOR
        internal override string Info =>  "A smart bullet that can recognize the path of the PathRay and move on it." + HAccurate + HDependent;
        internal override void EditorPanel(SerializedObject _so, bool hasMain = true, bool hasGeneral = true,
            bool hasEvents = true,
            bool hasInfo = true)
        {
            if (hasMain)
            {
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(local)));
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(rigidBody)));
                CastTypeField(
                    _so.FindProperty(nameof(moveType)),
                    _so.FindProperty(nameof(speed)), 
                    _so.FindProperty(nameof(duration)),
                    _so.FindProperty(nameof(curve)));
                axisRun.EditorPanel(_so.FindProperty(nameof(axisRun)));
            }

            if (hasGeneral) GeneralField(_so);
            if (hasEvents) EventField(_so);
            if (hasInfo) InformationField();
        }
#endif
    }
}