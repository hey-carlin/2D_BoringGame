using UnityEngine;

namespace Enemy
{
    public class WalkState : EnemyState
    {
        private float enterTime;
        private const float arriveThreshold = 0.25f; // 到达巡逻点阈值

        public WalkState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            enterTime = Time.time;

            // 如果处于追击模式，朝向玩家；否则使用两点巡逻或水平随机
            if (enemy.isChasing && enemy.player != null)
            {
                Vector2 dir = (enemy.player.position - enemy.transform.position).normalized;
                dir.y = 0f;
                enemy.movement.SetMoveDirection(dir);
            }
            else if (enemy.patrolPointA != null && enemy.patrolPointB != null)
            {
                // 使用 patrol 点
                if (enemy.currentPatrolTarget == null) enemy.currentPatrolTarget = enemy.patrolPointA;
                Vector2 dir = (enemy.currentPatrolTarget.position - enemy.transform.position);
                dir.y = 0f;
                enemy.movement.SetMoveDirection(dir.normalized);
            }
            else
            {
                // 退回到水平随机方向，避免 vertical 分量
                float vx = Random.Range(-1f, 1f);
                Vector2 dir = new Vector2(vx, 0f);
                enemy.movement.SetMoveDirection(dir);
            }

            enemy.animator.Play("Walk");
        }

        public override void OnUpdate()
        {
            float distToPlayer = Vector2.Distance(enemy.transform.position, enemy.player.position);

            // 优先攻击
            if (distToPlayer <= enemy.data.attackRange)
            {
                stateMachine.ChangeState(enemy.attackState);
                return;
            }

            // 追击逻辑：每帧修正方向；超出 patrolRange 在 controller 中处理
            if (enemy.isChasing)
            {
                if (enemy.player != null)
                {
                    Vector2 dir = (enemy.player.position - enemy.transform.position);
                    dir.y = 0f;
                    enemy.movement.SetMoveDirection(dir.normalized);
                }
            }

            // 非追击巡逻行为：两点巡逻或随机水平
            if (!enemy.isChasing)
            {
                if (enemy.patrolPointA != null && enemy.patrolPointB != null && enemy.currentPatrolTarget != null)
                {
                    float d = Vector2.Distance(enemy.transform.position, enemy.currentPatrolTarget.position);
                    // 到达目标时切换
                    if (d <= arriveThreshold)
                    {
                        enemy.SwitchToNextPatrolTarget();
                        Vector2 dir = (enemy.currentPatrolTarget.position - enemy.transform.position);
                        dir.y = 0f;
                        enemy.movement.SetMoveDirection(dir.normalized);
                    }
                    else
                    {
                        // 如果前方没有地面或被障碍阻挡，切换目标
                        Vector2 lookDir = (enemy.currentPatrolTarget.position - enemy.transform.position);
                        Vector2 horizDir = new Vector2(Mathf.Sign(lookDir.x), 0f);
                        bool groundAhead = enemy.movement.IsGroundAhead(horizDir, 0.6f, 1.2f);
                        if (!groundAhead || enemy.movement.IsBlocked())
                        {
                            enemy.SwitchToNextPatrolTarget();
                            Vector2 dir = (enemy.currentPatrolTarget.position - enemy.transform.position);
                            dir.y = 0f;
                            enemy.movement.SetMoveDirection(dir.normalized);
                        }
                    }
                }
                else
                {
                    // 随机水平巡逻：如果要走到边缘则反向
                    Vector2 currentDir = new Vector2(Mathf.Sign(enemy.movement.CurrentDirectionX), 0f);
                    bool groundAhead = enemy.movement.IsGroundAhead(currentDir, 0.6f, 1.2f);
                    if (!groundAhead || enemy.movement.IsBlocked())
                    {
                        // 反向
                        Vector2 reverse = new Vector2(-Mathf.Sign(currentDir.x), 0f);
                        enemy.movement.SetMoveDirection(reverse);
                    }
                }

                // 若玩家出现在 sightRange 内，进入追击（保障）
                if (distToPlayer <= enemy.data.sightRange)
                {
                    enemy.StartChase();
                }
            }

            // 非追击超时或撞墙回 idle（保持原逻辑）
            if (!enemy.isChasing && (enemy.movement.IsBlocked() ||
                Time.time - enterTime >= enemy.data.walkDuration))
            {
                stateMachine.ChangeState(enemy.idleState);
                return;
            }
        }

        public override void OnExit()
        {
            enemy.movement.StopMoving();
        }
    }
}