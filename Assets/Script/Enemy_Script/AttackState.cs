using UnityEngine;

namespace Enemy
{
    public class AttackState : EnemyState
    {
        private float timer;
        private bool dealtDamage;

        public AttackState(EnemyController enemy, StateMachine stateMachine)
            : base(enemy, stateMachine) { }

        public override void OnEnter()
        {
            timer = 0f;
            dealtDamage = false;
            enemy.movement.StopMoving();
            enemy.animator.Play("Attack");
        }

        public override void OnUpdate()
        {
            timer += Time.deltaTime;

            if (!dealtDamage && timer >= enemy.data.attackHitTime)
            {
                enemy.attack.PerformAttack();
                dealtDamage = true;
            }

            if (timer >= enemy.data.attackCooldown)
            {
                stateMachine.ChangeState(enemy.walkState); // ✅ 必须回 Walk
            }
        }
    }
}