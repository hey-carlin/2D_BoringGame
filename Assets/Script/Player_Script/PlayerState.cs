using UnityEngine;

namespace Player
{
    public abstract class PlayerState
    {
        protected PlayerStateMachine sm;

        public PlayerState(PlayerStateMachine sm)
        {
            this.sm = sm;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
    }
}