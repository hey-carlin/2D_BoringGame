using UnityEngine;
using Enemy;

namespace Enemy
{
    /// <summary>
    /// 敌人移动模块
    /// - 负责物理移动
    /// - 巡逻 / 追击时的速度控制
    /// - 2D 横版左右转向（FlipX）
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovement : MonoBehaviour
    {
        #region 对外属性
        public float moveSpeed { get; private set; }   // 当前移动速度
        public float sightRange { get; private set; }  // 索敌范围（备用）
        #endregion

        #region 配置
        public LayerMask obstacleLayer; // 障碍物层（用于检测墙 / 边缘）
        #endregion

        #region 私有字段
        private Rigidbody2D rb;               // 刚体
        private Vector2 currentDirection;      // 当前移动方向（归一化）
        private EnemyController enemy;         // 敌人主控
        #endregion

        #region 只读访问
        public Vector2 CurrentDirection => currentDirection;
        public float CurrentDirectionX => currentDirection.x;
        #endregion

        #region 转向设置（2D 横版只用 FlipX）
        public enum FacingMode { FlipX = 0 } // 防止误用旋转
        public FacingMode facingMode = FacingMode.FlipX;

        [Tooltip("用于翻转的图形节点（Sprite / 子物体）")]
        public Transform graphics;

        [Tooltip("转向平滑速度")]
        public float facingLerpSpeed = 12f;

        private float graphicsBaseScaleX = 1f; // 原始 X 缩放绝对值
        #endregion

        #region 初始化
        /// <summary>
        /// 外部初始化（由 EnemyController 调用）
        /// </summary>
        public void Init(float speed, float sight)
        {
            moveSpeed = speed;
            sightRange = sight;
        }

        void Awake()
        {
            // 获取核心组件
            enemy = GetComponent<EnemyController>();
            rb = GetComponent<Rigidbody2D>();

            // 2D 横版必须关重力 & 冻结旋转
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            // 自动找 Sprite（如果没手动拖）
            if (graphics == null)
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                graphics = sr != null ? sr.transform : transform;
            }

            // 记录原始 X 缩放（防止 Flip 时越来越小）
            graphicsBaseScaleX = Mathf.Abs(graphics.localScale.x);
            if (graphicsBaseScaleX == 0f)
                graphicsBaseScaleX = 1f;
        }
        #endregion

        #region 移动控制
        /// <summary>
        /// 设置移动方向（归一化）
        /// </summary>
        public void SetMoveDirection(Vector2 dir)
        {
            currentDirection = dir.normalized;
        }

        /// <summary>
        /// 立即停止移动
        /// </summary>
        public void StopMoving()
        {
            currentDirection = Vector2.zero;
            rb.velocity = Vector2.zero;
        }
        #endregion

        #region 转向（2D 横版核心）
        /// <summary>
        /// 强制面向某个方向（常用于追击玩家）
        /// </summary>
        public void FaceDirection(Vector2 direction)
        {
            if (graphics == null || direction.sqrMagnitude <= 0.0001f)
                return;

            bool faceRight = direction.x >= 0f;
            float targetX = graphicsBaseScaleX * (faceRight ? 1f : -1f);

            Vector3 s = graphics.localScale;
            s.x = Mathf.Lerp(s.x, targetX, facingLerpSpeed * Time.deltaTime);
            graphics.localScale = s;
        }
        #endregion

        #region 物理更新
        void FixedUpdate()
        {
            // 移动
            rb.velocity = currentDirection * moveSpeed;

            // 同步动画速度
            if (enemy != null && enemy.animator != null)
            {
                enemy.animator.SetFloat("Speed", rb.velocity.magnitude);
            }

            // ✅ 巡逻时：根据移动方向翻转
            if (graphics != null && currentDirection.sqrMagnitude > 0.0001f)
            {
                bool faceRight = currentDirection.x >= 0f;
                float targetX = graphicsBaseScaleX * (faceRight ? 1f : -1f);

                Vector3 s = graphics.localScale;
                s.x = Mathf.Lerp(s.x, targetX, facingLerpSpeed * Time.deltaTime);
                graphics.localScale = s;
            }
        }
        #endregion

        #region 环境检测
        /// <summary>
        /// 前方是否有地面（防止走下平台）
        /// </summary>
        public bool IsGroundAhead(Vector2 direction, float distance, float verticalOffset)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            Vector2 origin =
                (Vector2)transform.position +
                direction.normalized * distance +
                Vector2.up * verticalOffset;

            return Physics2D.Raycast(origin, Vector2.down, verticalOffset + 0.1f, obstacleLayer);
        }

        /// <summary>
        /// 前方是否被墙挡住
        /// </summary>
        public bool IsBlocked()
        {
            if (currentDirection == Vector2.zero)
                return false;

            return Physics2D.Raycast(transform.position, currentDirection, 0.5f, obstacleLayer);
        }
        #endregion
    }
}