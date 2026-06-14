using UnityEngine;

namespace Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovement : MonoBehaviour
    {
        public float patrolSpeed { get; private set; }
        public float chaseSpeed { get; private set; }

        public Vector2 CurrentDirection => currentDirection;

        private Rigidbody2D rb;
        private Vector2 currentDirection;
        private float currentSpeed;          // 当前目标速度（0=停止，>0=移动中）
        private EnemyController enemy;

        [Header("Graphics")]
        public Transform graphics;
        public float facingLerpSpeed = 12f;
        private float baseScaleX = 1f;
        private int facingDirection = 1;

        [Header("墙体检测")]
        public float wallCheckDistance = 0.6f;
        public LayerMask wallLayer = ~0;

        [Header("地面 / 悬崖检测（共用参数）")]
        public float groundCheckDown = 1.2f;         // 向下检测线长度
        public float groundCheckAhead = 0.6f;        // 悬崖检测向前偏移
        public LayerMask groundLayer = ~0;

        [Header("调试")]
        public bool showDetectionGizmos = true;

        public void Init(float patrolSpd, float chaseSpd)
        {
            patrolSpeed = patrolSpd;
            chaseSpeed = chaseSpd;
        }

        void Awake()
        {
            enemy = GetComponent<EnemyController>();
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.freezeRotation = true;

            if (graphics == null)
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                graphics = sr != null ? sr.transform : transform;
            }

            baseScaleX = Mathf.Abs(graphics.localScale.x);
            if (baseScaleX == 0f) baseScaleX = 1f;
        }

        // ──── 移动 ────

        /// <summary>以巡逻速度移动</summary>
        public void MoveAtPatrolSpeed(Vector2 dir)
        {
            currentDirection = dir.normalized;
            currentSpeed = patrolSpeed;
        }

        /// <summary>以追击速度移动</summary>
        public void MoveAtChaseSpeed(Vector2 dir)
        {
            currentDirection = dir.normalized;
            currentSpeed = chaseSpeed;
        }

        public void StopMoving()
        {
            currentDirection = Vector2.zero;
            currentSpeed = 0f;
        }

        // ──── 转向 ────

        public void FaceDirection(Vector2 direction, bool immediate = false)
        {
            if (graphics == null || direction.sqrMagnitude < 0.0001f) return;

            bool faceRight = direction.x >= 0f;
            int newFacing = faceRight ? 1 : -1;

            if (newFacing != facingDirection)
            {
                facingDirection = newFacing;
                immediate = true; // 方向反转时立即翻面
            }

            float targetX = baseScaleX * (faceRight ? 1f : -1f);
            Vector3 s = graphics.localScale;

            if (immediate || facingLerpSpeed <= 0f)
                s.x = targetX;
            else
                s.x = Mathf.MoveTowards(s.x, targetX, facingLerpSpeed * Time.deltaTime);

            graphics.localScale = s;
        }

        // ──── 物理 ────

        void FixedUpdate()
        {
            // 每帧持续施加水平速度（Y 留给重力）
            rb.velocity = new Vector2(currentDirection.x * currentSpeed, rb.velocity.y);

            if (currentDirection.sqrMagnitude > 0.0001f)
                FaceDirection(currentDirection);

            if (enemy != null && enemy.animator != null)
                enemy.animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        }

        // ──── 检测 ────

        /// <summary>当前是否站在地面上</summary>
        public bool IsGrounded()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDown, groundLayer);
            return hit.collider != null;
        }

        /// <summary>获取当前朝向（编辑器模式安全）</summary>
        private Vector2 GetFacingDir()
        {
            if (currentDirection.sqrMagnitude > 0.0001f)
                return currentDirection.normalized;
            if (enemy != null)
                return Vector2.right * enemy.patrolFacing;
            return Vector2.right; // 编辑器默认朝右
        }

        /// <summary>前方是否有墙</summary>
        public bool IsBlocked()
        {
            Vector2 dir = GetFacingDir();
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, wallCheckDistance, wallLayer);
            return hit.collider != null;
        }

        /// <summary>前方是否有地面（悬崖检测）</summary>
        public bool IsGroundAhead()
        {
            Vector2 dir = GetFacingDir();
            Vector2 origin = (Vector2)transform.position + dir * groundCheckAhead;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDown, groundLayer);
            return hit.collider != null;
        }

        /// <summary>路径被挡（前方是墙或悬崖）</summary>
        public bool IsPathBlocked()
        {
            return IsBlocked() || !IsGroundAhead();
        }

        // ──── Gizmos ────

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!showDetectionGizmos) return;

            Vector2 pos = transform.position;
            Vector2 dir = GetFacingDir();

            // 地面检测线（脚下向下）
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawLine(pos, pos + Vector2.down * groundCheckDown);
            Gizmos.DrawWireSphere(pos + Vector2.down * groundCheckDown, 0.05f);

            // 墙体检测线（前方水平）
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, pos + dir * wallCheckDistance);
            Gizmos.DrawWireSphere(pos + dir * wallCheckDistance, 0.05f);

            // 悬崖检测线（前方向下）
            Color cliffColor = IsGroundAhead() ? Color.green : Color.red;
            Gizmos.color = cliffColor;
            Vector2 groundOrigin = pos + dir * groundCheckAhead;
            Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDown);
            Gizmos.DrawWireSphere(groundOrigin + Vector2.down * groundCheckDown, 0.05f);
        }
#endif
    }
}
