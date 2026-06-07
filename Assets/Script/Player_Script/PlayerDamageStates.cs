using UnityEngine;
using Player;

namespace Player
{
    public class HitState : PlayerState
    {
        private float timer;

        public HitState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            timer = 0.3f;
            sm.rb.velocity = Vector2.zero;
            sm.animator.SetTrigger("Hit");
        }

        public override void OnUpdate()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                sm.ChangeState(sm.idleState);
        }
    }

    public class DeadState : PlayerState
    {
        public DeadState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            sm.controlsEnabled = false;
            sm.rb.velocity = Vector2.zero;
            sm.animator.SetTrigger("Death");
        }
    }
}
