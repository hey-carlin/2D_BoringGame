using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 受伤硬直：击退 + 红色闪烁 + 短暂停顿。
    /// 结束后根据索敌范围决定追击还是巡逻。
    /// </summary>
    public class HurtState : EnemyState
    {
        private float hurtTimer;
        private float flashTimer;
        private bool flashOn;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private static readonly Color HurtColor = new Color(1f, 0.3f, 0.3f, 1f);  // 跟玩家受击同色

        public HurtState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            hurtTimer = 0f;
            flashTimer = 0f;
            flashOn = false;

            enemy.movement.StopMoving();

            // 动画
            enemy.animator.Play("Idle"); // 没有 Hurt 动画则用 Idle

            // 闪烁缓存
            spriteRenderer = enemy.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
                ApplyFlash();
            }
        }

        public override void OnUpdate()
        {
            hurtTimer += Time.deltaTime;
            flashTimer += Time.deltaTime;

            // 闪烁
            if (spriteRenderer != null && flashTimer >= enemy.data.hurtFlashInterval)
            {
                flashTimer = 0f;
                flashOn = !flashOn;
                ApplyFlash();
            }

            // 硬直结束
            if (hurtTimer >= enemy.data.hurtDuration)
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = originalColor;

                // 恢复：索敌范围内 → 追击，否则 → 巡逻
                if (enemy.IsPlayerInAggroRange())
                    stateMachine.ChangeState(enemy.chaseState);
                else
                    stateMachine.ChangeState(enemy.walkState);
            }
        }

        public override void OnExit()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
        }

        private void ApplyFlash()
        {
            if (spriteRenderer == null) return;
            spriteRenderer.color = flashOn ? HurtColor : originalColor;
        }
    }
}
