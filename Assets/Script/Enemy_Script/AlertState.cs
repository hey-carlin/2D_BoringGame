using UnityEngine;

namespace Enemy
{
    public class AlertState : EnemyState
    {
        private float timer;

        public AlertState(EnemyController enemy, StateMachine stateMachine)
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

            if (enemy.player != null)
            {
                Vector2 direction = (enemy.player.position - enemy.transform.position).normalized;
                enemy.movement.FaceDirection(direction);
            }

            if (timer >= enemy.data.alertDuration)
            {
                if (enemy.IsPlayerInAttackRange())
                {
                    stateMachine.ChangeState(enemy.attackState);
                }
                else
                {
                    stateMachine.ChangeState(enemy.walkState);
                }
            }
        }
    }
}
