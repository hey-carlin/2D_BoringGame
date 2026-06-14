using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 追击状态：朝玩家移动，前方是悬崖/墙时放弃追击回到巡逻。
    /// </summary>
    public class ChaseState : EnemyState
    {
        public ChaseState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            enemy.animator.Play("Walk"); // 追击用 Walk 动画（可改为 Run）
        }

        public override void OnUpdate()
        {
            if (enemy.player == null) return;

            // 前方是悬崖/墙 → 放弃追击
            if (enemy.movement.IsPathBlocked())
            {
                stateMachine.ChangeState(enemy.walkState);
                return;
            }

            // 朝玩家水平移动（攻击切换由 Controller 统一管理）
            Vector2 dir = enemy.player.position - enemy.transform.position;
            if (Mathf.Abs(dir.x) > 0.05f)
            {
                enemy.movement.MoveAtChaseSpeed(new Vector2(Mathf.Sign(dir.x), 0f));
                enemy.patrolFacing = (int)Mathf.Sign(dir.x);
            }
        }

        public override void OnExit()
        {
            enemy.movement.StopMoving();
        }
    }
}
