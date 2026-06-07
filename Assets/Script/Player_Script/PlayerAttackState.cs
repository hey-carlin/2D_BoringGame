using UnityEngine;
using Player;

namespace Player
{
    public class AttackState : PlayerState
    {
        private float comboTimer = 0f;

        public AttackState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            comboTimer = sm.comboWindow;
        }

        public override void OnUpdate()
        {
            comboTimer -= Time.deltaTime;

            if (comboTimer <= 0f)
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
