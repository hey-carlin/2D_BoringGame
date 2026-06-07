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

            if (timer >= enemy.data.idleDuration)
            {
                stateMachine.ChangeState(enemy.walkState);
            }
        }
    }
}