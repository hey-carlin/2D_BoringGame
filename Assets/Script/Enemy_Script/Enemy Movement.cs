using UnityEngine;
using Enemy;

namespace Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovement : MonoBehaviour
    {
        #region 属性
        public float moveSpeed { get; private set; }
        public float sightRange { get; private set; }

        public Vector2 CurrentDirection => currentDirection;
        public float CurrentDirectionX => currentDirection.x; // ✅ 补上
        #endregion

        #region 字段
        private Rigidbody2D rb;
        private Vector2 currentDirection;
        private EnemyController enemy;
        #endregion

        #region 转向
        [Header("Graphics")]
        public Transform graphics;
        public float facingLerpSpeed = 12f;

        private float baseScaleX = 1f;
        #endregion

        #region 初始化
        public void Init(float speed, float sight)
        {
            moveSpeed = speed;
            sightRange = sight;
        }

        void Awake()
        {
            enemy = GetComponent<EnemyController>();
            rb = GetComponent<Rigidbody2D>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            if (graphics == null)
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                graphics = sr != null ? sr.transform : transform;
            }

            baseScaleX = Mathf.Abs(graphics.localScale.x);
            if (baseScaleX == 0f) baseScaleX = 1f;
        }
        #endregion

        #region 移动
        public void SetMoveDirection(Vector2 dir)
        {
            currentDirection = dir.normalized;
        }

        public void StopMoving()
        {
            currentDirection = Vector2.zero;
            rb.velocity = Vector2.zero;
        }
        #endregion

        #region 转向
        private int facingDirection = 1; // 1=右, -1=左, 用于检测方向反转

        public void FaceDirection(Vector2 direction, bool immediate = false)
        {
            if (graphics == null || direction.sqrMagnitude < 0.0001f)
                return;

            bool faceRight = direction.x >= 0f;
            int newFacing = faceRight ? 1 : -1;

            // 方向反转时立即翻转，避免 Lerp 造成的抽搐
            if (newFacing != facingDirection)
            {
                facingDirection = newFacing;
                immediate = true;
            }

            float targetX = baseScaleX * (faceRight ? 1f : -1f);

            Vector3 s = graphics.localScale;
            if (immediate || facingLerpSpeed <= 0f)
            {
                s.x = targetX;
            }
            else
            {
                s.x = Mathf.MoveTowards(s.x, targetX, facingLerpSpeed * Time.deltaTime);
            }

            graphics.localScale = s;
        }
        #endregion

        #region 物理
        void FixedUpdate()
        {
            rb.velocity = currentDirection * moveSpeed;

            if (enemy != null && enemy.animator != null)
            {
                enemy.animator.SetFloat("Speed", rb.velocity.magnitude);
            }

            if (currentDirection.sqrMagnitude > 0.0001f)
            {
                FaceDirection(currentDirection);
            }
        }
        #endregion

        #region 检测参数
        [Header("墙体检测线")]
        public float wallCheckDistance = 0.6f;           // 墙体检测线长度（水平向前）
        public LayerMask wallLayer = ~0;                  // 墙体层级

        [Header("地面/悬崖检测线")]
        public float groundCheckAhead = 0.6f;             // 检测起点前移距离
        public float groundCheckDown = 1.2f;              // 向下检测深度
        public LayerMask groundLayer = ~0;                 // 地面层级

        [Header("调试")]
        public bool showDetectionGizmos = true;
        #endregion

        #region 检测方法
        /// <summary>墙体检测线：前方水平射线，触碰墙体返回 true</summary>
        public bool IsBlocked()
        {
            if (currentDirection.sqrMagnitude < 0.0001f) return false;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, currentDirection, wallCheckDistance, wallLayer);
            return hit.collider != null;
        }

        /// <summary>地面/悬崖检测线：前方往下射线，检测不到地面返回 false（前方是悬崖）</summary>
        public bool IsGroundAhead()
        {
            Vector2 dir = currentDirection.sqrMagnitude > 0.0001f
                ? currentDirection.normalized
                : Vector2.right;

            Vector2 origin = (Vector2)transform.position + dir * groundCheckAhead;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDown, groundLayer);
            return hit.collider != null;
        }

        /// <summary>综合检测：前方是否有墙体或悬崖</summary>
        public bool IsPathBlocked()
        {
            return IsBlocked() || !IsGroundAhead();
        }
        #endregion

        #region 编辑器 Gizmos
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!showDetectionGizmos) return;

            Vector2 pos = transform.position;
            Vector2 dir = Application.isPlaying && currentDirection.sqrMagnitude > 0.0001f
                ? currentDirection.normalized
                : Vector2.right;

            // ── 墙体检测线（黄色）──
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, pos + dir * wallCheckDistance);
            Gizmos.DrawWireSphere(pos + dir * wallCheckDistance, 0.05f);

            // ── 地面检测线（绿色，前方往下）──
            Gizmos.color = Color.green;
            Vector2 groundOrigin = pos + dir * groundCheckAhead;
            Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDown);
            Gizmos.DrawWireSphere(groundOrigin + Vector2.down * groundCheckDown, 0.05f);
        }
#endif
        #endregion
    }
}