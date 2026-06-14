using UnityEngine;

namespace Player
{
    /// <summary>
    /// 待机状态：平滑减速到零。
    /// </summary>
    public class IdleState : PlayerState
    {
        public IdleState(PlayerStateMachine sm) : base(sm) { }

        public override void OnFixedUpdate()
        {
            float decel = sm.isGrounded ? sm.deceleration : sm.airDeceleration;
            sm.currentMoveSpeed = Mathf.MoveTowards(sm.currentMoveSpeed, 0f, decel * Time.fixedDeltaTime);
            sm.rb.velocity = new Vector2(sm.currentMoveSpeed, sm.rb.velocity.y);
        }
    }

    /// <summary>
    /// 奔跑状态：加速度驱动，地面快空中慢。
    /// </summary>
    public class RunState : PlayerState
    {
        public RunState(PlayerStateMachine sm) : base(sm) { }

        public override void OnFixedUpdate()
        {
            float targetSpeed = sm.horizontal * sm.moveSpeed;

            if (Mathf.Abs(sm.horizontal) > 0.1f)
            {
                float accel = sm.isGrounded ? sm.acceleration : sm.airAcceleration;
                sm.currentMoveSpeed = Mathf.MoveTowards(sm.currentMoveSpeed, targetSpeed, accel * Time.fixedDeltaTime);
            }
            else
            {
                float decel = sm.isGrounded ? sm.deceleration : sm.airDeceleration;
                sm.currentMoveSpeed = Mathf.MoveTowards(sm.currentMoveSpeed, 0f, decel * Time.fixedDeltaTime);
            }

            sm.rb.velocity = new Vector2(sm.currentMoveSpeed, sm.rb.velocity.y);

            if (!sm.isGrounded && Mathf.Abs(sm.currentMoveSpeed) < 0.05f && Mathf.Abs(sm.horizontal) < 0.1f)
            {
                sm.ChangeState(sm.idleState);
            }
        }
    }

    /// <summary>
    /// 跳跃状态：一段跳 + 空中二段跳（buffer 驱动）+ 落地检测。
    /// </summary>
    public class JumpState : PlayerState
    {
        private bool hasDoubleJumped;       // 本次空中是否已用二段跳
        private float airJumpGrace;         // 起跳后短暂禁止二段跳（防误触）

        public JumpState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            hasDoubleJumped = false;
            airJumpGrace = 0.08f;

            // 消耗 1 次跳跃
            sm.remainingJumps--;
            sm.SetJumpTrigger();

            // 施力起跳
            sm.rb.velocity = new Vector2(sm.rb.velocity.x, 0f);
            sm.rb.AddForce(Vector2.up * sm.jumpForce, ForceMode2D.Impulse);
        }

        public override void OnUpdate()
        {
            airJumpGrace -= Time.deltaTime;

            // ── 落地检测 ──
            if (sm.isGrounded && sm.rb.velocity.y <= 0.1f)
            {
                if (Mathf.Abs(sm.horizontal) > 0.1f)
                    sm.ChangeState(sm.runState);
                else
                    sm.ChangeState(sm.idleState);
                return;
            }

            // ── 空中二段跳（buffer 驱动，丝滑不卡）──
            if (!hasDoubleJumped
                && airJumpGrace <= 0f                     // 起跳后短暂保护
                && sm.remainingJumps > 0                  // 还有跳跃次数
                && sm.jumpBufferTimer > 0f)               // buffer 里有输入
            {
                hasDoubleJumped = true;
                sm.jumpBufferTimer = 0f;
                sm.remainingJumps--;
                sm.SetJumpTrigger();

                // 二段跳力（略弱于一段）
                sm.rb.velocity = new Vector2(sm.rb.velocity.x, 0f);
                sm.rb.AddForce(Vector2.up * sm.jumpForce * 0.85f, ForceMode2D.Impulse);
            }
        }

        public override void OnFixedUpdate()
        {
            // 空中移动控制
            float targetSpeed = sm.horizontal * sm.moveSpeed;
            float accel = Mathf.Abs(sm.horizontal) > 0.1f ? sm.airAcceleration : sm.airDeceleration;
            sm.currentMoveSpeed = Mathf.MoveTowards(sm.currentMoveSpeed, targetSpeed, accel * Time.fixedDeltaTime);
            sm.rb.velocity = new Vector2(sm.currentMoveSpeed, sm.rb.velocity.y);
        }
    }
}
