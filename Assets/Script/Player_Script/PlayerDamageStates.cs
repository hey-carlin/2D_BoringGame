using UnityEngine;

namespace Player
{
    /// <summary>
    /// 受伤硬直状态：无敌帧 + 击退 + 闪烁反馈。
    /// </summary>
    public class HitState : PlayerState
    {
        private float timer;
        private SpriteRenderer spriteRenderer;
        private float flashTimer;
        private const float FlashInterval = 0.08f;
        private bool flashOn;
        private static readonly Color FlashColor = new Color(1f, 0.3f, 0.3f, 1f);
        private Color originalColor = Color.white;
        private bool hasFlash;

        public HitState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            timer = 0.35f;                               // 硬直动画时长

            // 启动无敌（由 PlayerStateMachine 统一倒计时）
            sm.isInvincible = true;
            sm.invincibilityTimer = sm.invincibilityDuration;
            hasFlash = false;

            // 停止 & 击退
            sm.currentMoveSpeed = 0f;
            sm.rb.velocity = Vector2.zero;

            // 朝受伤反方向击退
            Vector2 knockDir = new Vector2(-sm.FacingDirection(), 1f).normalized;
            sm.rb.AddForce(knockDir * sm.hitKnockbackForce, ForceMode2D.Impulse);

            // 动画
            sm.animator.SetTrigger("Hit");

            // 缓存 SpriteRenderer
            spriteRenderer = sm.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
                hasFlash = true;
            }
        }

        public override void OnUpdate()
        {
            timer -= Time.deltaTime;

            // ── 无敌闪烁 ──
            if (hasFlash && spriteRenderer != null)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    flashTimer = FlashInterval;
                    flashOn = !flashOn;
                    spriteRenderer.color = flashOn ? FlashColor : originalColor;
                }
            }

            // ── 硬直结束 ──
            if (timer <= 0f && sm.isGrounded)
            {
                if (Mathf.Abs(sm.horizontal) > 0.1f)
                    sm.ChangeState(sm.runState);
                else
                    sm.ChangeState(sm.idleState);
            }
        }

        public override void OnFixedUpdate()
        {
            // 硬直中不受移动输入控制，仅靠击退速度自然衰减
            sm.currentMoveSpeed = sm.rb.velocity.x;
        }

        public override void OnExit()
        {
            // 无敌时间由 invTimer 控制，在 PlayerStateMachine 中递减
            // Exit 时可能还没到，所以在 FixedUpdate 里继续倒计时
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    /// <summary>
    /// 死亡状态：禁用所有输入和物理，播放死亡动画。
    /// </summary>
    public class DeadState : PlayerState
    {
        public DeadState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            sm.controlsEnabled = false;
            sm.isInvincible = true;
            sm.currentMoveSpeed = 0f;
            sm.rb.velocity = Vector2.zero;
            sm.rb.gravityScale = 0f;
            sm.animator.SetTrigger("Death");
        }

        public override void OnExit()
        {
            sm.rb.gravityScale = 2.5f;
        }
    }
}
