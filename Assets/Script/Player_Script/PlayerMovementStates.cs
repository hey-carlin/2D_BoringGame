using UnityEngine;
using Player;

namespace Player
{
    public class IdleState : PlayerState
    {
        public IdleState(PlayerStateMachine sm) : base(sm) { }

        public override void OnFixedUpdate()
        {
            sm.rb.velocity = new Vector2(0f, sm.rb.velocity.y);
        }
    }

    public class RunState : PlayerState
    {
        public RunState(PlayerStateMachine sm) : base(sm) { }

        public override void OnFixedUpdate()
        {
            sm.rb.velocity = new Vector2(sm.horizontal * sm.moveSpeed, sm.rb.velocity.y);
        }
    }

    public class JumpState : PlayerState
    {
        public JumpState(PlayerStateMachine sm) : base(sm) { }

        public override void OnEnter()
        {
            sm.SetJumpTrigger();
            sm.rb.velocity = new Vector2(sm.rb.velocity.x, 0f);
            sm.rb.AddForce(Vector2.up * sm.jumpForce, ForceMode2D.Impulse);
        }

        public override void OnUpdate()
        {
            if (sm.isGrounded && sm.rb.velocity.y <= 0.1f)
            {
                if (Mathf.Abs(sm.horizontal) > 0.1f)
                    sm.ChangeState(sm.runState);
                else
                    sm.ChangeState(sm.idleState);
            }
        }
    }
}
