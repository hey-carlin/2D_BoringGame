using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 随机待机状态：停止移动，等待随机时长后切换回随机巡逻。
    /// </summary>
    public class IdleState : EnemyState
    {
        private float idleTimer;
        private float idleDuration;

        public IdleState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            // 随机待机时长
            idleDuration = Random.Range(enemy.data.minIdleDuration, enemy.data.maxIdleDuration);
            idleTimer = 0f;

            enemy.movement.StopMoving();
            enemy.animator.Play("Idle");
        }

        public override void OnUpdate()
        {
            idleTimer += Time.deltaTime;

            // 待机超时 → 随机巡逻
            if (idleTimer >= idleDuration)
            {
                stateMachine.ChangeState(enemy.walkState);
            }
        }
    }
}
