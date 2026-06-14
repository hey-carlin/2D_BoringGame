using UnityEngine;

namespace Player
{
    public class AttackState : PlayerState
    {
        private float timer;
        private bool dealtDamage;             // 本次攻击是否已判定伤害

        public AttackState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            timer = sm.activeAttackDuration;
            dealtDamage = false;

            // ── 攻击前冲步 ──
            bool isHeavy = sm.animator.GetCurrentAnimatorStateInfo(0).IsName("Heavy_Attack");
            if (!isHeavy)
            {
                float stepDir = sm.FacingDirection();
                sm.currentMoveSpeed = stepDir * sm.attackForwardStep;
                sm.rb.velocity = new Vector2(sm.currentMoveSpeed, sm.rb.velocity.y);
            }
            else
            {
                sm.currentMoveSpeed = 0f;
                sm.rb.velocity = new Vector2(0f, sm.rb.velocity.y);
            }
        }

        public override void OnUpdate()
        {
            timer -= Time.deltaTime;

            // ── 攻击命中判定（计时驱动，不依赖 Animation Event）──
            //     在动画前 30% 处判定（即挥剑到位的时刻）
            float hitTime = sm.activeAttackDuration * 0.3f;
            if (!dealtDamage && timer <= sm.activeAttackDuration - hitTime)
            {
                dealtDamage = true;
                sm.OnAttackHitFrame();  // → PlayerMeleeAttack.OnAttackHitFrame()
            }

            // ── 攻击结束 ──
            if (timer <= 0f)
            {
                sm.animator.SetInteger("AttackID", 0);
                sm.AttackFinished();
            }
        }

        public override void OnFixedUpdate()
        {
            // 攻击中：轻击保留前冲惯性逐渐衰减，重击定在原地
            if (sm.animator.GetCurrentAnimatorStateInfo(0).IsName("Heavy_Attack"))
            {
                sm.rb.velocity = new Vector2(0f, sm.rb.velocity.y);
            }
            else
            {
                sm.currentMoveSpeed = Mathf.MoveTowards(sm.currentMoveSpeed, 0f, sm.deceleration * 0.5f * Time.fixedDeltaTime);
                sm.rb.velocity = new Vector2(sm.currentMoveSpeed, sm.rb.velocity.y);
            }
        }
    }
}
