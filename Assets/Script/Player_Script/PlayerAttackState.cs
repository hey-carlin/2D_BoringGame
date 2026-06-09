using UnityEngine;
using Player;

namespace Player
{
    public class AttackState : PlayerState
    {
        private float timer = 0f;

        public AttackState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            timer = sm.activeAttackTimeout;
        }

        public override void OnUpdate()
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                sm.animator.SetInteger("AttackID", 0);
                sm.AttackFinished();
            }
        }

        public override void OnFixedUpdate()
        {
            sm.rb.velocity = new Vector2(0f, sm.rb.velocity.y);
        }
    }
}
