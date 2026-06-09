using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 平台来回巡逻状态：
    /// - 一直朝一个方向走，遇到墙/悬崖立即掉头
    /// - 没有计时器，没有随机转向，纯障碍物驱动
    /// </summary>
    public class WalkState : EnemyState
    {
        private float flipCooldown;
        private const float FlipCooldownTime = 0.4f;

        public WalkState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            flipCooldown = 0f;

            // 保持 patrolFacing 方向，继续走
            float vx = enemy.patrolFacing;
            enemy.movement.SetMoveDirection(new Vector2(vx, 0f));

            enemy.animator.Play("Walk");
        }

        public override void OnUpdate()
        {
            flipCooldown -= Time.deltaTime;

            // ── 唯一转向条件：遇到墙或悬崖 ──
            if (flipCooldown <= 0f)
            {
                if (enemy.movement.IsPathBlocked())
                {
                    flipCooldown = FlipCooldownTime;

                    // 掉头
                    enemy.patrolFacing = -enemy.patrolFacing;
                    enemy.movement.SetMoveDirection(new Vector2(enemy.patrolFacing, 0f));
                }
            }
        }

        public override void OnExit()
        {
            enemy.movement.StopMoving();
        }
    }
}
