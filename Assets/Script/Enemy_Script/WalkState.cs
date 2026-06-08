using UnityEngine;

namespace Enemy
{
    public class WalkState : EnemyState
    {
        private float enterTime;
        private const float arriveThreshold = 0.25f;
        private const float flipCooldown = 0.4f;
        private float lastFlipTime = -999f;
        private const float blockedFlipCooldown = 0.6f;
        private float lastBlockedFlipTime = -999f;

        public WalkState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            enterTime = Time.time;
            lastFlipTime = Time.time;
            lastBlockedFlipTime = Time.time;

            if (enemy.patrolPointA != null && enemy.patrolPointB != null)
            {
                if (enemy.currentPatrolTarget == null)
                    enemy.currentPatrolTarget = enemy.patrolPointA;

                Vector2 dir = (enemy.currentPatrolTarget.position - enemy.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    enemy.movement.SetMoveDirection(dir.normalized);
            }
            else
            {
                // 有巡逻点但只有一个或两个都没有的情况，随机选一个方向
                if (enemy.currentPatrolTarget != null)
                {
                    Vector2 dir = (enemy.currentPatrolTarget.position - enemy.transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                        enemy.movement.SetMoveDirection(dir.normalized);
                }
                else
                {
                    float vx = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                    enemy.movement.SetMoveDirection(new Vector2(vx, 0f));
                }
            }

            enemy.animator.Play("Walk");
        }

        public override void OnUpdate()
        {
            // 检测玩家期间暂停巡逻，由 DetectionRoutine 控制移动
            if (enemy.detectionInProgress)
                return;

            // 被障碍物挡住或行走超时，切换巡逻目标（在 HandlePatrol 之前，避免干扰抵达判断）
            if (Time.time - lastBlockedFlipTime > blockedFlipCooldown)
            {
                if (enemy.movement.IsBlocked() || Time.time - enterTime >= enemy.data.walkDuration)
                {
                    lastBlockedFlipTime = Time.time;
                    enterTime = Time.time;
                    enemy.SwitchToNextPatrolTarget();
                }
            }

            // 持续朝巡逻目标移动（可能在此切换到 IdleState）
            HandlePatrol();
        }

        private void HandlePatrol()
        {
            if (enemy.currentPatrolTarget != null)
            {
                // ✅ 核心修复：每帧持续朝巡逻目标修正方向，确保严格走到目标点
                Vector2 dir = (enemy.currentPatrolTarget.position - enemy.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    enemy.movement.SetMoveDirection(dir.normalized);

                float d = Vector2.Distance(enemy.transform.position, enemy.currentPatrolTarget.position);
                if (d <= arriveThreshold)
                {
                    if (Time.time - lastFlipTime > flipCooldown)
                    {
                        lastFlipTime = Time.time;
                        // ✅ 到达巡逻点 → 进入待机暂停 → IdleState 负责翻转和目标切换
                        stateMachine.ChangeState(enemy.idleState);
                    }
                }
            }
            else
            {
                // 无巡逻目标：自由漫游，前方无地面或被挡住时翻转
                Vector2 currentDir = new Vector2(Mathf.Sign(enemy.movement.CurrentDirection.x), 0f);
                bool groundAhead = enemy.movement.IsGroundAhead(currentDir, 0.6f, 1.2f);

                if ((!groundAhead || enemy.movement.IsBlocked()) && Time.time - lastFlipTime > flipCooldown)
                {
                    lastFlipTime = Time.time;
                    Vector2 reverse = new Vector2(-Mathf.Sign(currentDir.x), 0f);
                    enemy.movement.SetMoveDirection(reverse);
                }
            }
        }

        public override void OnExit()
        {
            enemy.movement.StopMoving();
        }
    }
}