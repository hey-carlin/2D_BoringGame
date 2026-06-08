using UnityEngine;

namespace Enemy
{
    public class IdleState : EnemyState
    {
        private float timer;

        public IdleState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            timer = 0f;
            enemy.movement.StopMoving();
            enemy.animator.Play("Idle");
        }

        public override void OnUpdate()
        {
            timer += Time.deltaTime;

            // 检测玩家期间不处理巡逻逻辑
            if (enemy.detectionInProgress)
                return;

            if (timer >= enemy.data.idleDuration)
            {
                // 切换到下一个巡逻目标并立即翻转朝向
                enemy.SwitchToNextPatrolTarget();
                if (enemy.currentPatrolTarget != null)
                {
                    Vector2 dir = enemy.currentPatrolTarget.position - enemy.transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                        enemy.movement.FaceDirection(dir, true);
                }

                stateMachine.ChangeState(enemy.walkState);
            }
        }
    }
}