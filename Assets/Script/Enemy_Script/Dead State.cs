using UnityEngine;

namespace Enemy
{
    public class DeadState : EnemyState
    {
        public DeadState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            enemy.animator.Play("Death");
            enemy.movement.StopMoving();
            Object.Destroy(enemy.gameObject, 2f);
        }
    }
}