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

        #region 转向（2D 横版唯一正确方式）
        public void FaceDirection(Vector2 direction, bool immediate = false)
        {
            if (graphics == null || direction.sqrMagnitude < 0.0001f)
                return;

            bool faceRight = direction.x >= 0f;
            float targetX = baseScaleX * (faceRight ? 1f : -1f);

            Vector3 s = graphics.localScale;
            if (immediate || facingLerpSpeed <= 0f)
            {
                s.x = targetX;
            }
            else
            {
                s.x = Mathf.Lerp(s.x, targetX, facingLerpSpeed * Time.deltaTime);
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

        #region 检测
        public bool IsBlocked()
        {
            if (currentDirection == Vector2.zero) return false;
            return Physics2D.Raycast(transform.position, currentDirection, 0.5f, LayerMask.GetMask("Obstacle"));
        }

        public bool IsGroundAhead(Vector2 direction, float distance, float verticalOffset)
        {
            Vector2 origin = (Vector2)transform.position +
                            direction.normalized * distance +
                            Vector2.up * verticalOffset;

            return Physics2D.Raycast(origin, Vector2.down, verticalOffset + 0.1f, LayerMask.GetMask("Ground"));
        }
        #endregion
    }
}