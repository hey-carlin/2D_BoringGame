using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 追击状态：朝玩家移动。墙体/悬崖阻挡时放弃追击，回到随机巡逻。
    /// </summary>
    public class ChaseState : EnemyState
    {
        public ChaseState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            enemy.animator.Play("Walk");
        }

        public override void OnUpdate()
        {
            if (enemy.player == null) return;

            // ── 墙体/悬崖检测优先级最高 → 立即放弃追击 ──
            if (enemy.movement.IsPathBlocked())
            {
                stateMachine.ChangeState(enemy.walkState);
                return;
            }

            // 进入攻击范围 → 攻击
            if (enemy.IsPlayerInAttackRange())
            {
                stateMachine.ChangeState(enemy.attackState);
                return;
            }

            // 朝玩家移动
            Vector2 dir = (enemy.player.position - enemy.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                enemy.movement.SetMoveDirection(dir.normalized);
        }

        public override void OnExit()
        {
            enemy.movement.StopMoving();
        }
    }
}
