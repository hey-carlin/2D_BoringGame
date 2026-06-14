using UnityEngine;

namespace Enemy
{
    public class DeadState : EnemyState
    {
        private float timer;
        private SpriteRenderer spriteRenderer;
        private Collider2D[] colliders;
        private Rigidbody2D rb;

        public DeadState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            timer = 0f;

            enemy.animator.Play("Death");
            enemy.movement.StopMoving();

            rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.simulated = false;
            }

            // 禁用碰撞（死后不挡路、不被继续打）
            colliders = enemy.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
                col.enabled = false;

            // 渐隐用
            spriteRenderer = enemy.GetComponentInChildren<SpriteRenderer>();
        }

        public override void OnUpdate()
        {
            timer += Time.deltaTime;

            // 渐隐
            if (spriteRenderer != null && enemy.data.deathFadeDuration > 0f)
            {
                float fadeT = Mathf.Clamp01(timer / enemy.data.deathFadeDuration);
                float fadeStart = 0.4f;
                if (fadeT > fadeStart)
                {
                    float alpha = 1f - Mathf.InverseLerp(fadeStart, 1f, fadeT);
                    Color c = spriteRenderer.color;
                    c.a = alpha;
                    spriteRenderer.color = c;
                }
            }

            // 不再 Destroy — 重生由 EnemyController.RespawnRoutine 管理
        }

        public override void OnExit()
        {
            // 重生时恢复（RespawnRoutine 已经恢复渲染/碰撞，这里做兜底）
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f;
                spriteRenderer.color = c;
            }
            if (colliders != null)
            {
                foreach (var col in colliders)
                    if (col != null) col.enabled = true;
            }
            if (rb != null)
                rb.simulated = true;
        }
    }
}
