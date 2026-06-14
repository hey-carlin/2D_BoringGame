using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 巡逻行走：朝一个方向走，遇墙/悬崖掉头，不停歇。
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
            enemy.movement.MoveAtPatrolSpeed(new Vector2(enemy.patrolFacing, 0f));
            enemy.animator.Play("Walk");
        }

        public override void OnUpdate()
        {
            flipCooldown -= Time.deltaTime;

            // 遇墙/悬崖 → 掉头
            if (flipCooldown <= 0f && enemy.movement.IsPathBlocked())
            {
                flipCooldown = FlipCooldownTime;
                enemy.patrolFacing = -enemy.patrolFacing;
                enemy.movement.MoveAtPatrolSpeed(new Vector2(enemy.patrolFacing, 0f));
            }
        }

        public override void OnExit()
        {
            enemy.movement.StopMoving();
        }
    }
}
